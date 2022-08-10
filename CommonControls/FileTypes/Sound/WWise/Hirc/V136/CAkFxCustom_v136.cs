using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V136
{
    public class CAkFxCustom_v136 : HircItem
    {
        public uint plugin_id { get; set; }
        public AkPluginParam AkPluginParam { get; set; }
        public List<AkMediaMap> mediaList { get; set; } = new List<AkMediaMap>();
        public InitialRTPC InitialRTPC { get; set; }
        public StateChunk StateChunk { get; set; }
        public List<PluginPropertyValue> propertyValuesList { get; set; } = new List<PluginPropertyValue>();

        protected override void CreateSpesificData(ByteChunk chunk)
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

        public override void ComputeSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class AkMediaMap
    {
        public byte index { get; set; }
        public uint sourceId { get; set; }

        public static AkMediaMap Create(ByteChunk chunk)
        {
            var instance = new AkMediaMap();

            instance.index = chunk.ReadByte();
            instance.sourceId = chunk.ReadUInt32();

            return instance;
        }
    }

    public class PluginPropertyValue
    {
        public uint propertyId { get; set; }
        public byte rtpcAccum { get; set; }
        public float fValue { get; set; }

        public static PluginPropertyValue Create(ByteChunk chunk)
        {
            var instance = new PluginPropertyValue();

            instance.propertyId = chunk.ReadUInt32();
            instance.rtpcAccum = chunk.ReadByte();
            instance.fValue = chunk.ReadSingle();

            return instance;
        }
    }

    public class CAkFxSrcSilenceParams : AkPluginParam
    {
        public float fDuration { get; set; }
        public float fRandomizedLengthMinus { get; set; }
        public float fRandomizedLengthPlus { get; set; }

        public static CAkFxSrcSilenceParams Create(ByteChunk chunk, uint uSize)
        {
            var instance = new CAkFxSrcSilenceParams();
            instance.fDuration = chunk.ReadSingle();
            instance.fRandomizedLengthMinus = chunk.ReadSingle();
            instance.fRandomizedLengthPlus = chunk.ReadSingle();
            return instance;
        }
    }

    public class AkPluginParam
    {
        public List<byte> pParamBlock { get; set; } = new List<byte>();
        //used for default case, "gap" of bytes

        public static AkPluginParam Create(ByteChunk chunk, uint plugin_id, uint uSize)
        {
            //Only SrcSilence is used for WH3... maybe...
            //Actually the default case would take care of any of them, so ehhh
            switch (plugin_id)
            {
                //case 0x00640002: return CAkFxSrcSineParams.Create(chunk, uSize);
                case 0x00650002: return CAkFxSrcSilenceParams.Create(chunk, uSize);
                //case 0x00660002: return CAkToneGenParams.Create(chunk, uSize);
                //case 0x00690003: return CAkParameterEQFXParams.Create(chunk, uSize);
                //case 0x006A0003: return CAkDelayFXParams.Create(chunk, uSize);
                //case 0x006E0003: return CAkPeakLimiterFXParams.Create(chunk, uSize);
                //case 0x00730003: return CAkFDNReverbFXParams.Create(chunk, uSize);
                //case 0x00760003: return CAkRoomVerbFXParams.Create(chunk, uSize);
                //case 0x007D0003: return CAkFlangerFXParams.Create(chunk, uSize);
                //case 0x007E0003: return CAkGuitarDistortionFXParams.Create(chunk, uSize);
                //case 0x007F0003: return CAkConvolutionReverbFXParams.Create(chunk, uSize);
                //case 0x00810003: return CAkMeterFXParams.Create(chunk, uSize);
                //case 0x00870003: return CAkStereoDelayFXParams.Create(chunk, uSize);
                //case 0x008B0003: return CAkGainFXParams.Create(chunk, uSize);
                //case 0x00940002: return CAkSynthOneParams.Create(chunk, uSize);
                //case 0x00C80002: return CAkFxSrcAudioInputParams.Create(chunk, uSize);
                //case 0x00041033: return iZTrashDelayFXParams.Create(chunk, uSize);
                default:
                    //Default "gap"
                    var instance = new AkPluginParam();
                    for (int i = 0; i < uSize; i++)
                        instance.pParamBlock.Add(chunk.ReadByte());
                    return instance;
            }
        }
    }
}