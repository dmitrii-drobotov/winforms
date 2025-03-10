﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;
using ComIMessageFilter = Windows.Win32.Media.Audio.IMessageFilter;
using ComIServiceProvider = Windows.Win32.System.Com.IServiceProvider;
using Microsoft.Office;
using System.Windows.Forms.Primitives;
using System.Runtime.ExceptionServices;

namespace System.Windows.Forms;

public sealed partial class Application
{
    /// <summary>
    ///  This class is the embodiment of TLS for windows forms.  We do not expose this to end users because
    ///  TLS is really just an unfortunate artifact of using Win 32.  We want the world to be free
    ///  threaded.
    /// </summary>
    internal sealed class ThreadContext : MarshalByRefObject, IMsoComponent, IHandle<HANDLE>
    {
        private const int STATE_OLEINITIALIZED = 0x00000001;
        private const int STATE_EXTERNALOLEINIT = 0x00000002;
        private const int STATE_INTHREADEXCEPTION = 0x00000004;
        private const int STATE_POSTEDQUIT = 0x00000008;
        private const int STATE_FILTERSNAPSHOTVALID = 0x00000010;
        private const int STATE_TRACKINGCOMPONENT = 0x00000020;

        private static readonly nuint s_invalidId = unchecked((nuint)(-1));

        private static readonly Dictionary<uint, ThreadContext> s_contextHash = new();

        // When this gets to zero, we'll invoke a full garbage
        // collect and check for root/window leaks.
        private static readonly object s_tcInternalSyncObject = new();

        private static int s_totalMessageLoopCount;
        private static msoloop s_baseLoopReason;

        [ThreadStatic]
        private static ThreadContext? t_currentThreadContext;

        internal ThreadExceptionEventHandler? _threadExceptionHandler;
        internal EventHandler? _idleHandler;
        internal EventHandler? _enterModalHandler;
        internal EventHandler? _leaveModalHandler;

        // Parking window list
        private readonly List<ParkingWindow> _parkingWindows = new();
        private Control? _marshalingControl;
        private List<IMessageFilter>? _messageFilters;
        private List<IMessageFilter>? _messageFilterSnapshot;
        private int _inProcessFilters;
        private HANDLE _handle;
        private readonly uint _id;
        private int _messageLoopCount;
        private int _threadState;
        private int _modalCount;

        // Used for correct restoration of focus after modality
        private WeakReference<Control>? _activatingControlRef;

        // IMsoComponentManager stuff
        private IMsoComponentManager? _componentManager;
        private bool _externalComponentManager;
        private bool _fetchingComponentManager;

        // IMsoComponent stuff
        private nuint _componentID = s_invalidId;
        private Form? _currentForm;
        private ThreadWindows? _threadWindows;
        private int _disposeCount;   // To make sure that we don't allow
                                     // reentrancy in Dispose()

        // Debug helper variable
#if DEBUG
        private int _debugModalCounter;
#endif
        // We need to set this flag if we have started the ModalMessageLoop so that we don't create the ThreadWindows
        // when the ComponentManager calls on us (as IMSOComponent) during the OnEnterState.
        private bool _ourModalLoop;

        // A private field on Application that stores the callback delegate
        private MessageLoopCallback? _messageLoopCallback;

        /// <summary>
        ///  Creates a new thread context object.
        /// </summary>
        public unsafe ThreadContext()
        {
            HANDLE target;

            PInvoke.DuplicateHandle(
                PInvoke.GetCurrentProcess(),
                PInvoke.GetCurrentThread(),
                PInvoke.GetCurrentProcess(),
                &target,
                0,
                false,
                DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);

            _handle = target;

            _id = PInvoke.GetCurrentThreadId();
            _messageLoopCount = 0;
            t_currentThreadContext = this;

            lock (s_tcInternalSyncObject)
            {
                s_contextHash[_id] = this;
            }
        }

        public ApplicationContext? ApplicationContext { get; private set; }

