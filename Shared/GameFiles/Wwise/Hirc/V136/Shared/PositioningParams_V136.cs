using Shared.ByteParsing;

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

        // Mirrors ReadData's branching exactly: the 3D byte is only present when positioning and
        // 3D are both set, and the automation block only when the position type is non-zero.
        bool HasPositioning => (BitsPositioning >> 0 & 1) == 1;
        bool Has3D => (BitsPositioning >> 1 & 1) == 1;
        bool HasBits3D => HasPositioning && Has3D;
        bool HasAutomation => HasBits3D && (BitsPositioning >> 5 & 3) != 0;

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(BitsPositioning, out _));

            if (!HasBits3D)
                return memStream.ToArray();

            memStream.Write(ByteParsers.Byte.EncodeValue(Bits3D, out _));

            if (!HasAutomation)
                return memStream.ToArray();

            memStream.Write(ByteParsers.Byte.EncodeValue(PathMode, out _));
            memStream.Write(ByteParsers.Single.EncodeValue(TransitionTime, out _));

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)VertexList.Count, out _));
            foreach (var vertex in VertexList)
                memStream.Write(vertex.WriteData());

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)PlayListItems.Count, out _));
            foreach (var playListItem in PlayListItems)
                memStream.Write(playListItem.WriteData());

            foreach (var param in Params)
                memStream.Write(param.WriteData());

            return memStream.ToArray();
        }

        public uint GetSize()
        {
            if (!HasBits3D)
                return 1;
            if (!HasAutomation)
                return 2;

            return 2
                + 1 // PathMode
                + 4 // TransitionTime
                + 4 + (uint)VertexList.Count * AkPathVertex_V136.Size
                + 4 + (uint)PlayListItems.Count * AkPathListItemOffset_V136.Size
                + (uint)Params.Count * Ak3DAutomationParams_V136.Size;
        }

        public class AkPathVertex_V136
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public int Duration { get; set; }

            public const uint Size = 16;

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

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Single.EncodeValue(X, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(Y, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(Z, out _));
                memStream.Write(ByteParsers.Int32.EncodeValue(Duration, out _));
                return memStream.ToArray();
            }
        }

        public class AkPathListItemOffset_V136
        {
            public uint VerticesOffset { get; set; }
            public uint NumVertices { get; set; }

            public const uint Size = 8;

            public static AkPathListItemOffset_V136 ReadData(ByteChunk chunk)
            {
                return new AkPathListItemOffset_V136
                {
                    VerticesOffset = chunk.ReadUInt32(),
                    NumVertices = chunk.ReadUInt32()
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(VerticesOffset, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(NumVertices, out _));
                return memStream.ToArray();
            }
        }

        public class Ak3DAutomationParams_V136
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public const uint Size = 12;

            public static Ak3DAutomationParams_V136 ReadData(ByteChunk chunk)
            {
                return new Ak3DAutomationParams_V136
                {
                    X = chunk.ReadSingle(),
                    Y = chunk.ReadSingle(),
                    Z = chunk.ReadSingle()
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Single.EncodeValue(X, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(Y, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(Z, out _));
                return memStream.ToArray();
            }
        }
    }
}
