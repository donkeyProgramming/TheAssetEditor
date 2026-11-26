#nullable enable
namespace Editors.Audio.SoundTriggers
{
    public sealed record SoundTriggerTag(string SoundEvent, float StartTime, string File);

    public sealed class SoundTriggerSession(SoundTriggerResolver resolver, Func<uint, bool> prepare, Action<uint> play, Random random)
    {
        List<(double Start, SoundNode Sound)> _playable = [];
        List<uint> _leaves = [];
        readonly SoundPicker _picker = new(random);

        double _previous = double.NaN;
        bool _paused;
        bool _looped;

        public double OffsetSeconds { get; set; }

        public Dictionary<uint, uint> SwitchChoices { get; } = [];

        public IReadOnlyList<SwitchGroupSeen> Switches { get; private set; } = [];

        public IReadOnlyList<string> StatusLines { get; private set; } = [];

        public IEnumerable<uint> Leaves => _leaves;

        public void Rebuild(IReadOnlyList<SoundTriggerTag> tags, bool campaign)
        {
            var resolved = tags.Select(tag => (Tag: tag, Result: resolver.Resolve(tag.SoundEvent, campaign, SwitchChoices))).ToList();

            _playable = resolved
                .Where(x => x.Result.Sound != null)
                .Select(x => (Start: (double)x.Tag.StartTime, Sound: x.Result.Sound!))
                .ToList();
            _leaves = _playable.SelectMany(x => SoundNodes.Leaves(x.Sound)).Distinct().ToList();
            foreach (var id in _leaves)
                prepare(id);

            Switches = resolved.SelectMany(x => x.Result.Switches).DistinctBy(x => x.GroupId).ToList();

            var silent = resolved.Where(x => x.Result.Silent != null).ToList();
            var unsupported = silent.Count(x => x.Result.Silent!.StartsWith(SoundTriggerResolver.NotSupported, StringComparison.Ordinal));
            var summary = resolved.Count == 0
                ? "0 triggers"
                : $"{resolved.Count} {(resolved.Count == 1 ? "trigger" : "triggers")}: {_playable.Count} {(_playable.Count == 1 ? "plays" : "play")}, {silent.Count - unsupported} silent in game"
                    + (unsupported == 0 ? "" : $", {unsupported} not supported");
            StatusLines = [summary, .. silent.Select(x => $"{x.Tag.SoundEvent} ({x.Tag.File}): {x.Result.Silent}")];
        }

        public void Advance(double now, bool playing, double elapsedSeconds, double speed, double length, bool loop)
        {
            var (previous, paused, looped) = (_previous, _paused, _looped);
            _previous = now;
            _paused = !playing;
            // The player reports the step that reaches the clip's end as paused.
            var reachedTheEnd = !playing && !paused && !looped && now >= length;
            if (!playing && !reachedTheEnd)
                return;
            _looped = false;

            var step = looped ? (length, now, true) : SoundTriggerCrossing.Step(previous, now, length, elapsedSeconds, speed);
            if (paused && !looped && step == (previous, now, false))
                step = (previous - 1e-6, now, false);
            Fire(step, OffsetSeconds * speed, length, loop && !reachedTheEnd);
        }

        public void Loop(double speed, double length)
        {
            if (!double.IsNaN(_previous))
                Fire((_paused ? _previous - 1e-6 : _previous, length, false), OffsetSeconds * speed, length, true);
            _previous = 0;
            _looped = true;
        }

        void Fire((double From, double To, bool Wrapped) step, double offset, double length, bool loop)
        {
            foreach (var (start, sound) in _playable)
            {
                if (!Fires(start, offset, step, length, loop))
                    continue;

                foreach (var id in _picker.Pick(sound))
                    play(id);
            }
        }

        static bool Fires(double tagStart, double offset, (double From, double To, bool Wrapped) step, double length, bool loop)
        {
            if (tagStart < 0 || tagStart > length)
                return false;

            var due = tagStart - offset;
            var dueInClip = !loop ? Math.Clamp(due, 0, length)
                : due >= 0 && due <= length ? due
                : due - length * Math.Floor(due / length);
            if (SoundTriggerCrossing.Crossed(dueInClip, step, length))
                return true;

            return due < 0 && !step.Wrapped && step.From < 0 && step.To >= 0;
        }
    }
}
