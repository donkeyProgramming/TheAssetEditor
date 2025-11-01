using System;
using Editors.Audio.Shared.Wwise;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class HircSettings
    {
        public ContainerType ContainerType { get; set; }
        public RandomType RandomType { get; set; }
        public bool EnableRepetitionInterval { get; set; }
        public uint RepetitionInterval { get; set; }
        public PlaylistEndBehaviour PlaylistEndBehaviour { get; set; }
        public bool AlwaysResetPlaylist { get; set; }
        public PlayMode PlayMode { get; set; }
        public LoopingType LoopingType { get; set; }
        public uint NumberOfLoops { get; set; }
        public TransitionType TransitionType { get; set; }
        public decimal TransitionDuration { get; set; }

        public static HircSettings CreateSoundSettings(HircSettings hircSettings)
        {
            return new HircSettings
            {
                LoopingType = hircSettings.LoopingType,
                NumberOfLoops = hircSettings.NumberOfLoops,
            };
        }

        public static HircSettings CreateSoundSettings()
        {
            return new HircSettings
            {
                LoopingType = LoopingType.Disabled,
                NumberOfLoops = 1,
            };
        }

        public static HircSettings CreateRandomSequenceContainerSettings(HircSettings hircSettings)
        {
            return new HircSettings
            {
                ContainerType = hircSettings.ContainerType,
                EnableRepetitionInterval = hircSettings.EnableRepetitionInterval,
                RepetitionInterval = hircSettings.RepetitionInterval,
                PlaylistEndBehaviour = hircSettings.PlaylistEndBehaviour,
                AlwaysResetPlaylist = hircSettings.AlwaysResetPlaylist,
                PlayMode = hircSettings.PlayMode,
                LoopingType = hircSettings.LoopingType,
                NumberOfLoops = hircSettings.NumberOfLoops,
                TransitionType = hircSettings.TransitionType,
                TransitionDuration = hircSettings.TransitionDuration
            };
        }

        public static HircSettings CreateRecommendedRandomSequenceContainerSettings(int audioFilesCount)
        {
            return new HircSettings
            {
                ContainerType = ContainerType.Random,
                RandomType = RandomType.Shuffle,
                EnableRepetitionInterval = true,
                RepetitionInterval = (uint)Math.Ceiling(audioFilesCount / 2.0),
                PlaylistEndBehaviour = PlaylistEndBehaviour.Restart,
                AlwaysResetPlaylist = true,
                PlayMode = PlayMode.Step,
                LoopingType = LoopingType.Disabled,
                NumberOfLoops = 1,
                TransitionType = TransitionType.Disabled,
                TransitionDuration = 1
            };
        }
    }
}
