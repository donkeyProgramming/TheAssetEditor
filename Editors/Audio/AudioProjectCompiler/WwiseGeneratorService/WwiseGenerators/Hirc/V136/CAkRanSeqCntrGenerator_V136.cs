using Editors.Audio.AudioEditor.Models;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using static Editors.Audio.AudioEditor.Settings.Settings;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class CAkRanSeqCntrGenerator_V136 : IWwiseHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null)
        {
            var audioProjectRandomSequenceContainer = audioProjectItem as RandomSequenceContainer;

            var randomSequenceContainerHirc = CreateRandomSequenceContainerHirc(audioProjectRandomSequenceContainer);
            randomSequenceContainerHirc.NodeBaseParams = NodeBaseParamsGenerator_V136.CreateNodeBaseParams(audioProjectRandomSequenceContainer);

            if (audioProjectRandomSequenceContainer.AudioSettings.LoopingType == LoopingType.FiniteLooping)
                randomSequenceContainerHirc.LoopCount = (ushort)audioProjectRandomSequenceContainer.AudioSettings.NumberOfLoops;
            else if (audioProjectRandomSequenceContainer.AudioSettings.LoopingType == LoopingType.InfiniteLooping)
                randomSequenceContainerHirc.LoopCount = 0;
            else if (audioProjectRandomSequenceContainer.AudioSettings.LoopingType == LoopingType.Disabled)
                randomSequenceContainerHirc.LoopCount = 1;

            randomSequenceContainerHirc.LoopModMin = 0;
            randomSequenceContainerHirc.LoopModMax = 0;

            if (audioProjectRandomSequenceContainer.AudioSettings.TransitionDuration != 1)
                randomSequenceContainerHirc.TransitionTime = (ushort)audioProjectRandomSequenceContainer.AudioSettings.TransitionDuration * 1000;
            else
                randomSequenceContainerHirc.TransitionTime = 1 * 1000;

            randomSequenceContainerHirc.TransitionTimeModMin = 0;
            randomSequenceContainerHirc.TransitionTimeModMax = 0;

            if (audioProjectRandomSequenceContainer.AudioSettings.RepetitionInterval != 1)
                randomSequenceContainerHirc.AvoidRepeatCount = (ushort)audioProjectRandomSequenceContainer.AudioSettings.RepetitionInterval;
            else
                randomSequenceContainerHirc.AvoidRepeatCount = 1;

            randomSequenceContainerHirc.TransitionMode = (byte)audioProjectRandomSequenceContainer.AudioSettings.TransitionType;

            if (audioProjectRandomSequenceContainer.AudioSettings.PlaylistType == PlaylistType.RandomExhaustive)
                randomSequenceContainerHirc.RandomMode = 1;
            else
                randomSequenceContainerHirc.RandomMode = 0;

            if (audioProjectRandomSequenceContainer.AudioSettings.PlaylistType == PlaylistType.Sequence)
                randomSequenceContainerHirc.Mode = 1;
            else
                randomSequenceContainerHirc.Mode = 0;

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

            randomSequenceContainerHirc.BitVector = (byte)
            (
                isGlobal << 4 |
                isContinous << 3 |
                isRestartBackwards << 2 |
                resetPlaylistAtEachPlay << 1 |
                isUsingWeight
            );

            var sounds = soundBank.GetSounds(audioProjectRandomSequenceContainer.SoundReferences);
            randomSequenceContainerHirc.Children = ChildrenGenerator_V136.CreateChildrenList(sounds);
            randomSequenceContainerHirc.CAkPlayList.Playlist = AkPlaylistItemGenerator_V136.CreateAkPlaylistItem(sounds);

            randomSequenceContainerHirc.UpdateSectionSize();

            return randomSequenceContainerHirc;
        }

        private static CAkRanSeqCntr_V136 CreateRandomSequenceContainerHirc(RandomSequenceContainer audioProjectRandomSequenceContainer)
        {
            return new CAkRanSeqCntr_V136()
            {
                Id = audioProjectRandomSequenceContainer.Id,
                HircType = audioProjectRandomSequenceContainer.HircType,
            };
        }
    }
}
