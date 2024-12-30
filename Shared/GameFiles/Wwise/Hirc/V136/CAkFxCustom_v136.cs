using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkFxCustom_v136 : HircItem
    {
        public uint PluginId { get; set; }
        public AkPluginParam AkPluginParam { get; set; }
        public List<AkMediaMap> MediaList { get; set; } = [];
        public InitialRtpc InitialRtpc { get; set; }
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

            InitialRtpc = InitialRtpc.Create(chunk);
            StateChunk = StateChunk.Create(chunk);

            var numValues = chunk.ReadShort();
            for (var i = 0; i < numValues; i++)
                PropertyValuesList.Add(PluginPropertyValue.Create(chunk));
        }

        public override void UpdateSectionSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class AkMediaMap
    {
        public byte Index { get; set; }
        public uint SourceId { get; set; }

        public static AkMediaMap Create(ByteChunk chunk)
        {
            var instance = new AkMediaMap();
            instance.Index = chunk.ReadByte();
            instance.SourceId = chunk.ReadUInt32();
            return instance;
        }
    }

    public class PluginPropertyValue
    {
        public uint PropertyId { get; set; }
        public byte RtpcAccum { get; set; }
        public float FValue { get; set; }

        public static PluginPropertyValue Create(ByteChunk chunk)
        {
            var instance = new PluginPropertyValue();
            instance.PropertyId = chunk.ReadUInt32();
            instance.RtpcAccum = chunk.ReadByte();
            instance.FValue = chunk.ReadSingle();
            return instance;
        }
    }

    public class CAkFxSrcSilenceParams : AkPluginParam
    {
        public float FDuration { get; set; }
        public float FRandomizedLengthMinus { get; set; }
        public float FRandomizedLengthPlus { get; set; }

        public static CAkFxSrcSilenceParams Create(ByteChunk chunk, uint uSize)
        {
            var instance = new CAkFxSrcSilenceParams();
            instance.FDuration = chunk.ReadSingle();
            instance.FRandomizedLengthMinus = chunk.ReadSingle();
            instance.FRandomizedLengthPlus = chunk.ReadSingle();
            return instance;
        }
    }

    public class AkPluginParam
    {
        public List<byte> PParamBlock { get; set; } = [];
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
                    for (var i = 0; i < uSize; i++)
                        instance.PParamBlock.Add(chunk.ReadByte());
                    return instance;
            }
        }
    }
}
