﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel.Design;
using System.ComponentModel;
using System.Collections;
using System.Drawing;
using static Interop;

namespace System.Windows.Forms.Design;

/// <summary>
///  This is the designer for the list view control.  It implements hit testing for
///  the items in the list view.
/// </summary>
internal class ListViewDesigner : ControlDesigner
{
    private DesignerActionListCollection _actionLists;
    private ComCtl32.HDHITTESTINFO _hdrhit;
    private bool _inShowErrorDialog;

    /// <summary>
    ///  <para>
    ///  Retrieves a list of associated components.  These are components that should be included in a cut or copy operation on this component.
    ///  </para>
    /// </summary>
    public override ICollection AssociatedComponents
    {
        get
        {
            ListView lv = Control as ListView;
            if (lv is not null)
            {
                return lv.Columns;
            }

            return base.AssociatedComponents;
        }
    }

    private bool OwnerDraw
    {
        get
        {
            return (bool)ShadowProperties[nameof(OwnerDraw)];
        }
        set
        {
            ShadowProperties[nameof(OwnerDraw)] = value;
        }
    }

    private View View
    {
        get
        {
            return ((ListView)Component).View;
        }
        set
        {
            ((ListView)Component).View = value;
            if (value == View.Details)
            {
                HookChildHandles((HWND)Control.Handle);
            }
        }
    }

    protected override unsafe bool GetHitTest(Point point)
    {
        // We override GetHitTest to make the header in report view UI-active.

        ListView listView = (ListView)Component;
        if (listView.View == View.Details)
        {
            Point listViewPoint = Control.PointToClient(point);
            HWND hwndHit = PInvoke.ChildWindowFromPointEx(listView, listViewPoint, CWP_FLAGS.CWP_SKIPINVISIBLE);

            if (!hwndHit.IsNull && hwndHit != listView.Handle)
            {
                HWND headerHwnd = (HWND)PInvoke.SendMessage(listView, PInvoke.LVM_GETHEADER);
                if (hwndHit == headerHwnd)
                {
                    PInvoke.MapWindowPoints(HWND.Null, headerHwnd, ref point);
                    _hdrhit.pt = point;
                    PInvoke.SendMessage(headerHwnd, PInvoke.HDM_HITTEST, 0, ref _hdrhit);
                    if (_hdrhit.flags == HEADER_HITTEST_INFO_FLAGS.HHT_ONDIVIDER)
                        return true;
                }
            }
        }

        return false;
    }

    public override void Initialize(IComponent component)
    {
        ListView lv = (ListView)component;
        OwnerDraw = lv.OwnerDraw;
        lv.OwnerDraw = false;
        lv.UseCompatibleStateImageBehavior = false;

        AutoResizeHandles = true;

        base.Initialize(component);
        if (lv.View == View.Details)
        {
            HookChildHandles((HWND)Control.Handle);
        }
    }

    protected override void PreFilterProperties(IDictionary properties)
    {
        PropertyDescriptor ownerDrawProp = (PropertyDescriptor)properties["OwnerDraw"];

        if (ownerDrawProp is not null)
        {
            properties["OwnerDraw"] = TypeDescriptor.CreateProperty(typeof(ListViewDesigner), ownerDrawProp, Array.Empty<Attribute>());
        }

        PropertyDescriptor viewProp = (PropertyDescriptor)properties["View"];

        if (viewProp is not null)
        {
            properties["View"] = TypeDescriptor.CreateProperty(typeof(ListViewDesigner), viewProp, Array.Empty<Attribute>());
        }

        base.PreFilterProperties(properties);
    }

    protected override unsafe void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case (int)PInvoke.WM_NOTIFY:
            case (int)MessageId.WM_REFLECT_NOTIFY:
                NMHDR* nmhdr = (NMHDR*)(nint)m.LParamInternal;
                if ((int)nmhdr->code == (int)ComCtl32.HDN.ENDTRACKW)
                {
                    // Re-codegen if the columns have been resized
                    try
                    {
                        GetService<IComponentChangeService>().OnComponentChanged(Component);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (_inShowErrorDialog)
                        {
                            return;
                        }

                        _inShowErrorDialog = true;
                        try
                        {
                            ShowErrorDialog(Component.Site.GetService<IUIService>(), ex, (ListView)Component);
                        }
                        finally
                        {
                            _inShowErrorDialog = false;
                        }

                        return;
                    }
                }

                break;
        }

        base.WndProc(ref m);
    }

    public override DesignerActionListCollection ActionLists
    {
        get
        {
            if (_actionLists is null)
            {
                _actionLists = new DesignerActionListCollection();
                _actionLists.Add(new ListViewActionList(this));
            }

            return _actionLists;
        }
    }

    private static void ShowErrorDialog(IUIService uiService, Exception ex, Control control)
    {
        if (uiService is not null)
        {
            uiService.ShowError(ex);
        }
        else
        {
            string message = ex.Message;
            if (message is null || message.Length == 0)
            {
                message = ex.ToString();
            }

            RTLAwareMessageBox.Show(control, message, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1, 0);
        }
    }
}
