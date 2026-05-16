namespace Shared.GameFormats.Video
{
    public class FrameTableRecord
    {
        public uint Offset { get; set; }
        public uint Size { get; set; }
        public bool IsKeyFrame { get; set; }
    }
}
