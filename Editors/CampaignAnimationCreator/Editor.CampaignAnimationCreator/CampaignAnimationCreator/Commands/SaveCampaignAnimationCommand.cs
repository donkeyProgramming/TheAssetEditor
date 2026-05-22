using GameWorld.Core.Animation;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.GameFormats.Animation;

namespace Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands
{
    public class SaveCampaignAnimationCommand(IFileSaveService fileSaveService, IStandardDialogs standardDialogs) : IAeCommand
    {
        private readonly IFileSaveService _fileSaveService = fileSaveService;

        void IAeCommand.Execute() => throw new NotSupportedException("Use Execute with parameters instead.");

        public bool Execute(GameSkeleton? skeleton, AnimationClip? animClip)
        {
            if (skeleton == null)
            {
                standardDialogs.ShowDialogBox("Unable to save - No skeleton provided");
          
                return false;
            }

            if (animClip == null)
            {
                standardDialogs.ShowDialogBox("Unable to save - No animation provided");
                return false;
            }

            var animFile = animClip.ConvertToFileFormat(skeleton);
            var bytes = AnimationFile.ConvertToBytes(animFile);
            _fileSaveService.SaveAs(".anim", bytes);

            return true;
        }
    }
}
