using CommonControls.Events.UiCommands;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ToggleViewSelectedCommand : IExecutableUiCommand
    {
        ViewOnlySelectedService _viewOnlySelectedComp;

        public ToggleViewSelectedCommand(ViewOnlySelectedService viewOnlySelectedComp)
        {
            _viewOnlySelectedComp = viewOnlySelectedComp;
        }

        public void Execute()
        {
            _viewOnlySelectedComp.Toggle();
        }
    }
}
