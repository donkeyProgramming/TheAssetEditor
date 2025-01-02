using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using static Shared.GameFormats.Wwise.Hirc.V136.CAkRanSeqCntr_V136TEMP.CAkPlayList_V136;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class RandomContainerGenerator : IWwiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(RandomContainer);

        public HircItem ConvertToWwise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as RandomContainer;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWwise(typedProjectItem, project);
        }

        public static CAkRanSeqCntr_V136TEMP ConvertToWwise(RandomContainer inputContainer, CompilerData project)
        {
            var wwiseRandomContainer = new CAkRanSeqCntr_V136TEMP();
            wwiseRandomContainer.Id = inputContainer.Id;
            wwiseRandomContainer.HircType = AkBkHircType.SequenceContainer;
            wwiseRandomContainer.NodeBaseParams = CreateBaseNodeParams();
            wwiseRandomContainer.BitVector = 0x12;
            wwiseRandomContainer.TransitionTime = 1000;
            wwiseRandomContainer.NodeBaseParams.DirectParentId = inputContainer.DirectParentId;
            wwiseRandomContainer.LoopCount = 1;
            wwiseRandomContainer.AvoidRepeatCount = 2;

            var allChildIds = inputContainer.Children.Select(x => x).OrderBy(x => x).ToList();
            wwiseRandomContainer.Children = CreateChildrenList(allChildIds);
            wwiseRandomContainer.CAkPlayList.Playlist = allChildIds.Select(CreateAkPlaylistItem).ToList();

            wwiseRandomContainer.UpdateSectionSize();

            return wwiseRandomContainer;
        }

        private static AkPlaylistItem_V136 CreateAkPlaylistItem(uint childId)
        {
            return new AkPlaylistItem_V136
            {
                PlayId = childId,
                Weight = 50000
            };
        }

        private static Children_V136 CreateChildrenList(List<uint> childIds)
        {
            return new Children_V136
            {
                ChildIds = childIds
            };
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
            instance.BitVector = 0x02;
            instance.NodeInitialParams = new NodeInitialParams_V136()
            {
                AkPropBundle0 = new AkPropBundle_V136()
                {
                    PropsList = new List<AkPropBundle_V136.PropBundleInstance_V136>()
                    {
                    }
                },
                AkPropBundle1 = new AkPropBundleMinMax_V136()
                {
                    Values = new List<AkPropBundleMinMax_V136.AkPropBundleInstance_V136>()
                }
            };

            instance.PositioningParams = new PositioningParams_V136()
            {
                BitsPositioning = 0x00,
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
                BelowThresholdBehavior = 0x02,
                BitVector2 = 0
            };
            instance.StateChunk = new StateChunk_V136();
            instance.InitialRtpc = new InitialRtpc_V136();
            return instance;
        }
    }
}
