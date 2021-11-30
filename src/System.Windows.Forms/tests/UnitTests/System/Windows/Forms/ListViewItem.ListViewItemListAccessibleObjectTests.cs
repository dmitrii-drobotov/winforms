// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using Xunit;
using static System.Windows.Forms.ListViewItem;
using static Interop;

namespace System.Windows.Forms.Tests
{
    public class ListViewItem_ListViewItemListAccessibleObjectTests
    {
        [WinFormsFact]
        public void ListViewItemListAccessibleObject_Ctor_OwnerListViewItemCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ListViewItemListAccessibleObject(null));
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_Bounds_IsEmptyRectangle_IfOwningControlNotCreated()
        {
            using ListView control = new();
            control.View = View.List;
            control.Items.Add(new ListViewItem());

            Assert.Equal(Rectangle.Empty, control.Items[0].AccessibilityObject.Bounds);
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_FragmentNavigate_Parent()
        {
            using ListView control = new();
            control.View = View.List;
            control.Items.Add(new ListViewItem());
            AccessibleObject accessibleObject1 = control.Items[0].AccessibilityObject;

            Assert.Equal(control.AccessibilityObject, accessibleObject1.FragmentNavigate(UiaCore.NavigateDirection.Parent));
            Assert.False(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_FragmentNavigate_PreviousSibling()
        {
            using ListView control = new();
            control.View = View.List;
            control.Items.AddRange(new ListViewItem[] { new(), new(), new() });
            control.CreateControl();

            AccessibleObject accessibleObject1 = control.Items[0].AccessibilityObject;
            AccessibleObject accessibleObject2 = control.Items[1].AccessibilityObject;
            AccessibleObject accessibleObject3 = control.Items[2].AccessibilityObject;

            Assert.Null(accessibleObject1.FragmentNavigate(UiaCore.NavigateDirection.PreviousSibling));
            Assert.Equal(accessibleObject1, accessibleObject2.FragmentNavigate(UiaCore.NavigateDirection.PreviousSibling));
            Assert.Equal(accessibleObject2, accessibleObject3.FragmentNavigate(UiaCore.NavigateDirection.PreviousSibling));

            Assert.True(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_FragmentNavigate_NextSibling()
        {
            using ListView control = new();
            control.View = View.List;
            control.Items.AddRange(new ListViewItem[] { new(), new(), new() });
            control.CreateControl();

            AccessibleObject accessibleObject1 = control.Items[0].AccessibilityObject;
            AccessibleObject accessibleObject2 = control.Items[1].AccessibilityObject;
            AccessibleObject accessibleObject3 = control.Items[2].AccessibilityObject;

            Assert.Equal(accessibleObject2, accessibleObject1.FragmentNavigate(UiaCore.NavigateDirection.NextSibling));
            Assert.Equal(accessibleObject3, accessibleObject2.FragmentNavigate(UiaCore.NavigateDirection.NextSibling));
            Assert.Null(accessibleObject3.FragmentNavigate(UiaCore.NavigateDirection.NextSibling));

            Assert.True(control.IsHandleCreated);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_Bounds_ReturnsEmptyRectangle_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Rectangle.Empty, accessibleObject.Bounds);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_CurrentIndex_ReturnsMinusOne_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(-1, accessibleObject.CurrentIndex);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_DefaultAction_ReturnsExpected_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(SR.AccessibleActionDoubleClick, accessibleObject.DefaultAction);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_FragmentRoot_ReturnsNull_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.FragmentRoot);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_IsItemSelected_ReturnsFalse_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_ItemSelectionContainer_ReturnsNull_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.ItemSelectionContainer);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_Role_ReturnsExpected_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(AccessibleRole.ListItem, accessibleObject.Role);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_State_ReturnsExpected_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(AccessibleStates.Unavailable, accessibleObject.State);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.NavigateDirection.Parent)]
        [InlineData((int)UiaCore.NavigateDirection.NextSibling)]
        [InlineData((int)UiaCore.NavigateDirection.PreviousSibling)]
        [InlineData((int)UiaCore.NavigateDirection.FirstChild)]
        [InlineData((int)UiaCore.NavigateDirection.LastChild)]
        public void ListViewItemListAccessibleObject_FragmentNavigate_ReturnsNull_WithoutListView(int navigateDirection)
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.FragmentNavigate((UiaCore.NavigateDirection)navigateDirection));
        }

        [WinFormsTheory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void ListViewItemListAccessibleObject_GetChild_ReturnsNull_WithoutListView(int childId)
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.GetChild(childId));
        }

        [WinFormsTheory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void ListViewItemListAccessibleObject_GetChildInternal_ReturnsNull_WithoutListView(int childId)
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Null(accessibleObject.GetChildInternal(childId));
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_GetChildCount_ReturnsMinusOne_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(-1, accessibleObject.GetChildCount());
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_RuntimeId_ReturnsEmptyArray_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Array.Empty<int>(), accessibleObject.RuntimeId);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.ScrollItemPatternId, true)]
        [InlineData((int)UiaCore.UIA.LegacyIAccessiblePatternId, true)]
        [InlineData((int)UiaCore.UIA.SelectionItemPatternId, true)]
        [InlineData((int)UiaCore.UIA.InvokePatternId, true)]
        [InlineData((int)UiaCore.UIA.TogglePatternId, false)]
        public void ListViewItemListAccessibleObject_IsPatternSupported_ReturnsExpected_WithoutListView(int patternId, bool patternSupported)
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(patternSupported, accessibleObject.IsPatternSupported((UiaCore.UIA)patternId));
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_ToggleState_ReturnExpected_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

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
        public void ListViewItemListAccessibleObject_GetSubItemBounds_ReturnsEmptyRectangle_WithoutListView(int subItemIndex)
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(Rectangle.Empty, accessibleObject.GetSubItemBounds(subItemIndex));
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_AddToSelection_DoesNotThrowException_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.AddToSelection();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_SelectItem_DoesNotThrowException_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.SelectItem();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_Select_DoesNotThrowException_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.Select(AccessibleSelection.AddSelection);

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_DoDefaultAction_DoesNotThrowException_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.DoDefaultAction();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_SetFocus_DoesNotThrowException_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.SetFocus();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_RemoveFromSelection_DoesNotThrowException_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.False(accessibleObject.IsItemSelected);

            accessibleObject.RemoveFromSelection();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_ScrollIntoView_DoesNotThrowException_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            accessibleObject.ScrollIntoView();

            Assert.False(accessibleObject.IsItemSelected);
        }

        [WinFormsTheory]
        [InlineData((int)UiaCore.UIA.HasKeyboardFocusPropertyId, false)]
        [InlineData((int)UiaCore.UIA.IsEnabledPropertyId, false)]
        [InlineData((int)UiaCore.UIA.IsKeyboardFocusablePropertyId, false)]
        public void ListViewItemListAccessibleObject_GetPropertyValue_ReturnsExpected_WithoutListView(int propertyId, object expected)
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(expected, accessibleObject.GetPropertyValue((UiaCore.UIA)propertyId));
        }

        [WinFormsFact]
        public void ListViewItemListAccessibleObject_NativeWindowHandlePropertyId_ReturnsExpected_WithoutListView()
        {
            ListViewItemListAccessibleObject accessibleObject = new(new ListViewItem());

            Assert.Equal(IntPtr.Zero, accessibleObject.GetPropertyValue(UiaCore.UIA.NativeWindowHandlePropertyId));
        }
    }
}
