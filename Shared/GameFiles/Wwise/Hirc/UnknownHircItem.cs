using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc
{
    /// <summary>
    /// A hirc type with no reader, kept as its raw bytes so that a .bnk holding one can still be
    /// written back out unchanged - needed when a whole vanilla .bnk is re-shipped with edits.
    /// </summary>
    public class UnknownHircItem : HircItem
    {
        public string ErrorMsg { get; set; }

        /// <summary>The section as it was on disk, everything after the type, size and id.</summary>
        public byte[] Payload { get; set; } = [];

        protected override void ReadData(ByteChunk chunk)
        {
            Payload = chunk.ReadBytes((int)SectionSize - 4);
        }

        // The payload is never edited, so this is always the size it was read with.
        public override void UpdateSectionSize() => SectionSize = (uint)Payload.Length + 4;

        public override byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(HircHeader.WriteData(Header));
            memStream.Write(Payload);
            return memStream.ToArray();
        }
    }
}
