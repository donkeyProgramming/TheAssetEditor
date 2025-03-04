using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;
using static Shared.GameFormats.Wwise.Hirc.V136.CAkRanSeqCntr_V136.CAkPlayList_V136;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class RanSeqCntrHircGenerator_V136 : IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank)
        {
            var audioProjectRandomSequenceContainer = audioProjectItem as RandomSequenceContainer;

            var randomSequenceContainerHirc = CreateRandomSequenceContainerHirc(audioProjectRandomSequenceContainer);
            randomSequenceContainerHirc.NodeBaseParams = CreateNodeBaseParams(audioProjectRandomSequenceContainer);

            if (audioProjectRandomSequenceContainer.AudioSettings.LoopingType == LoopingType.FiniteLooping)
                randomSequenceContainerHirc.LoopCount = (ushort)audioProjectRandomSequenceContainer.AudioSettings.NumberOfLoops;
            else if (audioProjectRandomSequenceContainer.AudioSettings.LoopingType == LoopingType.InfiniteLooping)
                randomSequenceContainerHirc.LoopCount = 0;
            else if (audioProjectRandomSequenceContainer.AudioSettings.LoopingType == LoopingType.Disabled)
                randomSequenceContainerHirc.LoopCount = 1;

            randomSequenceContainerHirc.LoopModMin = 0;
            randomSequenceContainerHirc.LoopModMax = 0;

            if (audioProjectRandomSequenceContainer.AudioSettings.TransitionDuration != null)
                randomSequenceContainerHirc.TransitionTime = (ushort)audioProjectRandomSequenceContainer.AudioSettings.TransitionDuration * 1000;
            else
                randomSequenceContainerHirc.TransitionTime = 1 * 1000;

            randomSequenceContainerHirc.TransitionTimeModMin = 0;
            randomSequenceContainerHirc.TransitionTimeModMax = 0;

            if (audioProjectRandomSequenceContainer.AudioSettings.RepetitionInterval != null)
                randomSequenceContainerHirc.AvoidRepeatCount = (ushort)audioProjectRandomSequenceContainer.AudioSettings.RepetitionInterval;
            else
                randomSequenceContainerHirc.AvoidRepeatCount = 1;

            randomSequenceContainerHirc.TransitionMode = (byte)audioProjectRandomSequenceContainer.AudioSettings.TransitionType;

            if (audioProjectRandomSequenceContainer.AudioSettings.PlaylistType == PlaylistType.RandomExhaustive)
                randomSequenceContainerHirc.RandomMode = 1; // Shuffle
            else
                randomSequenceContainerHirc.RandomMode = 0; // Normal

            if (audioProjectRandomSequenceContainer.AudioSettings.PlaylistType == PlaylistType.Sequence)
                randomSequenceContainerHirc.Mode = 1; // Sequence
            else
                randomSequenceContainerHirc.Mode = 0; // Random

            var isUsingWeight = 0;

            var resetPlaylistAtEachPlay = 0;
            if (audioProjectRandomSequenceContainer.AudioSettings.AlwaysResetPlaylist)
                resetPlaylistAtEachPlay = 1;

            var isRestartBackwards = 0;
            if (audioProjectRandomSequenceContainer.AudioSettings.EndBehaviour == EndBehaviour.PlayInReverseOrder)
                isRestartBackwards = 1;

            var isContinous = 0;
            if (audioProjectRandomSequenceContainer.AudioSettings.PlaylistMode == PlaylistMode.Continuous)
                isContinous = 1;

            var isGlobal = 1;

            randomSequenceContainerHirc.BitVector = (byte)(
                isGlobal << 4 |
                isContinous << 3 |
                isRestartBackwards << 2 |
                resetPlaylistAtEachPlay << 1 |
                isUsingWeight);


            randomSequenceContainerHirc.Children = CreateChildrenList(audioProjectRandomSequenceContainer.Sounds);
            randomSequenceContainerHirc.CAkPlayList.Playlist = CreateAkPlaylistItem(audioProjectRandomSequenceContainer.Sounds);

            randomSequenceContainerHirc.UpdateSectionSize();

            return randomSequenceContainerHirc;
        }

        private static CAkRanSeqCntr_V136 CreateRandomSequenceContainerHirc(RandomSequenceContainer audioProjectRandomSequenceContainer)
        {
            return new CAkRanSeqCntr_V136()
            {
                ID = audioProjectRandomSequenceContainer.ID,
                HircType = audioProjectRandomSequenceContainer.HircType,
            };
        }

        private static NodeBaseParams_V136 CreateNodeBaseParams(RandomSequenceContainer audioProjectRandomSequenceContainer)
        {
            var nodeBaseParams = new NodeBaseParams_V136();
            nodeBaseParams.NodeInitialFxParams = new NodeInitialFxParams_V136()
            {
                IsOverrideParentFx = 0,
                NumFx = 0,
            };
            nodeBaseParams.OverrideAttachmentParams = 0;
            nodeBaseParams.OverrideBusID = 0;
            nodeBaseParams.DirectParentID = audioProjectRandomSequenceContainer.DirectParentID;
            nodeBaseParams.BitVector = 0;
            nodeBaseParams.NodeInitialParams = new NodeInitialParams_V136()
            {
                AkPropBundle0 = new AkPropBundle_V136() { PropsList = new List<AkPropBundle_V136.PropBundleInstance_V136>() },
                AkPropBundle1 = new AkPropBundleMinMax_V136() { PropsList = new List<AkPropBundleMinMax_V136.AkPropBundleInstance_V136>() }
            };
            nodeBaseParams.PositioningParams = new PositioningParams_V136()
            {
                BitsPositioning = 0x00,
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

        private static Children_V136 CreateChildrenList(List<Sound> sounds)
        {
            var childIds = sounds
                .Select(sound => sound.ID)
                .ToList();

            return new Children_V136
            {
                ChildIds = childIds
            };
        }

        private static List<AkPlaylistItem_V136> CreateAkPlaylistItem(List<Sound> sounds)
        {
            var playlist = new List<AkPlaylistItem_V136>();
            foreach (var sound in sounds)
            {
                var playlistItem = new AkPlaylistItem_V136
                {
                    PlayId = sound.ID,
                    Weight = 50000
                };
                playlist.Add(playlistItem);
            }
            return playlist;
        }
    }
}
