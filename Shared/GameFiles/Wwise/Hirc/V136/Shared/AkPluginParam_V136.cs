using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkPluginParam_V136
    {
        public List<byte> ParamBlock { get; set; } = [];
        //used for default case, "gap" of bytes

        public static AkPluginParam_V136 Create(ByteChunk chunk, uint plugin_id, uint uSize)
        {
            switch (plugin_id)
            {
            //Only SrcSilence is used for WH3... maybe...
            //Actually the default case would take care of any of them, so ehhh
                //case 0x00640002: return CAkFxSrcSineParams.Create(chunk, uSize);
                case 0x00650002: return CAkFxSrcSilenceParams_V136.Create(chunk);
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
                    // Default "gap"
                    var akPluginParam_V136 = new AkPluginParam_V136();
                    akPluginParam_V136.ParamBlock = new List<byte>((int)uSize);
                    for (var i = 0; i < uSize; i++)
                        akPluginParam_V136.ParamBlock.Add(chunk.ReadByte());
                    return akPluginParam_V136;
            }
        }

        public class CAkFxSrcSilenceParams_V136 : AkPluginParam_V136
        {
            public float Duration { get; set; }
            public float RandomizedLengthMinus { get; set; }
            public float RandomizedLengthPlus { get; set; }

            public static CAkFxSrcSilenceParams_V136 Create(ByteChunk chunk)
            {
                return new CAkFxSrcSilenceParams_V136
                {
                    Duration = chunk.ReadSingle(),
                    RandomizedLengthMinus = chunk.ReadSingle(),
                    RandomizedLengthPlus = chunk.ReadSingle()
                };
            }
        }
    }
}
