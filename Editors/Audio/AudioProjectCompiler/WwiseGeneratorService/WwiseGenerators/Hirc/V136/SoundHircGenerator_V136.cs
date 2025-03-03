using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using static Shared.GameFormats.Wwise.Hirc.V136.Shared.AkBankSourceData_V136;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class SoundHircGenerator_V136 : IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank)
        {
            var audioProjectSound = audioProjectItem as Sound;
            var soundHirc = CreateSoundHirc(audioProjectSound);
            soundHirc.AkBankSourceData = CreateAkBankSourceData(audioProjectSound);
            soundHirc.NodeBaseParams = CreateNodeBaseParams(audioProjectSound);
            soundHirc.UpdateSectionSize();
            return soundHirc;
        }

        private static CAkSound_V136 CreateSoundHirc(Sound audioProjectSound)
        {
            return new CAkSound_V136()
            {
                ID = audioProjectSound.ID,
                HircType = audioProjectSound.HircType,
            };
        }

        private static AkBankSourceData_V136 CreateAkBankSourceData(Sound audioProjectSound)
        {
            return new AkBankSourceData_V136()
            {
                PluginId = 0x00040001,
                StreamType = AKBKSourceType.Streaming,
                AkMediaInformation = new AkMediaInformation_V136()
                {
                    SourceID = audioProjectSound.SourceID,
                    InMemoryMediaSize = (uint)audioProjectSound.InMemoryMediaSize,
                    SourceBits = 0x01, //TODO: Update this to include a reference to the language i.e. if it's sfx 
                }
            };
        }

        private static NodeBaseParams_V136 CreateNodeBaseParams(Sound audioProjectSound)
        {
            var nodeBaseParams = new NodeBaseParams_V136();
            nodeBaseParams.NodeInitialFxParams = new NodeInitialFxParams_V136()
            {
                IsOverrideParentFx = 0,
                NumFx = 0,
            };
            nodeBaseParams.OverrideAttachmentParams = 0;
            nodeBaseParams.OverrideBusID = 0;
            nodeBaseParams.DirectParentID = audioProjectSound.DirectParentID;
            nodeBaseParams.BitVector = 0;
            nodeBaseParams.NodeInitialParams = new NodeInitialParams_V136()
            {
                AkPropBundle0 = new AkPropBundle_V136() { PropsList = new List<AkPropBundle_V136.PropBundleInstance_V136>() },
                AkPropBundle1 = new AkPropBundleMinMax_V136() { PropsList = new List<AkPropBundleMinMax_V136.AkPropBundleInstance_V136>() }
            };
            nodeBaseParams.PositioningParams = new PositioningParams_V136()
            {
                BitsPositioning = 0x00
            };
            nodeBaseParams.AuxParams = new AuxParams_V136()
            {
                BitVector = 0,
                ReflectionsAuxBus = 0
            };
            nodeBaseParams.AdvSettingsParams = new AdvSettingsParams_V136()
            {
                BitVector = 0x00,
                VirtualQueueBehavior = 0x01,
                MaxNumInstance = 0,
                BelowThresholdBehavior = 0,
                BitVector2 = 0x00
            };
            nodeBaseParams.StateChunk = new StateChunk_V136();
            nodeBaseParams.InitialRtpc = new InitialRtpc_V136();
            return nodeBaseParams;
        }
    }
}
