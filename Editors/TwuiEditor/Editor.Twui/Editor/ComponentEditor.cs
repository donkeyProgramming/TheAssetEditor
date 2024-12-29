using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Twui.Editor.Datatypes;

namespace Editors.Twui.Editor
{
    public partial class ComponentEditor : ObservableObject
    {
        [ObservableProperty] HierarchyItem? _selectedHierarchyItem;
        [ObservableProperty] Component? _selectedComponent;

        TwuiFile _currentFile;

        public ComponentEditor()
        { 
        
        }

        partial void OnSelectedHierarchyItemChanged(HierarchyItem? value)
        {
            if (value == null)
            {
                SelectedComponent = null;
                return;
            }

            var component = _currentFile.Components.FirstOrDefault(x => x.This == value.Id);
            SelectedComponent = component;
        }


        public void SetFile(TwuiFile file)
        { 
            _currentFile = file;
        }

    }
}
