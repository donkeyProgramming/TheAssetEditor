using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc
{
    public class ByteHirc : HircItem
    {
        public byte[] HircData { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            HircData = chunk.ReadBytes((int)Size - 4);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(HircData);
            return memStream.ToArray();
        }
    }
}
