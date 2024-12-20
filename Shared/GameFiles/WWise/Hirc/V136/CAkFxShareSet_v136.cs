﻿using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V136
{
    //Seems like it's exactly the same as FxCustom...
    //Not sure if there's a fancy C# way of doing things, but I just copy+pasted it below
    public class CAkFxShareSet_v136 : HircItem
    {
        public uint PluginId { get; set; }
        public AkPluginParam AkPluginParam { get; set; }
        public List<AkMediaMap> MediaList { get; set; } = [];
        public InitialRTPC InitialRTPC { get; set; }
        public StateChunk StateChunk { get; set; }
        public List<PluginPropertyValue> PropertyValuesList { get; set; } = [];

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            //contains the plugin type and company id (CA doesn't have one apparently)
            PluginId = chunk.ReadUInt32();

            var uSize = chunk.ReadUInt32();
            AkPluginParam = AkPluginParam.Create(chunk, PluginId, uSize);

            var uNumBankData = chunk.ReadByte();
            for (var i = 0; i < uNumBankData; i++)
                MediaList.Add(AkMediaMap.Create(chunk));

            InitialRTPC = InitialRTPC.Create(chunk);
            StateChunk = StateChunk.Create(chunk);

            var numValues = chunk.ReadShort();
            for (var i = 0; i < numValues; i++)
                PropertyValuesList.Add(PluginPropertyValue.Create(chunk));
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}
