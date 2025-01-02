using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkFxCustom_v136 : HircItem
    {
        public uint PluginId { get; set; }
        public uint Size { get; set; }
        public AkPluginParam_V136 AkPluginParam { get; set; } = new AkPluginParam_V136();
        public byte NumBankData { get; set; }
        public List<AkMediaMap_V136> MediaList { get; set; } = [];
        public InitialRtpc_V136 InitialRtpc { get; set; } = new InitialRtpc_V136();
        public StateChunk_V136 StateChunk { get; set; } = new StateChunk_V136();
        public short NumValues { get; set; }
        public List<PluginPropertyValue_V136> PropertyValuesList { get; set; } = [];

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            PluginId = chunk.ReadUInt32();
            Size = chunk.ReadUInt32();
            AkPluginParam = AkPluginParam_V136.Create(chunk, PluginId, Size);

            NumBankData = chunk.ReadByte();
            for (var i = 0; i < NumBankData; i++)
                MediaList.Add(AkMediaMap_V136.Create(chunk));

            InitialRtpc.Create(chunk);
            StateChunk.Create(chunk);

            NumValues = chunk.ReadShort();
            for (var i = 0; i < NumValues; i++)
                PropertyValuesList.Add(PluginPropertyValue_V136.Create(chunk));
        }

        public override byte[] GetAsByteArray() => throw new NotSupportedException("Users probably don't need this complexity.");
        public override void UpdateSectionSize() => throw new NotSupportedException("Users probably don't need this complexity.");
    }
}
