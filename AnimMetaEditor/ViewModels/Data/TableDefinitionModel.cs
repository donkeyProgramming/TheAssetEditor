using AnimMetaEditor.DataType;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using FileTypes.DB;

namespace AnimMetaEditor.ViewModels.Data
{
    class TableDefinitionModel : NotifyPropertyChangedImpl
    {
        public event ValueChangedDelegate<DbColumnDefinition> SelectedItemChanged;
        public event ValueChangedDelegate<DbTableDefinition> DefinitionChanged;

        DbTableDefinition _definition = new DbTableDefinition();
        public DbTableDefinition Definition { get { return _definition; } set { SetAndNotify(ref _definition, value, DefinitionChanged); } }


        DbColumnDefinition _selectedItem;
        public DbColumnDefinition SelectedItem { get { return _selectedItem; } set { SetAndNotify(ref _selectedItem, value, SelectedItemChanged); } }


        public void TriggerUpdates()
        {
            DefinitionChanged?.Invoke(Definition);
        }
    }
}
