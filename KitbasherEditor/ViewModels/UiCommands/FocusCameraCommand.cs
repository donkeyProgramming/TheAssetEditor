using CommonControls.Events.UiCommands;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class FocusCameraCommand : IExecutableUiCommand
    {
        private readonly FocusSelectableObjectService _cameraFocusComponent;

        public FocusCameraCommand(FocusSelectableObjectService cameraFocusComponent)
        {
            _cameraFocusComponent = cameraFocusComponent;
        }

        public void Execute() => _cameraFocusComponent.FocusSelection()
    }
}
