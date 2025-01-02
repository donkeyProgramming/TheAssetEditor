using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.Core.PackFiles;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using static Shared.GameFormats.Wwise.Hirc.V136.Shared.AkBankSourceData_V136;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class SoundGenerator : IWwiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(Sound);

        private readonly IPackFileService _pfs;

        public SoundGenerator(IPackFileService pfs)
        {
            _pfs = pfs;
        }

        public HircItem ConvertToWwise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as Sound;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWwise(typedProjectItem, project);
        }

        public CAkSound_v136 ConvertToWwise(Sound inputSound, CompilerData project)
        {
            var filePath = inputSound.FilePath;
            var file = _pfs.FindFile(filePath);
            var nodeBaseParams = CreateBaseNodeParams();
            var wavFile = Path.GetFileName(filePath);
            var wavFileName = wavFile.Replace(".wem", "");

            var wwiseSound = new CAkSound_v136()
            {
                Id = inputSound.Id,
                HircType = AkBkHircType.Sound,
                AkBankSourceData = new AkBankSourceData_V136()
                {
                    PluginId = 0x00040001, // [VORBIS]
                    StreamType = AKBKSourceType.Streaming,
                    AkMediaInformation = new AkMediaInformation_V136()
                    {
                        SourceId = uint.Parse(wavFileName),
                        InMemoryMediaSize = (uint)file.DataSource.Size,
                        SourceBits = 0x01,
                    }
                },
                NodeBaseParams = nodeBaseParams
            };

            wwiseSound.NodeBaseParams.DirectParentId = inputSound.DirectParentId;

            // Applying attenuation directly to sounds is necessary as they don't appear to use the vanilla mixer's attenuation even though they're being routed through it.
            var attenuationId = inputSound.Attenuation;

            if (attenuationId != 0)
            {
                wwiseSound.NodeBaseParams.NodeInitialParams.AkPropBundle0 = new AkPropBundle_V136()
                {
                    PropsList = new List<AkPropBundle_V136.PropBundleInstance_V136>()
                    {
                        new(){Id = AkPropId_V136.AttenuationID, Value = attenuationId}
                    }
                };
            }

            wwiseSound.UpdateSectionSize();

            return wwiseSound;
        }

        public List<CAkSound_v136> ConvertToWwise(IEnumerable<Sound> inputSound, CompilerData project)
        {
            return inputSound.Select(x => ConvertToWwise(x, project)).ToList();
        }

        public static NodeBaseParams_V136 CreateBaseNodeParams()
        {
            var instance = new NodeBaseParams_V136();
            instance.NodeInitialFxParams = new NodeInitialFxParams_V136()
            {
                IsOverrideParentFx = 0,
                FxChunk = new List<FxChunk_V136>(),
                BitsFxBypass = 0,
            };
            instance.OverrideAttachmentParams = 0;
            instance.OverrideBusId = 0;
            instance.DirectParentId = 0;
            instance.BitVector = 0;
            instance.NodeInitialParams = new NodeInitialParams_V136()
            {
                AkPropBundle0 = new AkPropBundle_V136()
                {
                    PropsList = new List<AkPropBundle_V136.PropBundleInstance_V136>()
                    {
                        //new(){Type = AkPropBundleType.StatePropNum_Priority, Value = 100},
                        //new(){Type = AkPropBundleType.UserAuxSendVolume0, Value = -96},
                        //new(){Type = AkPropBundleType.InitialDelay, Value = 0.5199999809265137f},
                    }
                },
                AkPropBundle1 = new AkPropBundleMinMax_V136()
                {
                    Values = new List<AkPropBundleMinMax_V136.AkPropBundleInstance_V136>()
                }
            };
            instance.PositioningParams = new PositioningParams_V136()
            {
                BitsPositioning = 0x03,
                Bits3d = 0x08
            };
            instance.AuxParams = new AuxParams_V136()
            {
                BitVector = 0,
                ReflectionsAuxBus = 0
            };
            instance.AdvSettingsParams = new AdvSettingsParams_V136()
            {
                BitVector = 0x00,
                VirtualQueueBehavior = 0x01,
                MaxNumInstance = 0,
                BelowThresholdBehavior = 0,
                BitVector2 = 0
            };
            instance.StateChunk = new StateChunk_V136();
            instance.InitialRtpc = new InitialRtpc_V136();
            return instance;
        }
    }
}
