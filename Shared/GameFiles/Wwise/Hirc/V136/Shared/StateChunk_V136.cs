using Shared.ByteParsing;

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
            using var memStream = new MemoryStream();

            // Props before groups, the order ReadData reads them in.
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StateProps.Count, out _));
            foreach (var stateProp in StateProps)
                memStream.Write(stateProp.WriteData());

            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StateChunks.Count, out _));
            foreach (var stateChunk in StateChunks)
                memStream.Write(stateChunk.WriteData());

            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var numStatePropsSize = ByteHelper.GetPropertyTypeSize(NumStateProps);
            var numStateGroupsSize = ByteHelper.GetPropertyTypeSize(NumStateGroups);
            return numStatePropsSize + numStateGroupsSize
                + (uint)StateProps.Count * AkStatePropertyInfo_V136.Size
                + (uint)StateChunks.Sum(x => x.GetSize());
        }

        public class AkStatePropertyInfo_V136
        {
            public byte PropertyId { get; set; }
            public byte Type { get; set; }
            public byte InDb { get; set; }

            public const uint Size = 3;

            public static AkStatePropertyInfo_V136 ReadData(ByteChunk chunk)
            {
                return new AkStatePropertyInfo_V136
                {
                    PropertyId = chunk.ReadByte(),
                    Type = chunk.ReadByte(),
                    InDb = chunk.ReadByte()
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.Byte.EncodeValue(PropertyId, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(Type, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(InDb, out _));
                return memStream.ToArray();
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

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(StateGroupId, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(StateSyncType, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)States.Count, out _));
                foreach (var state in States)
                    memStream.Write(state.WriteData());
                return memStream.ToArray();
            }

            // 4 (StateGroupId) + 1 (StateSyncType) + 1 (NumStates), then the states.
            public uint GetSize() => 6 + (uint)States.Count * AkState_V136.Size;
        }

        public class AkState_V136
        {
            public uint StateId { get; set; }
            public uint StateInstanceId { get; set; }

            public const uint Size = 8;

            public static AkState_V136 ReadData(ByteChunk chunk)
            {
                return new AkState_V136
                {
                    StateId = chunk.ReadUInt32(),
                    StateInstanceId = chunk.ReadUInt32()
                };
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(StateId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(StateInstanceId, out _));
                return memStream.ToArray();
            }
        }
    }
}
