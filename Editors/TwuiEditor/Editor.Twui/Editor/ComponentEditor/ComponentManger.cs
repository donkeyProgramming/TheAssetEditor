using CommunityToolkit.Mvvm.ComponentModel;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.ComponentEditor
{
    public partial class ComponentManger : ObservableObject
    {
        [ObservableProperty] HierarchyItem? _selectedHierarchyItem;
        [ObservableProperty] Component? _selectedComponent;

        TwuiFile? _currentFile;

        public ComponentManger()
        { 
        
        }

        partial void OnSelectedHierarchyItemChanged(HierarchyItem? value)
        {
            if (value == null)
            {
                SelectedComponent = null;
                return;
            }

            var component = _currentFile.Components.FirstOrDefault(x => x.This == value.Id);    // Build a veiw model here! 
            SelectedComponent = component;
        }

        public void SetFile(TwuiFile file)
        { 
            _currentFile = file;
        }

    }
}
