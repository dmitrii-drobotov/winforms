// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using Xunit;
using static System.Windows.Forms.ListViewItem;
using static Interop;

namespace System.Windows.Forms.Tests
{
    public class ListViewItem_ListViewItemDetailsAccessibleObjectTests
    {
        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_Ctor_OwnerListViewItemCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ListViewItemDetailsAccessibleObject(null));
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_FragmentNavigate_FirstChild_ReturnsExpected()
        {
            using ListView control = new() { View = View.Details };
            ListViewItem item = new();
            control.Columns.Add(new ColumnHeader());
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;
            AccessibleObject expected = item.SubItems[0].AccessibilityObject;

            Assert.Equal(expected, accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.FirstChild));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_FragmentNavigate_LastChild_ReturnsExpected()
        {
            using ListView control = new() { View = View.Details };
            ListViewItem item = new();
            control.Items.Add(item);
            control.Columns.AddRange(new ColumnHeader[] { new(), new(), new() });
            item.SubItems.AddRange(new ListViewSubItem[] { new(), new(), new(), new(), new() });

            AccessibleObject accessibleObject = item.AccessibilityObject;
            AccessibleObject expected = item.SubItems[control.Columns.Count - 1].AccessibilityObject;

            Assert.Equal(expected, accessibleObject.FragmentNavigate(UiaCore.NavigateDirection.LastChild));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_GetChild_ReturnsNull_IfIndexInvalid()
        {
            using ListView control = new() { View = View.Details };
            ListViewItem item = new();
            control.Columns.AddRange(new ColumnHeader[] { new(), new(), new() });
            int outRangeIndex = control.Columns.Count + 1;
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.Null(accessibleObject.GetChild(-1));
            Assert.Null(accessibleObject.GetChild(outRangeIndex));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_GetChild_ReturnsExpected()
        {
            using ListView control = new() { View = View.Details };
            ListViewItem item = new();
            control.Items.Add(item);
            control.Columns.AddRange(new ColumnHeader[] { new(), new(), new() });
            item.SubItems.AddRange(new ListViewSubItem[] { new(), new(), new(), new() });

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.Equal(item.SubItems[0].AccessibilityObject, accessibleObject.GetChild(0));
            Assert.Equal(item.SubItems[1].AccessibilityObject, accessibleObject.GetChild(1));
            Assert.Equal(item.SubItems[2].AccessibilityObject, accessibleObject.GetChild(2));
            Assert.Null(accessibleObject.GetChild(3));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_GetChildCount_ReturnsExpected_IfControlIsNotCreated()
        {
            using ListView control = new() { View = View.Details };
            ListViewItem item = new();
            control.Items.Add(item);

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.Equal(-1, accessibleObject.GetChildCount());
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_GetChildCount_ReturnsExpected()
        {
            using ListView control = new() { View = View.Details };
            ListViewItem item = new();
            control.Items.Add(item);
            control.Columns.AddRange(new ColumnHeader[] { new(), new(), new() });
            control.CreateControl();

            AccessibleObject accessibleObject = item.AccessibilityObject;

            Assert.Equal(control.Columns.Count, accessibleObject.GetChildCount());
            Assert.True(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_Bounds_ReturnsEmptyRectangle_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Rectangle.Empty, accessibleObject.Bounds);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_CurrentIndex_ReturnsMinusOne_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(-1, accessibleObject.CurrentIndex);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_DefaultAction_ReturnsExpected_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(SR.AccessibleActionDoubleClick, accessibleObject.DefaultAction);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_FragmentRoot_ReturnsNull_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.FragmentRoot);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_IsItemSelected_ReturnsFalse_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_ItemSelectionContainer_ReturnsNull_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.ItemSelectionContainer);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_Role_ReturnsExpected_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(AccessibleRole.ListItem, accessibleObject.Role);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_State_ReturnsExpected_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(AccessibleStates.Unavailable, accessibleObject.State);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.NavigateDirection.Parent)]
        [InlineData((int)UiaCore.NavigateDirection.NextSibling)]
        [InlineData((int)UiaCore.NavigateDirection.PreviousSibling)]
        [InlineData((int)UiaCore.NavigateDirection.FirstChild)]
        [InlineData((int)UiaCore.NavigateDirection.LastChild)]
        public void ListViewItemDetailsAccessibleObject_FragmentNavigate_ReturnsNull_WithoutListView(int navigateDirection)
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.FragmentNavigate((UiaCore.NavigateDirection)navigateDirection));
        }

        [WinFormsTheory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void ListViewItemDetailsAccessibleObject_GetChild_ReturnsNull_WithoutListView(int childId)
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.GetChild(childId));
        }

        [WinFormsTheory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void ListViewItemDetailsAccessibleObject_GetChildInternal_ReturnsNull_WithoutListView(int childId)
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.GetChildInternal(childId));
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_GetChildCount_ReturnsMinusOne_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(-1, accessibleObject.GetChildCount());
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_RuntimeId_ReturnsEmptyArray_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Array.Empty<int>(), accessibleObject.RuntimeId);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.ScrollItemPatternId, true)]
        [InlineData((int)UiaCore.UIA.LegacyIAccessiblePatternId, true)]
        [InlineData((int)UiaCore.UIA.SelectionItemPatternId, true)]
        [InlineData((int)UiaCore.UIA.InvokePatternId, true)]
        [InlineData((int)UiaCore.UIA.TogglePatternId, false)]
        public void ListViewItemDetailsAccessibleObject_IsPatternSupported_ReturnsExpected_WithoutListView(int patternId, bool patternSupported)
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(patternSupported, accessibleObject.IsPatternSupported((UiaCore.UIA)patternId));
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_ToggleState_ReturnExpected_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(UiaCore.ToggleState.Off, accessibleObject.ToggleState);

            accessibleObject.Toggle();

            Assert.Equal(UiaCore.ToggleState.On, accessibleObject.ToggleState);

            accessibleObject.Toggle();

            Assert.Equal(UiaCore.ToggleState.Off, accessibleObject.ToggleState);
        }

        [WinFormsTheory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ListViewItemDetailsAccessibleObject_GetSubItemBounds_ReturnsEmptyRectangle_WithoutListView(int subItemIndex)
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Rectangle.Empty, accessibleObject.GetSubItemBounds(subItemIndex));
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_AddToSelection_DoesNotThrowException_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.AddToSelection();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_SelectItem_DoesNotThrowException_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.SelectItem();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_Select_DoesNotThrowException_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.Select(AccessibleSelection.AddSelection);

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_DoDefaultAction_DoesNotThrowException_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.DoDefaultAction();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_SetFocus_DoesNotThrowException_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.SetFocus();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_RemoveFromSelection_DoesNotThrowException_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.RemoveFromSelection();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_ScrollIntoView_DoesNotThrowException_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            accessibleObject.ScrollIntoView();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.HasKeyboardFocusPropertyId, false)]
        [InlineData((int)UiaCore.UIA.IsEnabledPropertyId, false)]
        [InlineData((int)UiaCore.UIA.IsKeyboardFocusablePropertyId, false)]
        public void ListViewItemDetailsAccessibleObject_GetPropertyValue_ReturnsExpected_WithoutListView(int propertyId, object expected)
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(expected, accessibleObject.GetPropertyValue((UiaCore.UIA)propertyId));
        }

        [WinFormsFact]
        public void ListViewItemDetailsAccessibleObject_NativeWindowHandlePropertyId_ReturnsExpected_WithoutListView()
        {
            ListViewItemDetailsAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(IntPtr.Zero, accessibleObject.GetPropertyValue(UiaCore.UIA.NativeWindowHandlePropertyId));
        }
    }
}
