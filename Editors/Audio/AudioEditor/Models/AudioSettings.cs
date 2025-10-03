using System;
using static Editors.Audio.AudioEditor.Settings.Settings;

namespace Editors.Audio.AudioEditor.Models
{
    public class AudioSettings
    {
        public PlaylistType PlaylistType { get; set; }
        public bool EnableRepetitionInterval { get; set; }
        public uint RepetitionInterval { get; set; }
        public EndBehaviour EndBehaviour { get; set; }
        public bool AlwaysResetPlaylist { get; set; }
        public PlaylistMode PlaylistMode { get; set; }
        public LoopingType LoopingType { get; set; }
        public uint NumberOfLoops { get; set; }
        public TransitionType TransitionType { get; set; }
        public decimal TransitionDuration { get; set; }

        public static AudioSettings CreateSoundSettings(AudioSettings audioSettings)
        {
            return new AudioSettings
            {
                LoopingType = audioSettings.LoopingType,
                NumberOfLoops = audioSettings.NumberOfLoops,
            };
        }

        public static AudioSettings CreateSoundSettings()
        {
            return new AudioSettings
            {
                LoopingType = LoopingType.Disabled,
                NumberOfLoops = 1,
            };
        }

        public static AudioSettings CreateRandomSequenceContainerSettings(AudioSettings audioSettings)
        {
            return new AudioSettings
            {
                PlaylistType = audioSettings.PlaylistType,
                EnableRepetitionInterval = audioSettings.EnableRepetitionInterval,
                RepetitionInterval = audioSettings.RepetitionInterval,
                EndBehaviour = audioSettings.EndBehaviour,
                AlwaysResetPlaylist = audioSettings.AlwaysResetPlaylist,
                PlaylistMode = audioSettings.PlaylistMode,
                LoopingType = audioSettings.LoopingType,
                NumberOfLoops = audioSettings.NumberOfLoops,
                TransitionType = audioSettings.TransitionType,
                TransitionDuration = audioSettings.TransitionDuration
            };
        }

        public static AudioSettings CreateRecommendedRandomSequenceContainerSettings(int audioFilesCount)
        {
            return new AudioSettings
            {
                PlaylistType = PlaylistType.RandomExhaustive,
                EnableRepetitionInterval = true,
                RepetitionInterval = (uint)Math.Ceiling(audioFilesCount / 2.0),
                EndBehaviour = EndBehaviour.Restart,
                AlwaysResetPlaylist = true,
                PlaylistMode = PlaylistMode.Step,
                LoopingType = LoopingType.Disabled,
                NumberOfLoops = 1,
                TransitionType = TransitionType.Disabled,
                TransitionDuration = 1
            };
        }
    }
}
