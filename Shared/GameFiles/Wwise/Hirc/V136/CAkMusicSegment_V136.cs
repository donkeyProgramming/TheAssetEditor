using System.Text;
using Shared.ByteParsing;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public partial class CAkMusicSegment_V136 : HircItem
    {
        public MusicNodeParams_V136 MusicNodeParams { get; set; } = new MusicNodeParams_V136();
        public double Duration { get; set; }
        public List<AkMusicMarkerWwise_V136> ArrayMarkersList { get; set; } = [];

        protected override void ReadData(ByteChunk chunk)
        {
            MusicNodeParams.ReadData(chunk);
            Duration = BitConverter.Int64BitsToDouble(chunk.ReadInt64());

            var ulNumMarkers = chunk.ReadUInt32();
            for (var i = 0; i < ulNumMarkers; i++)
                ArrayMarkersList.Add(AkMusicMarkerWwise_V136.ReadData(chunk));
        }

        public override byte[] WriteData()
        {
            var memStream = WriteHeader();
            memStream.Write(MusicNodeParams.WriteData());
            memStream.Write(ByteParsers.Int64.EncodeValue(BitConverter.DoubleToInt64Bits(Duration), out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)ArrayMarkersList.Count, out _));
            foreach (var marker in ArrayMarkersList)
                memStream.Write(marker.WriteData());
            return memStream.ToArray();
        }

        public override void UpdateSectionSize()
        {
            var size = MusicNodeParams.GetSize()
                + 8 // Duration
                + 4 // NumMarkers
                + (uint)ArrayMarkersList.Sum(marker => marker.GetSize());

            // The id counts towards the section size; the type and size prefix do not.
            SectionSize = size + ByteHelper.GetPropertyTypeSize(Id);
        }

        public class AkMusicMarkerWwise_V136
        {
            public uint Id { get; set; }
            public double Position { get; set; }
            public uint StringSize { get; set; }
            public string? MarkerName { get; set; }

            public static AkMusicMarkerWwise_V136 ReadData(ByteChunk chunk)
            {
                var akMusicMarkerWwise = new AkMusicMarkerWwise_V136();
                akMusicMarkerWwise.Id = chunk.ReadUInt32();
                akMusicMarkerWwise.Position = BitConverter.Int64BitsToDouble(chunk.ReadInt64());

                akMusicMarkerWwise.StringSize = chunk.ReadUInt32();
                akMusicMarkerWwise.MarkerName = Encoding.UTF8.GetString(chunk.ReadBytes((int)akMusicMarkerWwise.StringSize));
                return akMusicMarkerWwise;
            }

            public byte[] WriteData()
            {
                var nameBytes = MarkerName == null ? [] : Encoding.UTF8.GetBytes(MarkerName);

                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(Id, out _));
                memStream.Write(ByteParsers.Int64.EncodeValue(BitConverter.DoubleToInt64Bits(Position), out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)nameBytes.Length, out _));
                memStream.Write(nameBytes);
                return memStream.ToArray();
            }

            public uint GetSize() => 16 + (uint)(MarkerName == null ? 0 : Encoding.UTF8.GetByteCount(MarkerName));
        }
    }
}
