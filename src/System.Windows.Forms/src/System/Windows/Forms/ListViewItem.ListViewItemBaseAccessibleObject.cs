// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Accessibility;
using static System.Windows.Forms.ListViewGroup;
using static Interop;

namespace System.Windows.Forms
{
    public partial class ListViewItem
    {
        /// <summary>
        ///  This class contains the base implementation of properties and methods for ListViewItem accessibility objects.
        /// </summary>
        /// <remarks>
        ///  The implementation of this class fully corresponds to the behavior of the ListViewItem accessibility object
        ///  when the ListView is in "LargeIcon" or "SmallIcon" view.
        /// </remarks>
        internal class ListViewItemBaseAccessibleObject : AccessibleObject
        {
            private protected readonly ListViewItem _owningItem;

            public ListViewItemBaseAccessibleObject(ListViewItem owningItem)
            {
                _owningItem = owningItem.OrThrowIfNull();
            }

            private protected ListView? OwningListView => _owningItem.ListView ?? _owningItem.Group?.ListView;

            private protected IAccessible? SystemIAccessible => OwningListView?.AccessibilityObject.GetSystemIAccessibleInternal();

            private protected ListViewGroup? OwningGroup => OwningListView is not null && OwningListView.GroupsDisplayed
                ? _owningItem.Group ?? OwningListView.DefaultGroup
                : null;

            private string AutomationId
                => string.Format("{0}-{1}", typeof(ListViewItem).Name, CurrentIndex);

            public override Rectangle Bounds
                => OwningListView is null || !OwningListView.IsHandleCreated || OwningGroup?.CollapsedState == ListViewGroupCollapsedState.Collapsed
                    ? Rectangle.Empty
                    : new Rectangle(
                        OwningListView.AccessibilityObject.Bounds.X + _owningItem.Bounds.X,
                        OwningListView.AccessibilityObject.Bounds.Y + _owningItem.Bounds.Y,
                        _owningItem.Bounds.Width,
                        _owningItem.Bounds.Height);

            internal int CurrentIndex
                => _owningItem.Index;

            internal override UiaCore.IRawElementProviderFragmentRoot? FragmentRoot
                => OwningListView?.AccessibilityObject;

            internal override bool IsItemSelected
                => (State & AccessibleStates.Selected) != 0;

            public override string? Name
            {
                get => _owningItem.Text;
                set => base.Name = value;
            }

            private bool OwningListItemFocused
            {
                get
                {
                    if (OwningListView is null)
                    {
                        return false;
                    }

                    bool owningListViewFocused = OwningListView.Focused;
                    bool owningListItemFocused = OwningListView.FocusedItem == _owningItem;
                    return owningListViewFocused && owningListItemFocused;
                }
            }

            /// <summary>
            ///  Gets the accessible role.
            /// </summary>
            public override AccessibleRole Role
                => AccessibleRole.ListItem;

            /// <summary>
            ///  Gets the accessible state.
            /// </summary>
            public override AccessibleStates State
            {
                get
                {
                    if (OwningListView is null)
                    {
                        return AccessibleStates.Unavailable;
                    }

                    AccessibleStates state = AccessibleStates.Selectable | AccessibleStates.Focusable | AccessibleStates.MultiSelectable;

                    if (OwningListView.SelectedIndices.Contains(_owningItem.Index))
                    {
                        return state |= AccessibleStates.Selected | AccessibleStates.Focused;
                    }

                    object? systemIAccessibleState = SystemIAccessible?.get_accState(GetChildId());
                    if (systemIAccessibleState is not null)
                    {
                        return state |= (AccessibleStates)systemIAccessibleState;
                    }

                    return state;
                }
            }

            internal override void AddToSelection()
                => SelectItem();

            public override string DefaultAction
                => SR.AccessibleActionDoubleClick;

            public override void DoDefaultAction()
                => SetFocus();

            internal override UiaCore.IRawElementProviderFragment? FragmentNavigate(UiaCore.NavigateDirection direction)
            {
                if (OwningListView is null)
                {
                    return null;
                }

                AccessibleObject _parentInternal = OwningGroup?.AccessibilityObject ?? OwningListView.AccessibilityObject;

                switch (direction)
                {
                    case UiaCore.NavigateDirection.Parent:
                        return _parentInternal;
                    case UiaCore.NavigateDirection.NextSibling:
                        int childIndex = _parentInternal.GetChildIndex(this);
                        return childIndex == InvalidIndex ? null : _parentInternal.GetChild(childIndex + 1);
                    case UiaCore.NavigateDirection.PreviousSibling:
                        return _parentInternal.GetChild(_parentInternal.GetChildIndex(this) - 1);
                    case UiaCore.NavigateDirection.FirstChild:
                    case UiaCore.NavigateDirection.LastChild:
                        return null;
                }

                return base.FragmentNavigate(direction);
            }

            public override AccessibleObject? GetChild(int index)
            {
                if (OwningListView is null)
                {
                    return null;
                }

                if (OwningListView.View == View.Details || OwningListView.View == View.Tile)
                {
                    throw new InvalidOperationException(string.Format(SR.ListViewItemAccessibilityObjectInvalidViewsException,
                        nameof(View.LargeIcon),
                        nameof(View.List),
                        nameof(View.SmallIcon)));
                }

                return null;
            }

            internal virtual AccessibleObject? GetChildInternal(int index) => GetChild(index);

