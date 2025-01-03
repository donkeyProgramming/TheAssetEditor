using System.Text;
using Shared.Core.ByteParsing;
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
            Duration = chunk.ReadInt64(); //chunk.ReadDouble();

            var ulNumMarkers = chunk.ReadUInt32();
            for (var i = 0; i < ulNumMarkers; i++)
                ArrayMarkersList.Add(AkMusicMarkerWwise_V136.ReadData(chunk));
        }

        public override byte[] WriteData() => throw new NotSupportedException("Users probably don't need this complexity.");
        public override void UpdateSectionSize() => throw new NotSupportedException("Users probably don't need this complexity.");

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
                akMusicMarkerWwise.Position = chunk.ReadInt64();
                akMusicMarkerWwise.MarkerName = Encoding.UTF8.GetString(chunk.ReadBytes((int)akMusicMarkerWwise.StringSize));
                return akMusicMarkerWwise;
            }
        }
    }
}
