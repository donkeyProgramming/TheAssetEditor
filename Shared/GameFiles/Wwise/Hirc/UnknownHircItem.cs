using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc
{
    /// <summary>
    /// A hirc this codebase has no reader for, kept as the bytes it was read from.
    ///
    /// The payload is retained rather than skipped so that a .bnk containing types nothing here
    /// models can still be written back out. Replacing a vanilla .bnk means shipping every hirc it
    /// held, and vanilla .bnks carry types the editor has never needed to read - without this, one
    /// unmodelled hirc anywhere in the file makes the whole .bnk unwritable.
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

        // The size is whatever it was when read. Nothing can edit a hirc it cannot interpret, so
        // there is never anything to recompute.
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
