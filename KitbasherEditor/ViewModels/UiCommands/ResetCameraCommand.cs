using CommonControls.Events.UiCommands;
using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    public class ResetCameraCommand : IExecutableUiCommand
    {
        private readonly FocusSelectableObjectService _cameraFocusComponent;

        public ResetCameraCommand(FocusSelectableObjectService cameraFocusComponent)
        {
            _cameraFocusComponent = cameraFocusComponent;
        }

        public void Execute() => _cameraFocusComponent.ResetCamera()
    }
}
