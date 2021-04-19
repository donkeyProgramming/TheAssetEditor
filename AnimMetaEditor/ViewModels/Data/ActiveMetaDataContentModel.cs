using AnimMetaEditor.DataType;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace AnimMetaEditor.ViewModels.Data
{
    class ActiveMetaDataContentModel : NotifyPropertyChangedImpl
    {
        public event ValueChangedDelegate<MetaDataTagItem.Data> SelectedTagItemChanged;
        public event ValueChangedDelegate<MetaDataTagItem> SelectedTagTypeChanged;

        MetaDataTagItem.Data _selectedItem;
        public MetaDataTagItem.Data SelectedTagItem { get { return _selectedItem; } set { SetAndNotify(ref _selectedItem, value, SelectedTagItemChanged); } }


        MetaDataTagItem _tagTypeItem;
        public MetaDataTagItem SelectedTagType 
        { 
            get { return _tagTypeItem; } 
            set 
            {
                _tagTypeItem = value;
                if (_tagTypeItem != null)
                    _selectedItem = _tagTypeItem.DataItems.FirstOrDefault();
                else
                    _selectedItem = null;

                SelectedTagTypeChanged?.Invoke(_tagTypeItem);
                SelectedTagItemChanged?.Invoke(_selectedItem);
            } 
        }


        MetaDataFile _file;
        public MetaDataFile File { get { return _file; } set { SetAndNotify(ref _file, value); } }
    }
}
