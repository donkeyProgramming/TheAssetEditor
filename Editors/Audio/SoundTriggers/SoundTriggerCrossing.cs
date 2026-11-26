#nullable enable
namespace Editors.Audio.SoundTriggers
{
    public static class SoundTriggerCrossing
    {
        public const double SlackSeconds = 0.05;

        public const double MaxFrameSeconds = 0.1;

        public static (double From, double To, bool Wrapped) Step(double previous, double now, double length, double elapsedSeconds, double speed)
        {
            var frame = Math.Min(elapsedSeconds, MaxFrameSeconds) * speed;
            var allowed = frame + SlackSeconds;

            var ahead = now - previous;
            if (ahead >= 0 && ahead <= allowed)
                return (previous, now, false);
            if (now < previous && length - previous + now <= allowed)
                return (previous, now, true);

            // The nudge keeps a tag exactly at the last frame's start inside the span.
            return (now - frame - 1e-6, now, false);
        }

        public static bool Crossed(double start, (double From, double To, bool Wrapped) step, double length)
        {
            if (start < 0 || start > length)
                return false;

            return step.Wrapped
                ? start > step.From || start <= step.To
                : start > step.From && start <= step.To;
        }
    }
}
