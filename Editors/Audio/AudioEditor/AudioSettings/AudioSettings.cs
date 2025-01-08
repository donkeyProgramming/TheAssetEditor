using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.AudioSettings
{
    public class AudioSettings
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

        public static readonly Dictionary<PlaylistType, string> PlaylistTypeToStringMap = new()
        {
            { PlaylistType.Random, Random },
            { PlaylistType.RandomExhaustive, RandomExhaustive },
            { PlaylistType.Sequence, Sequence }
        };

        public enum PlaylistMode
        {
            Continous,
            Step
        }

        public const string Continous = "Continous";
        public const string Step = "Step";

        public static readonly Dictionary<PlaylistMode, string> PlaylistModeToStringMap = new()
        {
            { PlaylistMode.Continous, Continous },
            { PlaylistMode.Step, Step }
        };

        public enum EndBehaviour
        {
            Restart,
            PlayInReverseOrder
        }

        public const string Restart = "Restart";
        public const string PlayInReverseOrder = "Play In Reverse Order";

        public static readonly Dictionary<EndBehaviour, string> EndBehaviourToStringMap = new()
        {
            { EndBehaviour.Restart, Restart },
            { EndBehaviour.PlayInReverseOrder, PlayInReverseOrder}
        };

        public enum Transition
        {
            XfadeAmp,
            XfadePower,
            Delay,
            SampleAccurate,
            TriggerRate
        }

        public const string XfadeAmp = "Xfade (amp)";
        public const string XfadePower = "Xfade (power)";
        public const string Delay = "Delay";
        public const string SampleAccurate = "Sample Accurate";
        public const string TriggerRate = "Trigger Rate";

        public static readonly Dictionary<Transition, string> TransitionToStringMap = new()
        {
            { Transition.XfadeAmp, XfadeAmp },
            { Transition.XfadePower, XfadePower },
            { Transition.Delay, Delay },
            { Transition.SampleAccurate, SampleAccurate },
            { Transition.TriggerRate, TriggerRate }
        };
    }
}
