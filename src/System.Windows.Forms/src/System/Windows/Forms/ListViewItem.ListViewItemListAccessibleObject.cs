// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing;
using static Interop;

namespace System.Windows.Forms
{
    public partial class ListViewItem
    {
        internal class ListViewItemListAccessibleObject : ListViewItemBaseAccessibleObject
        {
            public ListViewItemListAccessibleObject(ListViewItem owningItem) : base(owningItem)
            {
            }

            public override Rectangle Bounds
                => OwningListView is null || !OwningListView.IsHandleCreated
                    ? Rectangle.Empty
                    : new Rectangle(
                        OwningListView.AccessibilityObject.Bounds.X + _owningItem.Bounds.X,
                        OwningListView.AccessibilityObject.Bounds.Y + _owningItem.Bounds.Y,
                        _owningItem.Bounds.Width,
                        _owningItem.Bounds.Height);

            internal override UiaCore.IRawElementProviderFragment? FragmentNavigate(UiaCore.NavigateDirection direction)
            {
                if (OwningListView is null)
                {
                    return null;
                }

                AccessibleObject _parentInternal = OwningListView.AccessibilityObject;

                switch (direction)
                {
                    case UiaCore.NavigateDirection.Parent:
                        return _parentInternal;
                    case UiaCore.NavigateDirection.NextSibling:
                        int childIndex = _parentInternal.GetChildIndex(this);
                        return childIndex == InvalidIndex ? null : _parentInternal.GetChild(childIndex + 1);
                    case UiaCore.NavigateDirection.PreviousSibling:
                        return _parentInternal.GetChild(_parentInternal.GetChildIndex(this) - 1);
                }

                return base.FragmentNavigate(direction);
            }

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

                    return new int[]
                    {
                        owningListViewRuntimeId[0],
                        owningListViewRuntimeId[1],
                        4, // Win32-control specific RuntimeID constant.
                        CurrentIndex
                    };
                }
            }
        }
    }
}
