using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.Events;

namespace Editors.Twui.Editor.ComponentEditor
{
    public partial class ComponentManger : ObservableObject
    {
        private readonly IEventHub _eventHub;
        TwuiContext? _currentFile;

        [ObservableProperty] public partial TwuiComponent? SelectedComponent { get; set; }
        [ObservableProperty] public partial ComponentViewModel? SelectedComponentViewModel { get; set; }
        

        public ComponentManger(IEventHub eventHub)
        { 
            _eventHub = eventHub;
        }

        public void SetFile(TwuiContext file)
        {
            _currentFile = file;
        }

        [RelayCommand]
        private void ToggleSelected()
        {
            if (SelectedComponent == null)
                return;
            
            Toogle(SelectedComponent, !SelectedComponent.ShowInPreviewRenderer);
        }

        void Toogle(TwuiComponent component, bool value)
        {
            component.ShowInPreviewRenderer = value;
            foreach(var item in component.Children)
                Toogle(item, value);
        }


        partial void OnSelectedComponentChanged(TwuiComponent? oldValue, TwuiComponent? newValue)
        {
            if(oldValue != null)
                oldValue.IsSelected = false;
            if(newValue != null)
                newValue.IsSelected = true;

        }
        
    }
}
