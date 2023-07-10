// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace CommonControls.Common
{
    public interface ICopyPastItem
    {
        string Description { get; set; }
    }


    public class CopyPasteManager
    {
        ICopyPastItem _activeItem;
        IList<ICopyPastItem> _activeItems;

        public T GetPasteObject<T>() where T : class, ICopyPastItem
        {
            if (_activeItem is T typedItem)
                return typedItem;
            return null;
        }

        public IList<T> GetPasteObjects<T>() where T : class, ICopyPastItem
        {
            if (_activeItems is IList<T> typedItem)
                return typedItem;
            return null;
        }

        public void SetCopyItem(ICopyPastItem item) => _activeItem = item;
        public void SetCopyItems(List<ICopyPastItem> items) => _activeItems = items;
        public void Clear()
        {
            _activeItem = null;
            _activeItems = null;
        }



    }
}
