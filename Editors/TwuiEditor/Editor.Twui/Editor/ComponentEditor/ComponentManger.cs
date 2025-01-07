using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Twui.Editor.Events;
using Shared.Core.Events;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.ComponentEditor
{

    public partial class ComponentManger : ObservableObject
    {
        private readonly IEventHub _eventHub;
        TwuiFile? _currentFile;

        [ObservableProperty]public partial HierarchyItem? SelectedHierarchyItem { get; set; }
        [ObservableProperty]public partial Component? SelectedComponent { get; set; }
        [ObservableProperty] public partial ComponentViewModel? SelectedComponentViewModel { get; set; }
        

        public ComponentManger(IEventHub eventHub)
        {
            _eventHub = eventHub;
        }

        public void SetFile(TwuiFile file)
        {
            _currentFile = file;
        }

        partial void OnSelectedHierarchyItemChanged(HierarchyItem? value)
        {
            if (value == null)
            {
                SelectedComponent = null;
                return;
            }

            if (_currentFile == null)
                return;

            var component = _currentFile.Components.FirstOrDefault(x => x.This == value.Id);    // Build a veiw model here! 
            if(component != null)
            {
                SelectedComponent = component;
                SelectedComponentViewModel = new ComponentViewModel(SelectedComponent);
            }
            else
            {
                SelectedComponent = null;
                SelectedComponentViewModel = null;
            }

            _eventHub.Publish(new RedrawTwuiEvent(_currentFile, SelectedComponent));
        }
    }
}
