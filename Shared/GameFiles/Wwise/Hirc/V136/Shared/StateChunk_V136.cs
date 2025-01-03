using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class StateChunk_V136
    {
        public byte NumStateProps { get; set; }
        public List<AkStatePropertyInfo_V136> StateProps { get; set; } = [];
        public byte NumStateGroups { get; set; }
        public List<AkStateGroupChunk_V136> StateChunks { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            NumStateProps = chunk.ReadByte();
            for (var i = 0; i < NumStateProps; i++)
                StateProps.Add(AkStatePropertyInfo_V136.ReadData(chunk));

            NumStateGroups = chunk.ReadByte();
            for (var i = 0; i < NumStateGroups; i++)
                StateChunks.Add(AkStateGroupChunk_V136.ReadData(chunk));
        }

        public byte[] WriteData()
        {
            if (StateChunks.Count != 0 || StateProps.Count != 0)
                throw new NotSupportedException("Users probably don't need this complexity.");

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StateChunks.Count, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StateProps.Count, out _));
            return memStream.ToArray();
        }

        public uint GetSize()
        {
            if (StateChunks.Count != 0 || StateProps.Count != 0)
                throw new NotSupportedException("Users probably don't need this complexity.");

            var numStatePropsSize = ByteHelper.GetPropertyTypeSize(NumStateProps);
            var numStateGroupsSize = ByteHelper.GetPropertyTypeSize(NumStateGroups);
            return numStatePropsSize + numStateGroupsSize;
        }

        public class AkStatePropertyInfo_V136
        {
            public byte PropertyId { get; set; }
            public byte Type { get; set; }
            public byte InDb { get; set; }

            public static AkStatePropertyInfo_V136 ReadData(ByteChunk chunk)
            {
                return new AkStatePropertyInfo_V136
                {
                    PropertyId = chunk.ReadByte(),
                    Type = chunk.ReadByte(),
                    InDb = chunk.ReadByte()
                };
            }
        }

        public class AkStateGroupChunk_V136
        {
            public uint StateGroupId { get; set; }
            public byte StateSyncType { get; set; }
            public byte NumStates { get; set; }
            public List<AkState_V136> States { get; set; } = [];

            public static AkStateGroupChunk_V136 ReadData(ByteChunk chunk)
            {
                var instance = new AkStateGroupChunk_V136
                {
                    StateGroupId = chunk.ReadUInt32(),
                    StateSyncType = chunk.ReadByte(),
                    NumStates = chunk.ReadByte()
                };

                for (var i = 0; i < instance.NumStates; i++)
                    instance.States.Add(AkState_V136.ReadData(chunk));

                return instance;
            }
        }

        public class AkState_V136
        {
            public uint StateId { get; set; }
            public uint StateInstanceId { get; set; }

            public static AkState_V136 ReadData(ByteChunk chunk)
            {
                return new AkState_V136
                {
                    StateId = chunk.ReadUInt32(),
                    StateInstanceId = chunk.ReadUInt32()
                };
            }
        }
    }
}
