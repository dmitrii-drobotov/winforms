﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Forms;

/// <summary>
///  HtmlShimManager - this class manages the shims for HtmlWindows, HtmlDocuments, and HtmlElements.
///  essentially we need a long-lasting object to call back on events from the web browser, and the
///  manager is the one in charge of making sure this list stays around as long as needed.
///
///  When a HtmlWindow unloads we prune our list of corresponding document, window, and element shims.
/// </summary>
internal sealed class HtmlShimManager : IDisposable
{
    private Dictionary<HtmlWindow, HtmlWindow.HtmlWindowShim>? htmlWindowShims;
    private Dictionary<HtmlElement, HtmlElement.HtmlElementShim>? htmlElementShims;
    private Dictionary<HtmlDocument, HtmlDocument.HtmlDocumentShim>? _htmlDocumentShims;

    internal HtmlShimManager()
    {
    }

    /// <summary>
    ///  Adds a <see cref="HtmlDocument.HtmlDocumentShim"/> to list of shims to manage.
    ///  Can create a WindowShim as a side effect so it knows when to self prune from the list.
    /// </summary>
    public void AddDocumentShim(HtmlDocument doc)
    {
        HtmlDocument.HtmlDocumentShim? shim = null;

        if (_htmlDocumentShims is null)
        {
            _htmlDocumentShims = new Dictionary<HtmlDocument, HtmlDocument.HtmlDocumentShim>();
            shim = new HtmlDocument.HtmlDocumentShim(doc);
            _htmlDocumentShims[doc] = shim;
        }
        else if (!_htmlDocumentShims.ContainsKey(doc))
        {
            shim = new HtmlDocument.HtmlDocumentShim(doc);
            _htmlDocumentShims[doc] = shim;
        }

        if (shim is not null)
        {
            OnShimAdded(shim);
        }
    }

    /// <summary>
    ///  Adds a <see cref="HtmlWindow.HtmlWindowShim"/> to list of shims to manage.
    /// </summary>
    public void AddWindowShim(HtmlWindow window)
    {
        HtmlWindow.HtmlWindowShim? shim = null;
        if (htmlWindowShims is null)
        {
            htmlWindowShims = new Dictionary<HtmlWindow, HtmlWindow.HtmlWindowShim>();
            shim = new HtmlWindow.HtmlWindowShim(window);
            htmlWindowShims[window] = shim;
        }
        else if (!htmlWindowShims.ContainsKey(window))
        {
            shim = new HtmlWindow.HtmlWindowShim(window);
            htmlWindowShims[window] = shim;
        }

        if (shim is not null)
        {
            // strictly not necessary, but here for future use.
            OnShimAdded(shim);
        }
    }

    /// <summary> AddElementShim - adds a HtmlDocumentShim to list of shims to manage
    ///  Can create a WindowShim as a side effect so it knows when to self prune from the list.
    /// </summary>
    public void AddElementShim(HtmlElement element)
    {
        HtmlElement.HtmlElementShim? shim = null;

        if (htmlElementShims is null)
        {
            htmlElementShims = new Dictionary<HtmlElement, HtmlElement.HtmlElementShim>();
            shim = new HtmlElement.HtmlElementShim(element);
            htmlElementShims[element] = shim;
        }
        else if (!htmlElementShims.ContainsKey(element))
        {
            shim = new HtmlElement.HtmlElementShim(element);
            htmlElementShims[element] = shim;
        }

        if (shim is not null)
        {
            OnShimAdded(shim);
        }
    }

    internal HtmlDocument.HtmlDocumentShim? GetDocumentShim(HtmlDocument document)
    {
        if (_htmlDocumentShims is null)
        {
            return null;
        }

        if (_htmlDocumentShims.TryGetValue(document, out HtmlDocument.HtmlDocumentShim? value))
        {
            return value;
        }

        return null;
    }

    internal HtmlElement.HtmlElementShim? GetElementShim(HtmlElement element)
    {
        if (htmlElementShims is null)
        {
            return null;
        }

        if (htmlElementShims.TryGetValue(element, out HtmlElement.HtmlElementShim? elementShim))
        {
            return elementShim;
        }

        return null;
    }

    internal HtmlWindow.HtmlWindowShim? GetWindowShim(HtmlWindow window)
    {
        if (htmlWindowShims is null)
        {
            return null;
        }

        if (htmlWindowShims.TryGetValue(window, out HtmlWindow.HtmlWindowShim? windowShim))
        {
            return windowShim;
        }

        return null;
    }

    private void OnShimAdded(HtmlShim addedShim)
    {
        Debug.Assert(addedShim is not null, "Why are we calling this with a null shim?");
        if (addedShim is not null and not HtmlWindow.HtmlWindowShim)
        {
            // We need to add a window shim here for documents and elements
            // so we can sync Window.Unload. The window shim itself will trap
            // the unload event and call back on us on OnWindowUnloaded.  When
            // that happens we know we can free all our ptrs to COM.
            AddWindowShim(new HtmlWindow(this, addedShim.AssociatedWindow));
        }
    }

    /// <summary>
    ///  HtmlWindowShim calls back on us when it has unloaded the page.  At this point we need to
    ///  walk through our lists and make sure we've cleaned up
    /// </summary>
    internal void OnWindowUnloaded(HtmlWindow unloadedWindow)
    {
        Debug.Assert(unloadedWindow is not null, "Why are we calling this with a null window?");
        if (unloadedWindow is not null)
        {
            //
            // prune documents
            //
            if (_htmlDocumentShims is not null)
            {
                HtmlDocument.HtmlDocumentShim[] shims = new HtmlDocument.HtmlDocumentShim[_htmlDocumentShims.Count];
                _htmlDocumentShims.Values.CopyTo(shims, 0);

                foreach (HtmlDocument.HtmlDocumentShim shim in shims)
                {
                    if (shim.AssociatedWindow == unloadedWindow.NativeHtmlWindow)
                    {
                        _htmlDocumentShims.Remove(shim.Document);
                        shim.Dispose();
                    }
                }
            }

            //
            // prune elements
            //
            if (htmlElementShims is not null)
            {
                HtmlElement.HtmlElementShim[] shims = new HtmlElement.HtmlElementShim[htmlElementShims.Count];
                htmlElementShims.Values.CopyTo(shims, 0);

                foreach (HtmlElement.HtmlElementShim shim in shims)
                {
                    if (shim.AssociatedWindow == unloadedWindow.NativeHtmlWindow)
                    {
                        htmlElementShims.Remove(shim.Element);
                        shim.Dispose();
                    }
                }
            }

            // Prune the particular window from the list.
            if (htmlWindowShims is not null)
            {
                if (htmlWindowShims.Remove(unloadedWindow, out HtmlWindow.HtmlWindowShim? shim))
                {
                    shim.Dispose();
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (htmlElementShims is not null)
            {
                foreach (HtmlElement.HtmlElementShim shim in htmlElementShims.Values)
                {
                    shim.Dispose();
                }
            }

            if (_htmlDocumentShims is not null)
            {
                foreach (HtmlDocument.HtmlDocumentShim shim in _htmlDocumentShims.Values)
                {
                    shim.Dispose();
                }
            }

            if (htmlWindowShims is not null)
            {
                foreach (HtmlWindow.HtmlWindowShim shim in htmlWindowShims.Values)
                {
                    shim.Dispose();
                }
            }

            htmlWindowShims = null;
            _htmlDocumentShims = null;
            htmlWindowShims = null;
        }
    }
}
