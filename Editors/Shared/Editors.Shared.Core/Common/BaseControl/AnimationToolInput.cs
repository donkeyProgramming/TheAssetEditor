using Shared.Core.PackFiles.Models;
using Shared.GameFormats.AnimationPack;

namespace Editors.Shared.Core.Common.BaseControl
{
    public class AnimationToolInput
    {
        public PackFile Mesh { get; set; }
        public PackFile Animation { get; set; }
        public string? FragmentName { get; set; }
        public AnimationSlotType? AnimationSlot { get; set; }
    }
}
