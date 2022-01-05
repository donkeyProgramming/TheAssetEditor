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

        public T GetPasteObject<T>() where T : class, ICopyPastItem
        {
            if (_activeItem is T typedItem)
                return typedItem;
            return null;
        }

        public void SetCopyItem(ICopyPastItem item) => _activeItem = item;
        public void Clear() => _activeItem = null;



    }
}