        /// <summary>
        ///  Retrieves the component manager for this process.  If there is no component manager
        ///  currently installed, we install our own.
        /// </summary>
        internal unsafe IMsoComponentManager? ComponentManager
        {
            get
            {
                Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "Application.ComponentManager.Get:");

                if (_componentManager is not null || _fetchingComponentManager)
                {
                    return _componentManager;
                }

                // The CLR is a good COM citizen and will pump messages when things are waiting.
                // This is nice; it keeps the world responsive.  But, it is also very hard for
                // us because most of the code below causes waits, and the likelihood that
                // a message will come in and need a component manager is very high.  Recursing
                // here is very very bad, and will almost certainly lead to application failure
                // later on as we come out of the recursion.  So, we guard it here and return
                // null.  EVERYONE who accesses the component manager must handle a NULL return!

                _fetchingComponentManager = true;

                try
                {
                    // Attempt to obtain the Host Application MSOComponentManager
                    _componentManager = GetExternalComponentManager();
                    if (_componentManager is not null)
                    {
                        _externalComponentManager = true;
                        Debug.WriteLineIf(
                            CompModSwitches.MSOComponentManager.TraceInfo,
                            "Using MSO Component manager");
                    }
                    else
                    {
                        _componentManager = new ComponentManager();
                        Debug.WriteLineIf(
                            CompModSwitches.MSOComponentManager.TraceInfo,
                            "Using our own component manager");
                    }

                    if (_componentManager is not null)
                    {
                        RegisterComponentManager();
                    }
                }
                finally
                {
                    _fetchingComponentManager = false;
                }

                return _componentManager;

                unsafe static IMsoComponentManager? GetExternalComponentManager()
                {
                    Application.OleRequired();
                    using ComScope<ComIMessageFilter> messageFilter = new(null);

                    // Clear the thread's message filter to see if there was an existing filter
                    if (PInvoke.CoRegisterMessageFilter(null, messageFilter).Failed || messageFilter.IsNull)
                    {
                        return null;
                    }

                    // There was an existing filter, reregister it
                    ComIMessageFilter* dummy = default;
                    PInvoke.CoRegisterMessageFilter(messageFilter, &dummy);

                    // Now look to see if it implements the native IServiceProvider
                    using var serviceProvider = messageFilter.TryQuery<ComIServiceProvider>(out HRESULT hr);
                    if (hr.Failed)
                    {
                        return null;
                    }

                    // Check the service provider for the service that provides IMsoComponentManager
                    using ComScope<IUnknown> serviceHandle = new(null);
                    Guid sid = new(MsoComponentIds.SID_SMsoComponentManager);
                    Guid iid = new(MsoComponentIds.IID_IMsoComponentManager);

                    if (serviceProvider.Value->QueryService(&sid, &iid, serviceHandle).Failed || serviceHandle.IsNull)
                    {
                        return null;
                    }

                    // We have the component manager service, now get the component manager interface
                    void* componentManagerHandle = null;
                    hr = serviceHandle.Value->QueryInterface(&iid, &componentManagerHandle);

                    if (hr.Succeeded && componentManagerHandle is not null)
                    {
                        ComHelpers.TryGetManagedInterface(
                            (IUnknown*)componentManagerHandle,
                            takeOwnership: true,
                            out IMsoComponentManager? componentManager);

                        return componentManager;
                    }

                    return null;
                }

                void RegisterComponentManager()
                {
                    Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "Registering MSO component with the component manager");
                    MSOCRINFO info = new()
                    {
                        cbSize = (uint)sizeof(MSOCRINFO),
                        uIdleTimeInterval = 0,
                        grfcrf = msocrf.PreTranslateAll | msocrf.NeedIdleTime,
                        grfcadvf = msocadvf.Modal
                    };

                    UIntPtr id;
                    bool result = _componentManager.FRegisterComponent(this, &info, &id);
                    _componentID = id;
                    Debug.Assert(_componentID != s_invalidId, "Our ID sentinel was returned as a valid ID");

                    if (result && _componentManager is not Application.ComponentManager)
                    {
                        _messageLoopCount++;
                    }

                    Debug.Assert(result,
                        $"Failed to register WindowsForms with the ComponentManager -- DoEvents and modal dialogs will be broken. size: {info.cbSize}");
                    Debug.WriteLineIf(
                        CompModSwitches.MSOComponentManager.TraceInfo,
                        $"ComponentManager.FRegisterComponent returned {result}");
                    Debug.WriteLineIf(
                        CompModSwitches.MSOComponentManager.TraceInfo,
                        $"ComponentManager.FRegisterComponent assigned a componentID == [0x{_componentID.ToUInt64():X16}]");
                }
            }
        }

        internal bool CustomThreadExceptionHandlerAttached => _threadExceptionHandler is not null;

        /// <summary>
        ///  Retrieves the actual parking form.  This will demand create the parking window
        ///  if it needs to.
        /// </summary>
        internal ParkingWindow GetParkingWindow(DPI_AWARENESS_CONTEXT context)
        {
            // Locking 'this' here is ok since this is an internal class.
            lock (this)
            {
                ParkingWindow? parkingWindow = GetParkingWindowForContext(context);
                if (parkingWindow is null)
                {
#if DEBUG
                    if (CoreSwitches.PerfTrack.Enabled)
                    {
                        Debug.WriteLine("Creating parking form!");
                        Debug.WriteLine(CoreSwitches.PerfTrack.Enabled, Environment.StackTrace);
                    }
#endif

                    using (DpiHelper.EnterDpiAwarenessScope(context))
                    {
                        parkingWindow = new ParkingWindow();
                    }

                    _parkingWindows.Add(parkingWindow);
                }

                return parkingWindow;
            }
        }

        /// <summary>
        ///  Returns parking window that matches dpi awareness context. return null if not found.
        /// </summary>
        /// <returns>return matching parking window from list. returns null if not found</returns>
        internal ParkingWindow? GetParkingWindowForContext(DPI_AWARENESS_CONTEXT context)
        {
            if (_parkingWindows.Count == 0)
            {
                return null;
            }

            // Legacy OS/target framework scenario where ControlDpiContext is set to DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_UNSPECIFIED
            // because of 'ThreadContextDpiAwareness' API unavailability or this feature is not enabled.
            if (PInvoke.AreDpiAwarenessContextsEqualInternal(context, DPI_AWARENESS_CONTEXT.UNSPECIFIED_DPI_AWARENESS_CONTEXT))
            {
                Debug.Assert(_parkingWindows.Count == 1, "parkingWindows count can not be > 1 for legacy OS/target framework versions");

                return _parkingWindows[0];
            }

            // Supported OS scenario.
            foreach (ParkingWindow p in _parkingWindows)
            {
                if (PInvoke.AreDpiAwarenessContextsEqualInternal(p.DpiAwarenessContext, context))
                {
                    return p;
                }
            }

            // Parking window is not yet created for the requested DpiAwarenessContext
            return null;
        }

        internal Control? ActivatingControl
        {
            get => _activatingControlRef?.TryGetTarget(out Control? target) ?? false ? target : null;
            set => _activatingControlRef = value is null ? null : new(value);
        }

        /// <summary>
        ///  Retrieves the actual parking form.  This will demand create the MarshalingControl window
        ///  if it needs to.
        /// </summary>
        internal Control MarshalingControl
        {
            get
            {
                lock (this)
                {
#pragma warning disable IDE0074 // disabled because of debug block
                    if (_marshalingControl is null)
                    {
#if DEBUG
                        if (CoreSwitches.PerfTrack.Enabled)
                        {
                            Debug.WriteLine("Creating marshalling control!");
                            Debug.WriteLine(CoreSwitches.PerfTrack.Enabled, Environment.StackTrace);
                        }
#endif

                        _marshalingControl = new MarshalingControl();
                    }

                    return _marshalingControl;
#pragma warning restore IDE0074
                }
            }
        }

        /// <summary>
        ///  Allows you to setup a message filter for the application's message pump.  This
        ///  installs the filter on the current thread.
        /// </summary>
        internal void AddMessageFilter(IMessageFilter? filter)
        {
            _messageFilters ??= new();
            _messageFilterSnapshot ??= new();

            if (filter is not null)
            {
                SetState(STATE_FILTERSNAPSHOTVALID, false);
                if (_messageFilters.Count > 0 && filter is IMessageModifyAndFilter)
                {
                    // insert the IMessageModifyAndFilter filters first
                    _messageFilters.Insert(0, filter);
                }
                else
                {
                    _messageFilters.Add(filter);
                }
            }
        }

        // Called immediately before we begin pumping messages for a modal message loop.
        internal unsafe void BeginModalMessageLoop(ApplicationContext? context)
        {
#if DEBUG
            _debugModalCounter++;
#endif
            // Set the ourModalLoop flag so that the "IMSOComponent.OnEnterState" is a NOOP since we started the ModalMessageLoop.
            bool wasOurLoop = _ourModalLoop;
            _ourModalLoop = true;
            try
            {
                IMsoComponentManager? cm = ComponentManager;
                cm?.OnComponentEnterState(_componentID, msocstate.Modal, msoccontext.All, 0, null, 0);
            }
            finally
            {
                _ourModalLoop = wasOurLoop;
            }

            // This will initialize the ThreadWindows with proper flags.
            DisableWindowsForModalLoop(onlyWinForms: false, context);

            _modalCount++;

            if (_enterModalHandler is not null && _modalCount == 1)
            {
                _enterModalHandler(Thread.CurrentThread, EventArgs.Empty);
            }
        }

        // Disables windows in preparation of going modal.  If parameter is true, we disable all
        // windows, if false, only windows forms windows (i.e., windows controlled by this MsoComponent).
        // See also IMsoComponent.OnEnterState.
        internal void DisableWindowsForModalLoop(bool onlyWinForms, ApplicationContext? context)
        {
            Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "ComponentManager : Entering modal state");
            ThreadWindows? old = _threadWindows;
            _threadWindows = new ThreadWindows(onlyWinForms);
            _threadWindows.Enable(false);
            _threadWindows._previousThreadWindows = old;

            if (context is ModalApplicationContext modalContext)
            {
                modalContext.DisableThreadWindows(true, onlyWinForms);
            }
        }

        /// <summary>
        ///  Disposes this thread context object.  Note that this will marshal to the owning thread.
        /// </summary>
        internal void Dispose(bool postQuit)
        {
            // Need to avoid multiple threads coming in here or we'll leak the thread handle.
            lock (this)
            {
                try
                {
                    // Make sure that we are not reentrant
                    if (_disposeCount++ != 0)
                    {
                        return;
                    }

                    // Unravel our message loop. This will marshal us over to the right thread, making the dispose() method async.
                    if (_messageLoopCount > 0 && postQuit)
                    {
                        PostQuit();
                    }
                    else
                    {
                        bool ourThread = PInvoke.GetCurrentThreadId() == _id;

                        try
                        {
                            // We can only clean up if we're being called on our own thread.
                            if (!ourThread)
                            {
                                return;
                            }

                            // If we had a component manager, detach from it.
                            if (_componentManager is not null)
                            {
                                RevokeComponent();
                            }

                            DisposeThreadWindows();

                            try
                            {
                                RaiseThreadExit();
                            }
                            finally
                            {
                                if (GetState(STATE_OLEINITIALIZED) && !GetState(STATE_EXTERNALOLEINIT))
                                {
                                    SetState(STATE_OLEINITIALIZED, false);
                                    PInvoke.OleUninitialize();
                                }
                            }
                        }
                        finally
                        {
                            // We can always clean up this handle though.
                            if (!_handle.IsNull)
                            {
                                PInvoke.CloseHandle(this);
                                _handle = HANDLE.Null;
                            }

                            try
                            {
                                if (s_totalMessageLoopCount == 0)
                                {
                                    RaiseExit();
                                }
                            }
                            finally
                            {
                                lock (s_tcInternalSyncObject)
                                {
                                    s_contextHash.Remove(_id);
                                }

                                if (t_currentThreadContext == this)
                                {
                                    t_currentThreadContext = null;
                                }
                            }
                        }
                    }

                    GC.SuppressFinalize(this);
                }
                finally
                {
                    _disposeCount--;
                }
            }
        }

        /// <summary>
        ///  Disposes of this thread's parking form.
        /// </summary>
        private void DisposeParkingWindow()
        {
            if (_parkingWindows.Count != 0)
            {
                // We take two paths here.  If we are on the same thread as
                // the parking window, we can destroy its handle.  If not,
                // we just null it and let it GC.  When it finalizes it
                // will disconnect its handle and post a WM_CLOSE.
                //
                // It is important that we just call DestroyHandle here
                // and do not call Dispose.  Otherwise we would destroy
                // controls that are living on the parking window.
                uint hwndThread = PInvoke.GetWindowThreadProcessId(_parkingWindows[0], out _);
                uint currentThread = PInvoke.GetCurrentThreadId();

                for (int i = 0; i < _parkingWindows.Count; i++)
                {
                    if (hwndThread == currentThread)
                    {
                        _parkingWindows[i].Destroy();
                    }
                }

                _parkingWindows.Clear();
            }
        }

        /// <summary>
        ///  Gets rid of all windows in this thread context.  Nulls out
        ///  window objects that we hang on to.
        /// </summary>
        internal void DisposeThreadWindows()
        {
            // We dispose the main window first, so it can perform any
            // cleanup that it may need to do.
            try
            {
                if (ApplicationContext is not null)
                {
                    ApplicationContext.Dispose();
                    ApplicationContext = null;
                }

                // Then, we rudely destroy all of the windows on the thread
                ThreadWindows tw = new ThreadWindows(true);
                tw.Dispose();

                // And dispose the parking form, if it isn't already
                DisposeParkingWindow();
            }
            catch
            {
            }
        }

        // Enables windows in preparation of stopping modal.  If parameter is true, we enable all windows,
        // if false, only windows forms windows (i.e., windows controlled by this MsoComponent).
        // See also IMsoComponent.OnEnterState.
        internal void EnableWindowsForModalLoop(bool onlyWinForms, ApplicationContext? context)
        {
            Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "ComponentManager : Leaving modal state");
            if (_threadWindows is not null)
            {
                _threadWindows.Enable(true);
                Debug.Assert(_threadWindows is not null, "OnEnterState recursed, but it's not supposed to be reentrant");
                _threadWindows = _threadWindows._previousThreadWindows;
            }

            if (context is ModalApplicationContext modalContext)
            {
                modalContext.DisableThreadWindows(false, onlyWinForms);
            }
        }

        // Called immediately after we end pumping messages for a modal message loop.
        internal unsafe void EndModalMessageLoop(ApplicationContext? context)
        {
#if DEBUG
            _debugModalCounter--;
            Debug.Assert(_debugModalCounter >= 0, "Mis-matched calls to Application.BeginModalMessageLoop() and Application.EndModalMessageLoop()");
#endif
            // This will re-enable the windows...
            EnableWindowsForModalLoop(false, context); // onlyWinForms = false

            bool wasOurLoop = _ourModalLoop;
            _ourModalLoop = true;
            try
            {
                // If We started the ModalMessageLoop .. this will call us back on the IMSOComponent.OnStateEnter and not do anything ...
                IMsoComponentManager? cm = ComponentManager;
                cm?.FOnComponentExitState(_componentID, msocstate.Modal, msoccontext.All, 0, null);
            }
            finally
            {
                // Reset the flag since we are exiting out of a ModalMessageLoop..
                _ourModalLoop = wasOurLoop;
            }

            _modalCount--;

            if (_leaveModalHandler is not null && _modalCount == 0)
            {
                _leaveModalHandler(Thread.CurrentThread, EventArgs.Empty);
            }
        }

        /// <summary>
        ///  Exits the program by disposing of all thread contexts and message loops.
        /// </summary>
        internal static void ExitApplication() => ExitCommon(disposing: true);

        private static void ExitCommon(bool disposing)
        {
            lock (s_tcInternalSyncObject)
            {
                if (s_contextHash is not null)
                {
                    ThreadContext[] ctxs = new ThreadContext[s_contextHash.Values.Count];
                    s_contextHash.Values.CopyTo(ctxs, 0);
                    for (int i = 0; i < ctxs.Length; ++i)
                    {
                        if (ctxs[i].ApplicationContext is ApplicationContext context)
                        {
                            context.ExitThread();
                        }
                        else
                        {
                            ctxs[i].Dispose(disposing);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  Our finalization. This shouldn't be called as we should always be disposed.
        /// </summary>
        ~ThreadContext()
        {
            // Don't call OleUninitialize as the finalizer is called on the wrong thread.
            // We can always clean up this handle, though.
            if (!_handle.IsNull)
            {
                PInvoke.CloseHandle(_handle);
                _handle = HANDLE.Null;
            }
        }

        // When a Form receives a WM_ACTIVATE message, it calls this method so we can do the
        // appropriate MsoComponentManager activation magic
        internal unsafe void FormActivated(bool activate)
        {
            if (activate)
            {
                IMsoComponentManager? cm = ComponentManager;
                if (cm is not null && cm is not Application.ComponentManager)
                {
                    cm.FOnComponentActivate(_componentID);
                }
            }
        }

        /// <summary>
        ///  Sets this component as the tracking component - trumping any active component
        ///  for message filtering.
        /// </summary>
        internal unsafe void TrackInput(bool track)
        {
            // protect against double setting, as this causes asserts in the VS component manager.
            if (track != GetState(STATE_TRACKINGCOMPONENT))
            {
                IMsoComponentManager? cm = ComponentManager;
                if (cm is not null && cm is not Application.ComponentManager)
                {
                    cm.FSetTrackingComponent(_componentID, track);
                    SetState(STATE_TRACKINGCOMPONENT, track);
                }
            }
        }

        /// <summary>
        ///  Retrieves a ThreadContext object for the current thread
        /// </summary>
        internal static ThreadContext FromCurrent()
            => t_currentThreadContext ?? new ThreadContext();

        /// <summary>
        ///  Retrieves a ThreadContext object for the given thread ID
        /// </summary>
        internal static ThreadContext? FromId(uint id)
        {
            if (!s_contextHash.TryGetValue(id, out ThreadContext? context) && id == PInvoke.GetCurrentThreadId())
            {
                context = new ThreadContext();
            }

            return context;
        }

        /// <summary>
        ///  Determines if it is OK to allow an application to quit and shutdown
        ///  the runtime.  We only allow this if we own the base message pump.
        /// </summary>
        internal static bool GetAllowQuit()
            => s_totalMessageLoopCount > 0 && s_baseLoopReason == msoloop.Main;

        /// <summary>
        ///  Retrieves the handle to this thread.
        /// </summary>
        public HANDLE Handle => _handle;

        HANDLE IHandle<HANDLE>.Handle => Handle;

        /// <summary>
        ///  Retrieves the ID of this thread.
        /// </summary>
        internal uint GetId() => _id;

        /// <summary>
        ///  Determines if a message loop exists on this thread.
        /// </summary>
        internal bool GetMessageLoop() => GetMessageLoop(mustBeActive: false);

        /// <summary>
        ///  Determines if a message loop exists on this thread.
        /// </summary>
        internal unsafe bool GetMessageLoop(bool mustBeActive)
        {
            // If we are already running a loop, we're fine.
            // If we are running in external manager we may need to make sure first the loop is active
            if (_messageLoopCount > (mustBeActive && _externalComponentManager ? 1 : 0))
            {
                return true;
            }

            // Also, access the ComponentManager property to demand create it, and we're also
            // fine if it is an external manager, because it has already pushed a loop.
            if (ComponentManager is not null && _externalComponentManager)
            {
                if (mustBeActive == false)
                {
                    return true;
                }

                using ComScope<IUnknown> component = new(null);
                if (ComponentManager.FGetActiveComponent(msogac.Active, component, null, 0))
                {
                    return ComHelpers.WrapsManagedObject(this, component);
                }
            }

            // Finally, check if a message loop has been registered
            MessageLoopCallback? callback = _messageLoopCallback;
            if (callback is not null)
            {
                return callback();
            }

            // Otherwise, we do not have a loop running.
            return false;
        }

        private bool GetState(int bit) => (_threadState & bit) != 0;

        /// <summary>
        ///  A method of determining whether we are handling messages that does not demand register
        ///  the componentmanager
        /// </summary>
        internal bool IsValidComponentId() => _componentID != s_invalidId;

        internal ApartmentState OleRequired()
        {
            _ = Thread.CurrentThread;
            if (!GetState(STATE_OLEINITIALIZED))
            {
                HRESULT ret;
                unsafe
                {
                    ret = PInvoke.OleInitialize(pvReserved: (void*)null);
                }

                SetState(STATE_OLEINITIALIZED, true);
                if (ret == HRESULT.RPC_E_CHANGED_MODE)
                {
                    // This could happen if the thread was already initialized for MTA
                    // and then we call OleInitialize which tries to initialized it for STA
                    // This currently happens while profiling...
                    SetState(STATE_EXTERNALOLEINIT, true);
                }
            }

            return GetState(STATE_EXTERNALOLEINIT) ? ApartmentState.MTA : ApartmentState.STA;
        }

        private void OnAppThreadExit(object? sender, EventArgs e)
            => Dispose(postQuit: true);

        /// <summary>
        ///  Called when an untrapped exception occurs in a thread. This allows the programmer to trap these, and, if
        ///  left untrapped, throws a standard error dialog.
        /// </summary>
        internal void OnThreadException(Exception ex)
        {
            if (GetState(STATE_INTHREADEXCEPTION))
            {
                return;
            }

            SetState(STATE_INTHREADEXCEPTION, true);
            try
            {
                if (_threadExceptionHandler is not null)
                {
                    _threadExceptionHandler(Thread.CurrentThread, new ThreadExceptionEventArgs(ex));
                }
                else
                {
                    if (LocalAppContextSwitches.DoNotCatchUnhandledExceptions)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }

                    if (SystemInformation.UserInteractive)
                    {
                        ThreadExceptionDialog dialog = new(ex);
                        DialogResult result = DialogResult.OK;

                        try
                        {
                            result = dialog.ShowDialog();
                        }
                        finally
                        {
                            dialog.Dispose();
                        }

                        switch (result)
                        {
                            case DialogResult.Abort:
                                Exit();
                                Environment.Exit(0);
                                break;
                            case DialogResult.Yes:
                                if (ex is WarningException warning)
                                {
                                    Help.ShowHelp(null, warning.HelpUrl, warning.HelpTopic);
                                }

                                break;
                        }
                    }
                    else
                    {
                        // Ignore unhandled thread exceptions. The user can
                        // override if they really care.
                    }
                }
            }
            finally
            {
                SetState(STATE_INTHREADEXCEPTION, false);
            }
        }

        internal void PostQuit()
        {
            Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "ComponentManager : Attempting to terminate message loop");

            // Per KB 183116: https://web.archive.org/web/20070510025823/http://support.microsoft.com/kb/183116
            //
            // WM_QUIT may be consumed by another message pump under very specific circumstances.
            // When that occurs, we rely on the STATE_POSTEDQUIT to be caught in the next
            // idle, at which point we can tear down.
            //
            // We can't follow the KB article exactly, because we don't have an HWND to PostMessage to.
            PInvoke.PostThreadMessage(_id, (uint)PInvoke.WM_QUIT, default, default);
            SetState(STATE_POSTEDQUIT, true);
        }

        /// <summary>
        ///  Allows the hosting environment to register a callback
        /// </summary>
        internal void RegisterMessageLoop(MessageLoopCallback? callback)
        {
            _messageLoopCallback = callback;
        }

        /// <summary>
        ///  Removes a message filter previously installed with addMessageFilter.
        /// </summary>
        internal void RemoveMessageFilter(IMessageFilter f)
        {
            if (_messageFilters is not null)
            {
                SetState(STATE_FILTERSNAPSHOTVALID, false);
                _messageFilters.Remove(f);
            }
        }

        /// <summary>
        ///  Starts a message loop for the given reason.
        /// </summary>
        internal void RunMessageLoop(msoloop reason, ApplicationContext? context)
        {
            // Ensure that we attempt to apply theming before doing anything that might create a window.
            using ThemingScope scope = new(UseVisualStyles);
            RunMessageLoopInner(reason, context);
        }

        private unsafe void RunMessageLoopInner(msoloop reason, ApplicationContext? context)
        {
            Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "ThreadContext.PushMessageLoop {");
            Debug.Indent();

            if (reason == msoloop.ModalForm && !SystemInformation.UserInteractive)
            {
                throw new InvalidOperationException(SR.CantShowModalOnNonInteractive);
            }

            // If we've entered because of a Main message loop being pushed
            // (different than a modal message loop or DoEVents loop)
            // then clear the QUIT flag to allow normal processing.
            // this flag gets set during loop teardown for another form.
            if (reason == msoloop.Main)
            {
                SetState(STATE_POSTEDQUIT, false);
            }

            if (s_totalMessageLoopCount++ == 0)
            {
                s_baseLoopReason = reason;
            }

            _messageLoopCount++;

            if (reason == msoloop.Main)
            {
                // If someone has tried to push another main message loop on this thread,
                // ignore it.
                if (_messageLoopCount != 1)
                {
                    throw new InvalidOperationException(SR.CantNestMessageLoops);
                }

                ApplicationContext = context;

                ApplicationContext!.ThreadExit += new EventHandler(OnAppThreadExit);

                if (ApplicationContext.MainForm is not null)
                {
                    ApplicationContext.MainForm.Visible = true;
                }
            }

            Form? oldForm = _currentForm;
            if (context is not null)
            {
                _currentForm = context.MainForm;
            }

            bool fullModal = false;
            bool localModal = false;
            HWND hwndOwner = default;

            if (reason == msoloop.DoEventsModal)
            {
                localModal = true;
            }

            if (reason == msoloop.ModalForm || reason == msoloop.ModalAlert)
            {
                fullModal = true;

                // We're about to disable all windows in the thread so our modal dialog can be the top dog.  Because this can interact
                // with external MSO things, and also because the modal dialog could have already had its handle created,
                // Check to see if the handle exists and if the window is currently enabled. We remember this so we can set the
                // window back to enabled after disabling everyone else.  This is just a precaution against someone doing the
                // wrong thing and disabling our dialog.

                bool modalEnabled = _currentForm is not null && _currentForm.Enabled;

                Debug.WriteLineIf(
                    CompModSwitches.MSOComponentManager.TraceInfo,
                    $"[0x{_componentID.ToUInt64():X16}] Notifying component manager that we are entering a modal loop");

                BeginModalMessageLoop(context);

                // If the owner window of the dialog is still enabled, disable it now.
                // This can happen if the owner window is from a different thread or
                // process.
                if (_currentForm is not null)
                {
                    hwndOwner = (HWND)PInvoke.GetWindowLong(_currentForm, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT);
                    if (!hwndOwner.IsNull)
                    {
                        if (PInvoke.IsWindowEnabled(hwndOwner))
                        {
                            PInvoke.EnableWindow(hwndOwner, false);
                        }
                        else
                        {
                            // Reset hwndOwner so we are not tempted to fiddle with it
                            hwndOwner = default;
                        }
                    }
                }

                // The second half of the modalEnabled flag above.  Here, if we were previously
                // enabled, make sure that's still the case.
                if (_currentForm is not null && _currentForm.IsHandleCreated && PInvoke.IsWindowEnabled(_currentForm) != modalEnabled)
                {
                    PInvoke.EnableWindow(_currentForm, modalEnabled);
                }
            }

            try
            {
                Debug.WriteLineIf(
                    CompModSwitches.MSOComponentManager.TraceInfo,
                    $"[0x{_componentID.ToUInt64():X16}] Calling ComponentManager.FPushMessageLoop...");

                bool result;

                // Register marshaller for background tasks.  At this point,
                // need to be able to successfully get the handle to the
                // parking window.  Only do it when we're entering the first
                // message loop for this thread.
                if (_messageLoopCount == 1)
                {
                    WindowsFormsSynchronizationContext.InstallIfNeeded();
                }

                // Need to do this in a try/finally.  Also good to do after we installed the synch context.
                if (fullModal && _currentForm is not null)
                {
                    _currentForm.Visible = true;
                }

                if ((!fullModal && !localModal) || ComponentManager is ComponentManager)
                {
                    result = ComponentManager!.FPushMessageLoop(_componentID, reason, null);
                }
                else if (reason == msoloop.DoEvents || reason == msoloop.DoEventsModal)
                {
                    result = LocalModalMessageLoop(null);
                }
                else
                {
                    result = LocalModalMessageLoop(_currentForm);
                }

                Debug.WriteLineIf(
                    CompModSwitches.MSOComponentManager.TraceInfo,
                    $"[0x{_componentID.ToUInt64():X16}] ComponentManager.FPushMessageLoop returned {result}");
            }
            finally
            {
                if (fullModal)
                {
                    Debug.WriteLineIf(
                        CompModSwitches.MSOComponentManager.TraceInfo,
                        $"[0x{_componentID.ToUInt64():X16}] Notifying component manager that we are exiting a modal loop");

                    EndModalMessageLoop(context);

                    // Again, if the hwndOwner was valid and disabled above, re-enable it.
                    if (hwndOwner != IntPtr.Zero)
                    {
                        PInvoke.EnableWindow(hwndOwner, true);
                    }
                }

                _currentForm = oldForm;
                s_totalMessageLoopCount--;
                _messageLoopCount--;

                if (_messageLoopCount == 0)
                {
                    // Last message loop shutting down, restore the sync context that was in place before we started
                    // the first message loop.
                    WindowsFormsSynchronizationContext.Uninstall(turnOffAutoInstall: false);
                }

                if (reason == msoloop.Main)
                {
                    Dispose(true);
                }
                else if (_messageLoopCount == 0 && _componentManager is not null)
                {
                    // If we had a component manager, detach from it.
                    RevokeComponent();
                }
            }

            Debug.Unindent();
            Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "}");
        }

        private unsafe bool LocalModalMessageLoop(Form? form)
        {
            try
            {
                // Execute the message loop until the active component tells us to stop.
                MSG msg = default;
                bool continueLoop = true;

                while (continueLoop)
                {
                    if (PInvoke.GetMessage(&msg, HWND.Null, 0, 0))
                    {
                        if (!PreTranslateMessage(ref msg))
                        {
                            PInvoke.TranslateMessage(msg);
                            PInvoke.DispatchMessage(&msg);
                        }

                        if (form is not null)
                        {
                            continueLoop = !form.CheckCloseDialog(false);
                        }
                    }
                    else if (form is null)
                    {
                        break;
                    }
                    else if (!PInvoke.PeekMessage(&msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE))
                    {
                        PInvoke.WaitMessage();
                    }
                }

                return continueLoop;
            }
            catch
            {
                return false;
            }
        }

        internal bool ProcessFilters(ref MSG msg, out bool modified)
        {
            bool filtered = false;

            modified = false;

            // Account for the case where someone removes a message filter as a result of PreFilterMessage.
            // The message filter will be removed from the _next_ message.

            // If message filter is added or removed inside the user-provided PreFilterMessage function,
            // and user code pumps messages, we might re-enter ProcessFilter on the same stack, we
            // should not update the snapshot until the next message.
            if (_messageFilters is not null && !GetState(STATE_FILTERSNAPSHOTVALID) && _inProcessFilters == 0)
            {
                if (_messageFilterSnapshot is not null)
                {
                    _messageFilterSnapshot.Clear();
                    if (_messageFilters.Count > 0)
                    {
                        _messageFilterSnapshot.AddRange(_messageFilters);
                    }
                }

                SetState(STATE_FILTERSNAPSHOTVALID, true);
            }

            _inProcessFilters++;
            try
            {
                if (_messageFilterSnapshot is not null && _messageFilterSnapshot.Count != 0)
                {
                    IMessageFilter f;
                    int count = _messageFilterSnapshot.Count;

                    Message m = Message.Create(msg.hwnd, msg.message, msg.wParam, msg.lParam);

                    for (int i = 0; i < count; i++)
                    {
                        f = _messageFilterSnapshot[i];
                        bool filterMessage = f.PreFilterMessage(ref m);

                        // Make sure that we update the msg struct with the new result after the call to
                        // PreFilterMessage.
                        if (f is IMessageModifyAndFilter)
                        {
                            msg.hwnd = (HWND)m.HWnd;
                            msg.message = (uint)m.MsgInternal;
                            msg.wParam = m.WParamInternal;
                            msg.lParam = m.LParamInternal;
                            modified = true;
                        }

                        if (filterMessage)
                        {
                            filtered = true;
                            break;
                        }
                    }
                }
            }
            finally
            {
                _inProcessFilters--;
            }

            return filtered;
        }

        /// <summary>
        ///  Message filtering routine that is called before dispatching a message.
        ///  If this returns true, the message is already processed.  If it returns
        ///  false, the message should be allowed to continue through the dispatch
        ///  mechanism.
        /// </summary>
        internal bool PreTranslateMessage(ref MSG msg)
        {
            if (ProcessFilters(ref msg, out _))
            {
                return true;
            }

            if (msg.IsKeyMessage())
            {
                if (msg.message == (uint)PInvoke.WM_CHAR)
                {
                    int breakLParamMask = 0x1460000; // 1 = extended keyboard, 46 = scan code
                    if (unchecked((int)(uint)msg.wParam) == 3 && (unchecked((int)msg.lParam) & breakLParamMask) == breakLParamMask) // ctrl-brk
                    {
                        // wParam is the key character, which for ctrl-brk is the same as ctrl-C.
                        // So we need to go to the lparam to distinguish the two cases.
                        // You might also be able to do this with WM_KEYDOWN (again with wParam=3)

                        if (Debugger.IsAttached)
                        {
                            Debugger.Break();
                        }
                    }
                }

                Control? target = Control.FromChildHandle(msg.hwnd);
                bool retValue = false;

                Message m = Message.Create(msg.hwnd, msg.message, msg.wParam, msg.lParam);

                if (target is not null)
                {
                    if (NativeWindow.WndProcShouldBeDebuggable)
                    {
                        // We don't want to do a catch in the debuggable case.
                        if (Control.PreProcessControlMessageInternal(target, ref m) == PreProcessControlState.MessageProcessed)
                        {
                            retValue = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            if (Control.PreProcessControlMessageInternal(target, ref m) == PreProcessControlState.MessageProcessed)
                            {
                                retValue = true;
                            }
                        }
                        catch (Exception e)
                        {
                            OnThreadException(e);
                        }
                    }
                }
                else
                {
                    // See if this is a dialog message -- this is for handling any native dialogs that are launched from
                    // winforms code.  This can happen with ActiveX controls that launch dialogs specifically

                    // First, get the first top-level window in the hierarchy.
                    HWND hwndRoot = PInvoke.GetAncestor(msg.hwnd, GET_ANCESTOR_FLAGS.GA_ROOT);

                    // If we got a valid HWND, then call IsDialogMessage on it.  If that returns true, it's been processed
                    // so we should return true to prevent Translate/Dispatch from being called.
                    if (!hwndRoot.IsNull && PInvoke.IsDialogMessage(hwndRoot, in msg))
                    {
                        return true;
                    }
                }

                msg.wParam = m.WParamInternal;
                msg.lParam = m.LParamInternal;

                if (retValue)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///  Revokes our component from the active component manager.  Does
        ///  nothing if there is no active component manager or we are
        ///  already invoked.
        /// </summary>
        private unsafe void RevokeComponent()
        {
            if (_componentManager is not null && _componentID != s_invalidId)
            {
                IMsoComponentManager msocm = _componentManager;

                try
                {
                    msocm.FRevokeComponent(_componentID);
                    if (Marshal.IsComObject(msocm))
                    {
                        Marshal.ReleaseComObject(msocm);
                    }
                }
                finally
                {
                    _componentManager = null;
                    _componentID = s_invalidId;
                }
            }
        }

        private void SetState(int bit, bool value)
        {
            if (value)
            {
                _threadState |= bit;
            }
            else
            {
                _threadState &= (~bit);
            }
        }

        // Things to test in VS when you change the IMsoComponent code:
        //
        // - You can bring up dialogs multiple times (ie, the editor for TextBox.Lines)
        // - Double-click DataFormWizard, cancel wizard
        // - When a dialog is open and you switch to another application, when you switch
        //   back to VS the dialog gets the focus
        // - If one modal dialog launches another, they are all modal (Try web forms Table\Rows\Cell)
        // - When a dialog is up, VS is completely disabled, including moving and resizing VS.
        // - After doing all this, you can ctrl-shift-N start a new project and VS is enabled.

        BOOL IMsoComponent.FDebugMessage(IntPtr hInst, uint msg, IntPtr wparam, IntPtr lparam)
            => true;

        unsafe BOOL IMsoComponent.FPreTranslateMessage(MSG* msg)
            => PreTranslateMessage(ref Unsafe.AsRef<MSG>(msg));

        void IMsoComponent.OnEnterState(msocstate uStateID, BOOL fEnter)
        {
            Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, $"ComponentManager : OnEnterState({uStateID}, {fEnter})");

            // Return if our (WINFORMS) Modal Loop is still running.
            if (_ourModalLoop)
            {
                return;
            }

            if (uStateID == msocstate.Modal)
            {
                // We should only be messing with windows we own.  See the "ctrl-shift-N" test above.
                if (fEnter)
                {
                    DisableWindowsForModalLoop(true, null); // WinFormsOnly = true
                }
                else
                {
                    EnableWindowsForModalLoop(true, null); // WinFormsOnly = true
                }
            }
        }

        void IMsoComponent.OnAppActivate(BOOL fActive, uint dwOtherThreadID)
        {
        }

        void IMsoComponent.OnLoseActivation()
        {
            Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "ComponentManager : Our component is losing activation.");
        }

        unsafe void IMsoComponent.OnActivationChange(
            IMsoComponent? component,
            BOOL fSameComponent,
            MSOCRINFO* pcrinfo,
            BOOL fHostIsActivating,
            IntPtr pchostinfo,
            uint dwReserved)
        {
            Debug.WriteLineIf(CompModSwitches.MSOComponentManager.TraceInfo, "ComponentManager : OnActivationChange");
        }

        BOOL IMsoComponent.FDoIdle(msoidlef grfidlef)
        {
            _idleHandler?.Invoke(Thread.CurrentThread, EventArgs.Empty);
            return false;
        }

        unsafe BOOL IMsoComponent.FContinueMessageLoop(
            msoloop uReason,
            void* pvLoopData,
            MSG* pMsgPeeked)
        {
            bool continueLoop = true;

            // If we get a null message, and we have previously posted the WM_QUIT message,
            // then someone ate the message.
            if (pMsgPeeked is null && GetState(STATE_POSTEDQUIT))
            {
                Debug.WriteLineIf(
                    CompModSwitches.MSOComponentManager.TraceInfo,
                    "ComponentManager : Abnormal loop termination, no WM_QUIT received");
                continueLoop = false;
            }
            else
            {
                switch (uReason)
                {
                    case msoloop.FocusWait:

                        // For focus wait, check to see if we are now the active application.
                        PInvoke.GetWindowThreadProcessId(PInvoke.GetActiveWindow(), out uint pid);
                        if (pid == PInvoke.GetCurrentProcessId())
                        {
                            continueLoop = false;
                        }

                        break;

                    case msoloop.ModalAlert:
                    case msoloop.ModalForm:

                        // For modal forms, check to see if the current active form has been
                        // dismissed.  If there is no active form, then it is an error that
                        // we got into here, so we terminate the loop.

                        if (_currentForm is null || _currentForm.CheckCloseDialog(false))
                        {
                            continueLoop = false;
                        }

                        break;

                    case msoloop.DoEvents:
                    case msoloop.DoEventsModal:
                        // For DoEvents, just see if there are more messages on the queue.
                        MSG temp = default;
                        if (!PInvoke.PeekMessage(&temp, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE))
                        {
                            continueLoop = false;
                        }

                        break;
                }
            }

            return continueLoop;
        }

        BOOL IMsoComponent.FQueryTerminate(BOOL fPromptUser) => true;

        void IMsoComponent.Terminate()
        {
            if (_messageLoopCount > 0 && ComponentManager is not Application.ComponentManager)
            {
                _messageLoopCount--;
            }

            Dispose(false);
        }

        IntPtr IMsoComponent.HwndGetWindow(msocWindow dwWhich, uint dwReserved)
            => IntPtr.Zero;
    }
}
