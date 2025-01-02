using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Shared.GameFormats.Wwise.Hirc.V136;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class ActorMixerGenerator : IWwiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(ActorMixer);

        public HircItem ConvertToWwise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as ActorMixer;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWwise(typedProjectItem, project);
        }

        public static CAkActorMixer_V136TEMP ConvertToWwise(ActorMixer actorMixer, CompilerData project)
        {
            var wwiseActorMixer = new CAkActorMixer_V136TEMP();
            wwiseActorMixer.Id = actorMixer.Id;
            wwiseActorMixer.HircType = AkBkHircType.ActorMixer;
            wwiseActorMixer.NodeBaseParams = CreateBaseNodeParams();
            wwiseActorMixer.NodeBaseParams.DirectParentId = actorMixer.DirectParentId;

            var allChildIds = actorMixer.Children.ToList();

            wwiseActorMixer.Children = new Children_V136()
            {
                ChildIds = allChildIds
            };

            wwiseActorMixer.UpdateSectionSize();

            return wwiseActorMixer;
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
