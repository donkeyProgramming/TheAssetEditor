using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkPluginParam_V136
    {
        public List<byte> ParamBlock { get; set; } = [];
        //used for default case, "gap" of bytes

        public static AkPluginParam_V136 ReadData(ByteChunk chunk, uint plugin_id, uint uSize)
        {
            switch (plugin_id)
            {
            //Only SrcSilence is used for WH3... maybe...
            //Actually the default case would take care of any of them, so ehhh
                //case 0x00640002: return CAkFxSrcSineParams.ReadData(chunk, uSize);
                case 0x00650002: return CAkFxSrcSilenceParams_V136.ReadData(chunk);
                //case 0x00660002: return CAkToneGenParams.ReadData(chunk, uSize);
                //case 0x00690003: return CAkParameterEQFXParams.ReadData(chunk, uSize);
                //case 0x006A0003: return CAkDelayFXParams.ReadData(chunk, uSize);
                //case 0x006E0003: return CAkPeakLimiterFXParams.ReadData(chunk, uSize);
                //case 0x00730003: return CAkFDNReverbFXParams.ReadData(chunk, uSize);
                //case 0x00760003: return CAkRoomVerbFXParams.ReadData(chunk, uSize);
                //case 0x007D0003: return CAkFlangerFXParams.ReadData(chunk, uSize);
                //case 0x007E0003: return CAkGuitarDistortionFXParams.ReadData(chunk, uSize);
                //case 0x007F0003: return CAkConvolutionReverbFXParams.ReadData(chunk, uSize);
                //case 0x00810003: return CAkMeterFXParams.ReadData(chunk, uSize);
                //case 0x00870003: return CAkStereoDelayFXParams.ReadData(chunk, uSize);
                //case 0x008B0003: return CAkGainFXParams.ReadData(chunk, uSize);
                //case 0x00940002: return CAkSynthOneParams.ReadData(chunk, uSize);
                //case 0x00C80002: return CAkFxSrcAudioInputParams.ReadData(chunk, uSize);
                //case 0x00041033: return iZTrashDelayFXParams.ReadData(chunk, uSize);
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

            public static CAkFxSrcSilenceParams_V136 ReadData(ByteChunk chunk)
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
