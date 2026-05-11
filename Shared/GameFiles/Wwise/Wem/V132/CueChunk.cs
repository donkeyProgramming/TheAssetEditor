using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public class CueChunk : RiffChunk
    {
        public List<CuePoint> CuePoints { get; set; } = [];

        public CueChunk()
        {
            Tag = WemChunks.Cue;
        }

        public override void ReadData(ByteChunk chunk)
        {
            var count = checked((int)chunk.ReadUInt32());
            for (var i = 0; i < count; i++)
                CuePoints.Add(CuePoint.ReadData(chunk));
        }

        public override byte[] WriteData()
        {
            using var stream = new MemoryStream();

            stream.Write(ByteParsers.UInt32.EncodeValue((uint)CuePoints.Count, out _));
            foreach (var point in CuePoints)
                stream.Write(point.WriteData());

            var byteArray = stream.ToArray();
            var sanityReload = new CueChunk();
            sanityReload.ReadChunk(new ByteChunk(byteArray));

            return byteArray;
        }
    }
}
