using Shared.Core.Misc;

namespace Shared.GameFormats.AnimationPack.AnimPackFileTypes
{
    public class UnknownAnimFile : IAnimationPackFile
    {
        public AnimationPackFile Parent { get; set; }
        public string FileName { get; set; }
        public bool IsUnknownFile { get; set; } = true;
        public NotifyAttr<bool> IsChanged { get; set; } = new NotifyAttr<bool>(false);

        byte[] _data;

        public UnknownAnimFile(string fileName, byte[] data)
        {
            FileName = fileName;
            _data = data;
        }

        public void CreateFromBytes(byte[] bytes)
        {
            _data = bytes;
        }

        public byte[] ToByteArray()
        {
            return _data;
        }
    }
}
