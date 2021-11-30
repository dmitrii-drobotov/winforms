// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using Xunit;
using static System.Windows.Forms.ListViewItem;
using static Interop;

namespace System.Windows.Forms.Tests
{
    public class ListViewItem_ListViewItemTileAccessibleObjectTests
    {
        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_Ctor_OwnerListViewItemCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ListViewItemTileAccessibleObject(null));
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_Bounds_ReturnsEmptyRectangle_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Rectangle.Empty, accessibleObject.Bounds);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_CurrentIndex_ReturnsMinusOne_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(-1, accessibleObject.CurrentIndex);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_DefaultAction_ReturnsExpected_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(SR.AccessibleActionDoubleClick, accessibleObject.DefaultAction);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_FragmentRoot_ReturnsNull_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.FragmentRoot);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_IsItemSelected_ReturnsFalse_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_ItemSelectionContainer_ReturnsNull_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.ItemSelectionContainer);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_Role_ReturnsExpected_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(AccessibleRole.ListItem, accessibleObject.Role);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_State_ReturnsExpected_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(AccessibleStates.Unavailable, accessibleObject.State);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.NavigateDirection.Parent)]
        [InlineData((int)UiaCore.NavigateDirection.NextSibling)]
        [InlineData((int)UiaCore.NavigateDirection.PreviousSibling)]
        [InlineData((int)UiaCore.NavigateDirection.FirstChild)]
        [InlineData((int)UiaCore.NavigateDirection.LastChild)]
        public void ListViewItemTileAccessibleObject_FragmentNavigate_ReturnsNull_WithoutListView(int navigateDirection)
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.FragmentNavigate((UiaCore.NavigateDirection)navigateDirection));
        }

        [WinFormsTheory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void ListViewItemTileAccessibleObject_GetChild_ReturnsNull_WithoutListView(int childId)
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.GetChild(childId));
        }

        [WinFormsTheory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void ListViewItemTileAccessibleObject_GetChildInternal_ReturnsNull_WithoutListView(int childId)
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.GetChildInternal(childId));
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_GetChildCount_ReturnsMinusOne_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(-1, accessibleObject.GetChildCount());
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_RuntimeId_ReturnsEmptyArray_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Array.Empty<int>(), accessibleObject.RuntimeId);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.ScrollItemPatternId, true)]
        [InlineData((int)UiaCore.UIA.LegacyIAccessiblePatternId, true)]
        [InlineData((int)UiaCore.UIA.SelectionItemPatternId, true)]
        [InlineData((int)UiaCore.UIA.InvokePatternId, true)]
        [InlineData((int)UiaCore.UIA.TogglePatternId, false)]
        public void ListViewItemTileAccessibleObject_IsPatternSupported_ReturnsExpected_WithoutListView(int patternId, bool patternSupported)
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(patternSupported, accessibleObject.IsPatternSupported((UiaCore.UIA)patternId));
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_ToggleState_ReturnExpected_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

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
        public void ListViewItemTileAccessibleObject_GetSubItemBounds_ReturnsEmptyRectangle_WithoutListView(int subItemIndex)
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Rectangle.Empty, accessibleObject.GetSubItemBounds(subItemIndex));
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_AddToSelection_DoesNotThrowException_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.AddToSelection();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_SelectItem_DoesNotThrowException_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.SelectItem();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_Select_DoesNotThrowException_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.Select(AccessibleSelection.AddSelection);

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_DoDefaultAction_DoesNotThrowException_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.DoDefaultAction();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_SetFocus_DoesNotThrowException_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.SetFocus();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_RemoveFromSelection_DoesNotThrowException_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.RemoveFromSelection();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_ScrollIntoView_DoesNotThrowException_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            accessibleObject.ScrollIntoView();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.HasKeyboardFocusPropertyId, false)]
        [InlineData((int)UiaCore.UIA.IsEnabledPropertyId, false)]
        [InlineData((int)UiaCore.UIA.IsKeyboardFocusablePropertyId, false)]
        public void ListViewItemTileAccessibleObject_GetPropertyValue_ReturnsExpected_WithoutListView(int propertyId, object expected)
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(expected, accessibleObject.GetPropertyValue((UiaCore.UIA)propertyId));
        }

        [WinFormsFact]
        public void ListViewItemTileAccessibleObject_NativeWindowHandlePropertyId_ReturnsExpected_WithoutListView()
        {
            ListViewItemTileAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(IntPtr.Zero, accessibleObject.GetPropertyValue(UiaCore.UIA.NativeWindowHandlePropertyId));
        }
    }
}
