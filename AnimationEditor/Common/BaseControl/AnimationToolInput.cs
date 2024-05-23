using CommonControls.FileTypes.AnimationPack;
using SharedCore.PackFiles.Models;

namespace AnimationEditor.PropCreator.ViewModels
{
    public class AnimationToolInput
    {
        public PackFile Mesh { get; set; }
        public PackFile Animation { get; set; }
        public string FragmentName { get; set; }
        public AnimationSlotType AnimationSlot { get; set; }
    }
}
