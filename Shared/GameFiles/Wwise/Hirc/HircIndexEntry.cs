namespace Shared.GameFormats.Wwise.Hirc
{
    public class HircIndexEntry
    {
        public required HircHeader Header { get; set; }
        public long Offset { get; set; }
        public int Length { get; set; }
        public uint Index { get; set; }
    }
}
