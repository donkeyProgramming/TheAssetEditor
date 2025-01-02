using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkMusicTransitionObject_V136
    {
        public int SegmentId { get; set; }
        public AkMusicFade_V136 FadeInParams { get; set; } = new AkMusicFade_V136();
        public AkMusicFade_V136 FadeOutParams { get; set; } = new AkMusicFade_V136();
        public byte PlayPreEntry { get; set; }
        public byte PlayPostExit { get; set; }

        public void ReadData(ByteChunk chunk)
        {
            SegmentId = chunk.ReadInt32();
            FadeInParams.ReadData(chunk);
            FadeOutParams.ReadData(chunk);
            PlayPreEntry = chunk.ReadByte();
            PlayPostExit = chunk.ReadByte();
        }

        public class AkMusicFade_V136
        {
            public int TransitionTime { get; set; }
            public uint FadeCurve { get; set; }
            public int FadeOffset { get; set; }

            public void ReadData(ByteChunk chunk)
            {
                TransitionTime = chunk.ReadInt32();
                FadeCurve = chunk.ReadUInt32();
                FadeOffset = chunk.ReadInt32();
            }
        }
    }
}
