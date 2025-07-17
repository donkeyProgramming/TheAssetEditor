using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Settings
{
    public class Settings
    {
        public enum PlaylistType
        {
            Random,
            RandomExhaustive,
            Sequence
        }

        public const string Random = "Random";
        public const string RandomExhaustive = "Random Exhaustive";
        public const string Sequence = "Sequence";

        public static Dictionary<PlaylistType, string> PlaylistTypeStringLookup { get; } = new()
        {
            { PlaylistType.Random, Random },
            { PlaylistType.RandomExhaustive, RandomExhaustive },
            { PlaylistType.Sequence, Sequence }
        };

        public enum PlaylistMode
        {
            Step,
            Continuous
        }

        public const string Step = "Step";
        public const string Continous = "Continuous";

        public static Dictionary<PlaylistMode, string> PlaylistModeStringLookup { get; } = new()
        {
            { PlaylistMode.Continuous, Continous },
            { PlaylistMode.Step, Step }
        };

        public enum EndBehaviour
        {
            Restart,
            PlayInReverseOrder
        }

        public const string Restart = "Restart";
        public const string PlayInReverseOrder = "Play In Reverse Order";

        public static Dictionary<EndBehaviour, string> EndBehaviourStringLookup { get; } = new()
        {
            { EndBehaviour.Restart, Restart },
            { EndBehaviour.PlayInReverseOrder, PlayInReverseOrder}
        };

        public enum LoopingType
        {
            Disabled,
            FiniteLooping,
            InfiniteLooping,
        }

        public const string LoopingTypeDisabled = "Disabled";
        public const string FiniteLooping = "Finite Looping";
        public const string InfiniteLooping = "Infinite Looping";

        public static Dictionary<LoopingType, string> LoopingTypeStringLookup { get; } = new()
        {
            { LoopingType.Disabled, LoopingTypeDisabled },
            { LoopingType.FiniteLooping, FiniteLooping },
            { LoopingType.InfiniteLooping, InfiniteLooping }
        };

        public enum TransitionType
        {
            Disabled,
            XfadeAmp,
            XfadePower,
            Delay,
            SampleAccurate,
            TriggerRate
        }

        public const string TransitionTypeDisabled = "Disabled";
        public const string XfadeAmp = "Xfade (amp)";
        public const string XfadePower = "Xfade (power)";
        public const string Delay = "Delay";
        public const string SampleAccurate = "Sample Accurate";
        public const string TriggerRate = "Trigger Rate";

        public static Dictionary<TransitionType, string> TransitionTypeStringLookup { get; } = new()
        {
            { TransitionType.Disabled, TransitionTypeDisabled },
            { TransitionType.XfadeAmp, XfadeAmp },
            { TransitionType.XfadePower, XfadePower },
            { TransitionType.Delay, Delay },
            { TransitionType.SampleAccurate, SampleAccurate },
            { TransitionType.TriggerRate, TriggerRate }
        };
    }
}
