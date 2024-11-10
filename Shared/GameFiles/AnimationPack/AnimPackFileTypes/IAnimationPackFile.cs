using Shared.Core.Misc;

namespace Shared.GameFormats.AnimationPack.AnimPackFileTypes
{
    public interface IAnimationPackFile
    {
        AnimationPackFile Parent { get; set; }

        string FileName { get; set; }
        public bool IsUnknownFile { get; set; }
        public NotifyAttr<bool> IsChanged { get; set; }

        void CreateFromBytes(byte[] bytes);
        byte[] ToByteArray();
    }
}
