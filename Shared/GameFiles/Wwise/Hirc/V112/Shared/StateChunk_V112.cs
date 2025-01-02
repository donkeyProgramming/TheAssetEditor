using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V112.Shared
{
    public class StateChunk_V112
    {
        public uint NumStateGroups { get; set; }
        public List<AkStateGroupChunk_V112> StateChunks { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            NumStateGroups = chunk.ReadUInt32();
            for (var i = 0; i < NumStateGroups; i++)
            {
                var akStateGroupChunk = new AkStateGroupChunk_V112();
                akStateGroupChunk.ReadData(chunk);
                StateChunks.Add(akStateGroupChunk);
            }
        }

        public byte[] WriteData()
        {
            if (StateChunks.Count != 0 )
                throw new NotSupportedException("Users probably don't need this complexity.");

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StateChunks.Count, out _));
            return memStream.ToArray();
        }

        public uint GetSize()
        {
            if (StateChunks.Count != 0)
                throw new NotSupportedException("Users probably don't need this complexity.");

            var numStateGroupsSize = ByteHelper.GetPropertyTypeSize(NumStateGroups);

            return numStateGroupsSize;
        }

        public class AkStateGroupChunk_V112
        {
            public uint StateGroupId { get; set; }
            public byte StateSyncType { get; set; }
            public ushort NumStates { get; set; }
            public List<AkState_V112> States { get; set; } = [];

            public void ReadData(ByteChunk chunk)
            {
                StateGroupId = chunk.ReadUInt32();
                StateSyncType = chunk.ReadByte();
                NumStates = chunk.ReadUShort();
                for (var i = 0; i < NumStates; i++)
                {
                    var akState = new AkState_V112();
                    akState.ReadData(chunk);
                    States.Add(akState);
                }
            }
        }

        public class AkState_V112
        {
            public uint StateId { get; set; }
            public uint StateInstanceId { get; set; }

            public void ReadData(ByteChunk chunk)
            {
                StateId = chunk.ReadUInt32();
                StateInstanceId = chunk.ReadUInt32();
            }
        }
    }
}
