using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class PositioningParams_V136
    {
        public byte BitsPositioning { get; set; }
        public byte Bits3D { get; set; }
        public byte PathMode { get; set; }
        public float TransitionTime { get; set; }
        public uint NumVertexes { get; set; }
        public List<AkPathVertex_V136> VertexList { get; set; } = [];
        public uint NumPlayListItems { get; set; }
        public List<AkPathListItemOffset_V136> PlayListItems { get; set; } = [];
        public List<Ak3DAutomationParams_V136> Params { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            BitsPositioning = chunk.ReadByte();
            var has_positioning = (BitsPositioning >> 0 & 1) == 1;
            var has_3d = (BitsPositioning >> 1 & 1) == 1;
            if (has_positioning && has_3d)
            {
                Bits3D = chunk.ReadByte();

                var e3DPositionType = BitsPositioning >> 5 & 3;
                var has_automation = e3DPositionType != 0;
                if (has_automation)
                {
                    PathMode = chunk.ReadByte();
                    TransitionTime = chunk.ReadSingle();
                    NumVertexes = chunk.ReadUInt32();
                    for (var i = 0; i < NumVertexes; i++)
                        VertexList.Add(AkPathVertex_V136.ReadData(chunk));

                    NumPlayListItems = chunk.ReadUInt32();
                    for (var i = 0; i < NumPlayListItems; i++)
                        PlayListItems.Add(AkPathListItemOffset_V136.ReadData(chunk));

                    for (var i = 0; i < NumPlayListItems; i++)
                        Params.Add(Ak3DAutomationParams_V136.ReadData(chunk));
                }
            }
        }

        public byte[] WriteData()
        {
            if (BitsPositioning == 0x03 && Bits3D == 0x08)
                return [0x03, 0x08];
            else if (BitsPositioning == 0x00)
                return [0x00];
            else
                throw new NotSupportedException("Users probably don't need this complexity.");
        }

        public uint GetSize()
        {
            if (BitsPositioning == 0x03 && Bits3D == 0x08)
                return 2;
            else if (BitsPositioning == 0x00)
                return 1;
            else
                throw new NotSupportedException("Users probably don't need this complexity.");
        }

        public class AkPathVertex_V136
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public int Duration { get; set; }

            public static AkPathVertex_V136 ReadData(ByteChunk chunk)
            {
                return new AkPathVertex_V136
                {
                    X = chunk.ReadSingle(),
                    Y = chunk.ReadSingle(),
                    Z = chunk.ReadSingle(),
                    Duration = chunk.ReadInt32()
                };
            }
        }

        public class AkPathListItemOffset_V136
        {
            public uint VerticesOffset { get; set; }
            public uint NumVertices { get; set; }

            public static AkPathListItemOffset_V136 ReadData(ByteChunk chunk)
            {
                return new AkPathListItemOffset_V136
                {
                    VerticesOffset = chunk.ReadUInt32(),
                    NumVertices = chunk.ReadUInt32()
                };
            }
        }

        public class Ak3DAutomationParams_V136
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public static Ak3DAutomationParams_V136 ReadData(ByteChunk chunk)
            {
                return new Ak3DAutomationParams_V136
                {
                    X = chunk.ReadSingle(),
                    Y = chunk.ReadSingle(),
                    Z = chunk.ReadSingle()
                };
            }
        }
    }
}
