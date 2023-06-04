using System;
using System.Collections.Generic;
using System.Text;

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
