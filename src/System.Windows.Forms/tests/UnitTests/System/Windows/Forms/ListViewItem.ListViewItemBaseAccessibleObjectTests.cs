// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Windows.Forms.TestUtilities;
using Xunit;
using static System.Windows.Forms.ListViewItem;
using static Interop;

namespace System.Windows.Forms.Tests
{
    public class ListViewItem_ListViewItemBaseAccessibleObjectTests
    {
        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_Ctor_OwnerListViewItemCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ListViewItemBaseAccessibleObject(null));
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_Ctor_OwnerListViewItemWithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.NotNull(listViewItem.AccessibilityObject);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_Role_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            Assert.Equal(AccessibleRole.ListItem, item.AccessibilityObject.Role);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_DefaultAction_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            Assert.Equal(SR.AccessibleActionDoubleClick, item.AccessibilityObject.DefaultAction);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_CurrentIndex_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            var accessibleObject = (ListViewItemBaseAccessibleObject)item.AccessibilityObject;

            Assert.Equal(item.Index, accessibleObject.CurrentIndex);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_FragmentRoot_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            Assert.Equal(control.AccessibilityObject, item.AccessibilityObject.FragmentRoot);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsTheory]
        [CommonMemberData(typeof(CommonTestHelper), nameof(CommonTestHelper.GetBoolTheoryData))]
        public void ListViewItemBaseAccessibleObject_IsItemSelected_ReturnsExpected(bool isSelected)
        {
            using ListView control = new();
            ListViewItem item = new() { Selected = isSelected };
            control.Items.Add(item);

            Assert.Equal(isSelected, item.AccessibilityObject.IsItemSelected);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_DoDefaultAction_DoesNothing_IfControlIsNotCreated()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.False((accessibleObject.State & AccessibleStates.Selected) != 0);

            accessibleObject.DoDefaultAction();

            Assert.False((accessibleObject.State & AccessibleStates.Selected) != 0);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_AddToSelection_WorksExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);
            control.CreateControl();

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.AddToSelection();

            Assert.True(accessibleObject.IsItemSelected);
            Assert.True(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_DoDefaultAction_IfControlIsNotCreated()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.False((accessibleObject.State & AccessibleStates.Selected) != 0);

            accessibleObject.DoDefaultAction();

            Assert.False((accessibleObject.State & AccessibleStates.Selected) != 0);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_DoDefaultAction_WorksExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);
            control.CreateControl();

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.False((accessibleObject.State & AccessibleStates.Selected) != 0);

            accessibleObject.DoDefaultAction();

            Assert.True((accessibleObject.State & AccessibleStates.Selected) != 0);
            Assert.True(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_FragmentNavigate_Parent_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;
            var actual = accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.Parent);

            Assert.Equal(control.AccessibilityObject, actual);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_FragmentNavigate_ToSibling_ReturnsNull()
        {
            using ListView control = new();
            control.Items.AddRange(new ListViewItem[] { new(), new(), new() });

            AccessibleObject accessibleObject1 = control.Items[0].AccessibilityObject;
            AccessibleObject accessibleObject2 = control.Items[1].AccessibilityObject;

            Assert.Null(accessibleObject1.FragmentNavigate(UiaCore.NavigateDirection.NextSibling));
            Assert.Null(accessibleObject2.FragmentNavigate(UiaCore.NavigateDirection.NextSibling));
            Assert.Null(accessibleObject1.FragmentNavigate(UiaCore.NavigateDirection.PreviousSibling));
            Assert.Null(accessibleObject2.FragmentNavigate(UiaCore.NavigateDirection.PreviousSibling));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_FragmentNavigate_Child_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.Null(accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.FirstChild));
            Assert.Null(accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.LastChild));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsTheory]
        [InlineData(View.Details)]
        [InlineData(View.Tile)]
        [InlineData(View.List)]
        [InlineData(View.SmallIcon)]
        [InlineData(View.LargeIcon)]
        public void ListViewItemBaseAccessibleObject_GetChild_ReturnsNull_IfViewIsNotDetailsOrTile(View view)
        {
            using ListView control = new() { View = view };
            ListViewItem item = new();
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.Null(item.AccessibilityObject.GetChild(0));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsTheory]
        [InlineData(View.Details)]
        [InlineData(View.Tile)]
        [InlineData(View.List)]
        [InlineData(View.SmallIcon)]
        [InlineData(View.LargeIcon)]
        public void ListViewItemBaseAccessibleObject_GetChildCount_ReturnsNull_IfViewIsNotDetailsOrTile(View view)
        {
            using ListView control = new() { View = view };
            ListViewItem item = new();
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.Equal(-1, item.AccessibilityObject.GetChildCount());
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_GetSubItemBounds_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            var accessibleObject = (ListViewItemBaseAccessibleObject)item.AccessibilityObject;

            Assert.Equal(Rectangle.Empty, accessibleObject.GetSubItemBounds(0));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_GetPropertyValue_ControlType_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            var actual = item.AccessibilityObject.GetPropertyValue(UiaCore.UIA.ControlTypePropertyId);

            Assert.Equal(UiaCore.UIA.ListItemControlTypeId, actual);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_GetPropertyValue_FrameworkProperty_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            var actual = item.AccessibilityObject.GetPropertyValue(UiaCore.UIA.FrameworkIdPropertyId);

            Assert.Equal(NativeMethods.WinFormFrameworkId, actual);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_GetPropertyValue_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            Assert.Equal(SR.AccessibleActionDoubleClick, item.AccessibilityObject.GetPropertyValue(UiaCore.UIA.LegacyIAccessibleDefaultActionPropertyId));
            Assert.Null(item.AccessibilityObject.GetPropertyValue(UiaCore.UIA.ValueValuePropertyId));
            Assert.True((bool)item.AccessibilityObject.GetPropertyValue(UiaCore.UIA.IsInvokePatternAvailablePropertyId));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.ScrollItemPatternId)]
        [InlineData((int)UiaCore.UIA.LegacyIAccessiblePatternId)]
        [InlineData((int)UiaCore.UIA.SelectionItemPatternId)]
        [InlineData((int)UiaCore.UIA.InvokePatternId)]
        [InlineData((int)UiaCore.UIA.TogglePatternId)]
        public void ListViewItemBaseAccessibleObject_IsPatternSupported_ReturnsExpected(int patternId)
        {
            using ListView control = new() { CheckBoxes = true };
            ListViewItem item = new();
            control.Items.Add(item);

            Assert.True(item.AccessibilityObject.IsPatternSupported((UiaCore.UIA)patternId));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_ItemSelectionContainer_ReturnsExpected()
        {
            using ListView control = new();
            ListViewItem item = new();
            control.Items.Add(item);

            Assert.Equal(control.AccessibilityObject, item.AccessibilityObject.ItemSelectionContainer);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsTheory]
        [InlineData(true, (int)UiaCore.ToggleState.On)]
        [InlineData(false, (int)UiaCore.ToggleState.Off)]
        public void ListViewItemBaseAccessibleObject_ToggleState_ReturnsExpected(bool isChecked, int expected)
        {
            using ListView control = new();
            ListViewItem item = new() { Checked = isChecked };
            control.Items.Add(item);

            Assert.Equal((UiaCore.ToggleState)expected, item.AccessibilityObject.ToggleState);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsTheory]
        [InlineData(false, (int)UiaCore.ToggleState.Off, (int)UiaCore.ToggleState.On)]
        [InlineData(true, (int)UiaCore.ToggleState.On, (int)UiaCore.ToggleState.Off)]
        public void ListViewItemBaseAccessibleObject_Toggle_WorksExpected(bool isChecked, int before, int expected)
        {
            using ListView control = new();
            ListViewItem item = new() { Checked = isChecked };
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.Equal((UiaCore.ToggleState)before, accessibleObject.ToggleState);

            accessibleObject.Toggle();

            Assert.Equal((UiaCore.ToggleState)expected, accessibleObject.ToggleState);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_Bounds_ReturnsEmptyRectangle_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Equal(Rectangle.Empty, listViewItem.AccessibilityObject.Bounds);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_CurrentIndex_ReturnsMinusOne_WithoutListView()
        {
            ListViewItem listViewItem = new();
            ListViewItemBaseAccessibleObject accessibleObject = (ListViewItemBaseAccessibleObject)listViewItem.AccessibilityObject;

            Assert.Equal(-1, accessibleObject.CurrentIndex);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_DefaultAction_ReturnsExpected_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Equal(SR.AccessibleActionDoubleClick, listViewItem.AccessibilityObject.DefaultAction);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_FragmentRoot_ReturnsNull_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Null(listViewItem.AccessibilityObject.FragmentRoot);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_IsItemSelected_ReturnsFalse_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.False(listViewItem.AccessibilityObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_ItemSelectionContainer_ReturnsNull_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Null(listViewItem.AccessibilityObject.ItemSelectionContainer);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_Role_ReturnsExpected_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Equal(AccessibleRole.ListItem, listViewItem.AccessibilityObject.Role);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_State_ReturnsExpected_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Equal(AccessibleStates.Unavailable, listViewItem.AccessibilityObject.State);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.NavigateDirection.Parent)]
        [InlineData((int)UiaCore.NavigateDirection.NextSibling)]
        [InlineData((int)UiaCore.NavigateDirection.PreviousSibling)]
        [InlineData((int)UiaCore.NavigateDirection.FirstChild)]
        [InlineData((int)UiaCore.NavigateDirection.LastChild)]
        public void ListViewItemBaseAccessibleObject_FragmentNavigate_ReturnsNull_WithoutListView(int navigateDirection)
        {
            ListViewItem listViewItem = new();

            Assert.Null(listViewItem.AccessibilityObject.FragmentNavigate((UiaCore.NavigateDirection)navigateDirection));
        }

        [WinFormsTheory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void ListViewItemBaseAccessibleObject_GetChild_ReturnsNull_WithoutListView(int childId)
        {
            ListViewItem listViewItem = new();

            Assert.Null(listViewItem.AccessibilityObject.GetChild(childId));
        }

        [WinFormsTheory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void ListViewItemBaseAccessibleObject_GetChildInternal_ReturnsNull_WithoutListView(int childId)
        {
            ListViewItem listViewItem = new();
            ListViewItemBaseAccessibleObject accessibleObject = (ListViewItemBaseAccessibleObject)listViewItem.AccessibilityObject;

            Assert.Null(accessibleObject.GetChildInternal(childId));
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_GetChildCount_ReturnsMinusOne_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Equal(-1, listViewItem.AccessibilityObject.GetChildCount());
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_RuntimeId_ReturnsEmptyArray_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Equal(Array.Empty<int>(), listViewItem.AccessibilityObject.RuntimeId);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.ScrollItemPatternId, true)]
        [InlineData((int)UiaCore.UIA.LegacyIAccessiblePatternId, true)]
        [InlineData((int)UiaCore.UIA.SelectionItemPatternId, true)]
        [InlineData((int)UiaCore.UIA.InvokePatternId, true)]
        [InlineData((int)UiaCore.UIA.TogglePatternId, false)]
        public void ListViewItemBaseAccessibleObject_IsPatternSupported_ReturnsExpected_WithoutListView(int patternId, bool patternSupported)
        {
            ListViewItem listViewItem = new();

            Assert.Equal(patternSupported, listViewItem.AccessibilityObject.IsPatternSupported((UiaCore.UIA)patternId));
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_ToggleState_ReturnExpected_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Equal(UiaCore.ToggleState.Off, listViewItem.AccessibilityObject.ToggleState);

            listViewItem.AccessibilityObject.Toggle();

            Assert.Equal(UiaCore.ToggleState.On, listViewItem.AccessibilityObject.ToggleState);

            listViewItem.AccessibilityObject.Toggle();

            Assert.Equal(UiaCore.ToggleState.Off, listViewItem.AccessibilityObject.ToggleState);
        }

        [WinFormsTheory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ListViewItemBaseAccessibleObject_GetSubItemBounds_ReturnsEmptyRectangle_WithoutListView(int subItemIndex)
        {
            ListViewItem listViewItem = new();
            ListViewItemBaseAccessibleObject accessibleObject = (ListViewItemBaseAccessibleObject)listViewItem.AccessibilityObject;

            Assert.Equal(Rectangle.Empty, accessibleObject.GetSubItemBounds(subItemIndex));
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_AddToSelection_DoesNotThrowException_WithoutListView()
        {
            ListViewItem listViewItem = new();
            AccessibleObject accessibleObject = listViewItem.AccessibilityObject;

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.AddToSelection();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_SelectItem_DoesNotThrowException_WithoutListView()
        {
            ListViewItem listViewItem = new();
            AccessibleObject accessibleObject = listViewItem.AccessibilityObject;

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.SelectItem();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_Select_DoesNotThrowException_WithoutListView()
        {
            ListViewItem listViewItem = new();
            AccessibleObject accessibleObject = listViewItem.AccessibilityObject;

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.Select(AccessibleSelection.AddSelection);

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_DoDefaultAction_DoesNotThrowException_WithoutListView()
        {
            ListViewItem listViewItem = new();
            AccessibleObject accessibleObject = listViewItem.AccessibilityObject;

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.DoDefaultAction();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_SetFocus_DoesNotThrowException_WithoutListView()
        {
            ListViewItem listViewItem = new();
            AccessibleObject accessibleObject = listViewItem.AccessibilityObject;

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.SetFocus();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_RemoveFromSelection_DoesNotThrowException_WithoutListView()
        {
            ListViewItem listViewItem = new();
            AccessibleObject accessibleObject = listViewItem.AccessibilityObject;

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.RemoveFromSelection();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_ScrollIntoView_DoesNotThrowException_WithoutListView()
        {
            ListViewItem listViewItem = new();
            AccessibleObject accessibleObject = listViewItem.AccessibilityObject;

            accessibleObject.ScrollIntoView();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.HasKeyboardFocusPropertyId, false)]
        [InlineData((int)UiaCore.UIA.IsEnabledPropertyId, false)]
        [InlineData((int)UiaCore.UIA.IsKeyboardFocusablePropertyId, false)]
        public void ListViewItemBaseAccessibleObject_GetPropertyValue_ReturnsExpected_WithoutListView(int propertyId, object expected)
        {
            ListViewItem listViewItem = new();

            Assert.Equal(expected, listViewItem.AccessibilityObject.GetPropertyValue((UiaCore.UIA)propertyId));
        }

        [WinFormsFact]
        public void ListViewItemBaseAccessibleObject_NativeWindowHandlePropertyId_ReturnsExpected_WithoutListView()
        {
            ListViewItem listViewItem = new();

            Assert.Equal(IntPtr.Zero, listViewItem.AccessibilityObject.GetPropertyValue(UiaCore.UIA.NativeWindowHandlePropertyId));
        }
    }
}