            public override int GetChildCount()
            {
                if (OwningListView is null)
                {
                    return InvalidIndex;
                }

                if (OwningListView.View == View.Details || OwningListView.View == View.Tile)
                {
                    throw new InvalidOperationException(string.Format(SR.ListViewItemAccessibilityObjectInvalidViewsException,
                        nameof(View.LargeIcon),
                        nameof(View.List),
                        nameof(View.SmallIcon)));
                }

                return InvalidIndex;
            }

            internal override int GetChildIndex(AccessibleObject? child) => InvalidIndex;

            internal override object? GetPropertyValue(UiaCore.UIA propertyID)
                => propertyID switch
                {
                    UiaCore.UIA.AutomationIdPropertyId => AutomationId,
                    UiaCore.UIA.ControlTypePropertyId => UiaCore.UIA.ListItemControlTypeId,
                    UiaCore.UIA.HasKeyboardFocusPropertyId => OwningListItemFocused,
                    UiaCore.UIA.IsKeyboardFocusablePropertyId => (State & AccessibleStates.Focusable) == AccessibleStates.Focusable,
                    UiaCore.UIA.IsEnabledPropertyId => OwningListView?.Enabled ?? false,
                    UiaCore.UIA.IsOffscreenPropertyId => OwningGroup?.CollapsedState == ListViewGroupCollapsedState.Collapsed
                                                        || (bool)(base.GetPropertyValue(UiaCore.UIA.IsOffscreenPropertyId) ?? false),
                    UiaCore.UIA.NativeWindowHandlePropertyId => OwningListView?.Handle ?? IntPtr.Zero,
                    _ => base.GetPropertyValue(propertyID)
                };

            internal virtual Rectangle GetSubItemBounds(int subItemIndex) => Rectangle.Empty;

            internal override int[] RuntimeId
            {
                get
                {
                    if (OwningListView is null)
                    {
                        return Array.Empty<int>();
                    }

                    var owningListViewRuntimeId = OwningListView.AccessibilityObject.RuntimeId;

                    Debug.Assert(owningListViewRuntimeId.Length >= 2);

                    if (OwningGroup is not null)
                    {
                        return new int[]
                        {
                            owningListViewRuntimeId[0],
                            owningListViewRuntimeId[1],
                            4, // Win32-control specific RuntimeID constant, is used in similar Win32 controls and is used in WinForms controls for consistency.
                            OwningGroup.AccessibilityObject is ListViewGroupAccessibleObject listViewGroupAccessibleObject
                                            ? listViewGroupAccessibleObject.CurrentIndex
                                            : InvalidIndex,
                            CurrentIndex
                        };
                    }

                    return new int[]
                    {
                        owningListViewRuntimeId[0],
                        owningListViewRuntimeId[1],
                        4, // Win32-control specific RuntimeID constant.
                        CurrentIndex
                    };
                }
            }

            internal override UiaCore.ToggleState ToggleState
                => _owningItem.Checked
                    ? UiaCore.ToggleState.On
                    : UiaCore.ToggleState.Off;

            /// <summary>
            ///  Indicates whether specified pattern is supported.
            /// </summary>
            /// <param name="patternId">The pattern ID.</param>
            /// <returns>True if specified </returns>
            internal override bool IsPatternSupported(UiaCore.UIA patternId)
                => patternId switch
                {
                    UiaCore.UIA.ScrollItemPatternId => true,
                    UiaCore.UIA.LegacyIAccessiblePatternId => true,
                    UiaCore.UIA.SelectionItemPatternId => true,
                    UiaCore.UIA.InvokePatternId => true,
                    UiaCore.UIA.TogglePatternId => OwningListView?.CheckBoxes ?? false,
                    _ => base.IsPatternSupported(patternId)
                };

            internal override void RemoveFromSelection()
            {
                // Do nothing, C++ implementation returns UIA_E_INVALIDOPERATION 0x80131509
            }

            internal override UiaCore.IRawElementProviderSimple? ItemSelectionContainer
                => OwningListView?.AccessibilityObject;

            internal override void ScrollIntoView() => _owningItem.EnsureVisible();

            internal unsafe override void SelectItem()
            {
                if (OwningListView is null || !OwningListView.IsHandleCreated)
                {
                    return;
                }

                OwningListView.SelectedIndices.Add(CurrentIndex);
                User32.InvalidateRect(new HandleRef(this, OwningListView.Handle), lpRect: null, bErase: BOOL.FALSE);

                RaiseAutomationEvent(UiaCore.UIA.AutomationFocusChangedEventId);
                RaiseAutomationEvent(UiaCore.UIA.SelectionItem_ElementSelectedEventId);
            }

            internal override void SetFocus()
            {
                RaiseAutomationEvent(UiaCore.UIA.AutomationFocusChangedEventId);
                SelectItem();
            }

            public override void Select(AccessibleSelection flags)
            {
                if (OwningListView is null || !OwningListView.IsHandleCreated)
                {
                    return;
                }

                try
                {
                    SystemIAccessible?.accSelect((int)flags, GetChildId());
                }
                catch (ArgumentException)
                {
                    // In Everett, the ListBox accessible children did not have any selection capability.
                    // In Whidbey, they delegate the selection capability to OLEACC.
                    // However, OLEACC does not deal w/ several Selection flags: ExtendSelection, AddSelection, RemoveSelection.
                    // OLEACC instead throws an ArgumentException.
                    // Since Whidbey API's should not throw an exception in places where Everett API's did not, we catch
                    // the ArgumentException and fail silently.
                }
            }

            internal override void Toggle()
            {
                _owningItem.Checked = !_owningItem.Checked;
            }
        }
    }
}
