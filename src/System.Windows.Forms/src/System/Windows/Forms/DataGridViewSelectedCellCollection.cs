﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;

namespace System.Windows.Forms;

/// <summary>
///  Represents a collection of selected <see cref="DataGridViewCell"/> objects in the <see cref="DataGridView"/>
///  control.
/// </summary>
[ListBindable(false)]
public class DataGridViewSelectedCellCollection : BaseCollection, IList
{
    private readonly List<DataGridViewCell> _items = new();

    int IList.Add(object? value)
    {
        throw new NotSupportedException(SR.DataGridView_ReadOnlyCollection);
    }

    void IList.Clear()
    {
        throw new NotSupportedException(SR.DataGridView_ReadOnlyCollection);
    }

    bool IList.Contains(object? value) => ((IList)_items).Contains(value);

    int IList.IndexOf(object? value) => ((IList)_items).IndexOf(value);

    void IList.Insert(int index, object? value)
    {
        throw new NotSupportedException(SR.DataGridView_ReadOnlyCollection);
    }

    void IList.Remove(object? value)
    {
        throw new NotSupportedException(SR.DataGridView_ReadOnlyCollection);
    }

    void IList.RemoveAt(int index)
    {
        throw new NotSupportedException(SR.DataGridView_ReadOnlyCollection);
    }

    bool IList.IsFixedSize => true;

    bool IList.IsReadOnly => true;

    object? IList.this[int index]
    {
        get { return _items[index]; }
        set { throw new NotSupportedException(SR.DataGridView_ReadOnlyCollection); }
    }

    void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    int ICollection.Count => _items.Count;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    internal DataGridViewSelectedCellCollection()
    {
    }

    protected override ArrayList List
    {
        get
        {
            return ArrayList.Adapter(_items);
        }
    }

    public DataGridViewCell this[int index]
    {
        get
        {
            return _items[index];
        }
    }

    /// <summary>
    ///  Adds a <see cref="DataGridViewCell"/> to this collection.
    /// </summary>
    internal int Add(DataGridViewCell dataGridViewCell)
    {
        Debug.Assert(!Contains(dataGridViewCell));
        return ((IList)_items).Add(dataGridViewCell);
    }

    /// <summary>
    ///  Adds all the <see cref="DataGridViewCell"/> objects from the provided linked list to this collection.
    /// </summary>
    internal void AddCellLinkedList(DataGridViewCellLinkedList dataGridViewCells)
    {
        Debug.Assert(dataGridViewCells is not null);
        foreach (DataGridViewCell dataGridViewCell in dataGridViewCells)
        {
            Debug.Assert(!Contains(dataGridViewCell));
            _items.Add(dataGridViewCell);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Clear()
    {
        throw new NotSupportedException(SR.DataGridView_ReadOnlyCollection);
    }

    /// <summary>
    ///  Checks to see if a DataGridViewCell is contained in this collection.
    /// </summary>
    public bool Contains(DataGridViewCell dataGridViewCell) => ((IList)_items).Contains(dataGridViewCell);

    public void CopyTo(DataGridViewCell[] array, int index) => _items.CopyTo(array, index);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Insert(int index, DataGridViewCell dataGridViewCell)
    {
        throw new NotSupportedException(SR.DataGridView_ReadOnlyCollection);
    }
}
