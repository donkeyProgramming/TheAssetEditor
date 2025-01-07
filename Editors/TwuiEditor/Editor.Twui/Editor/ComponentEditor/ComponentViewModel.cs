using CommunityToolkit.Mvvm.ComponentModel;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.ComponentEditor
{
    public partial class ComponentViewModel : ObservableObject
    {
        private Component? _selectedComponent;

        public ComponentViewModel(Component? selectedComponent)
        {
            _selectedComponent = selectedComponent;
        }
    }
}




// Name 
// Offset
// Docking
// Docking offset
// Size
// Images
// Texts
// IsChildComponent

// State info
