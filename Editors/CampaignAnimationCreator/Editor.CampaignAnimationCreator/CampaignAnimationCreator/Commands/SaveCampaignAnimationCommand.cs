using Editors.Shared.Core.Common;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.GameFormats.Animation;

namespace Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands
{
    public class SaveCampaignAnimationCommand : IUiCommand
    {
        private readonly IFileSaveService _fileSaveService;

        public SaveCampaignAnimationCommand(IFileSaveService fileSaveService)
        {
            _fileSaveService = fileSaveService;
        }

        public bool Execute(SceneObject sceneObject, out string? errorText)
        {
            errorText = null;

            if (sceneObject == null)
            {
                errorText = "No model loaded";
                return false;
            }

            if (sceneObject.Skeleton == null)
            {
                errorText = "Model has no skeleton";
                return false;
            }

            if (sceneObject.AnimationClip == null)
            {
                errorText = "No animation selected";
                return false;
            }

            var animFile = sceneObject.AnimationClip.ConvertToFileFormat(sceneObject.Skeleton);
            var bytes = AnimationFile.ConvertToBytes(animFile);
            _fileSaveService.SaveAs(".anim", bytes);

            return true;
        }
    }
}
