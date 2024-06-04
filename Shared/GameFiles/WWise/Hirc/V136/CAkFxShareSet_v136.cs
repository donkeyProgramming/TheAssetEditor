using Shared.Core.ByteParsing;
using System;
using System.Collections.Generic;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    //Seems like it's exactly the same as FxCustom...
    //Not sure if there's a fancy C# way of doing things, but I just copy+pasted it below
    public class CAkFxShareSet_v136 : HircItem
    {
        public uint plugin_id { get; set; }
        public AkPluginParam AkPluginParam { get; set; }
        public List<AkMediaMap> mediaList { get; set; } = new List<AkMediaMap>();
        public InitialRTPC InitialRTPC { get; set; }
        public StateChunk StateChunk { get; set; }
        public List<PluginPropertyValue> propertyValuesList { get; set; } = new List<PluginPropertyValue>();

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            //contains the plugin type and company id (CA doesn't have one apparently)
            plugin_id = chunk.ReadUInt32();

            var uSize = chunk.ReadUInt32();
            AkPluginParam = AkPluginParam.Create(chunk, plugin_id, uSize);

            var uNumBankData = chunk.ReadByte();
            for (int i = 0; i < uNumBankData; i++)
                mediaList.Add(AkMediaMap.Create(chunk));

            InitialRTPC = InitialRTPC.Create(chunk);
            StateChunk = StateChunk.Create(chunk);

            var numValues = chunk.ReadShort();
            for (int i = 0; i < numValues; i++)
                propertyValuesList.Add(PluginPropertyValue.Create(chunk));
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }
}