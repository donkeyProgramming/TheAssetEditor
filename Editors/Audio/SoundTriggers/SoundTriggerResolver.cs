#nullable enable
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.SoundTriggers
{
    public abstract record SoundNode;

    public sealed record SoundLeaf(uint SourceId) : SoundNode;

    public sealed record SoundAll(IReadOnlyList<SoundNode> Children) : SoundNode;

    public sealed record SoundOneOf(IReadOnlyList<SoundNode> Children, uint Id = 0, IReadOnlyList<int>? Weights = null, int AvoidRepeat = 0,
        bool Shuffle = false, bool Sequence = false) : SoundNode;

    public static class SoundNodes
    {
        public static IEnumerable<uint> Leaves(SoundNode node) => node switch
        {
            SoundLeaf leaf => [leaf.SourceId],
            SoundAll all => all.Children.SelectMany(Leaves),
            SoundOneOf oneOf => oneOf.Children.SelectMany(Leaves),
            _ => [],
        };

        public static IReadOnlyList<uint> Pick(SoundNode node, Random random) => new SoundPicker(random).Pick(node);
    }

    public sealed class SoundPicker(Random random)
    {
        readonly Dictionary<uint, List<int>> _played = [];
        readonly Dictionary<uint, int> _next = [];

        public IReadOnlyList<uint> Pick(SoundNode node) => node switch
        {
            SoundLeaf leaf => [leaf.SourceId],
            SoundAll all => all.Children.SelectMany(Pick).ToList(),
            SoundOneOf { Children.Count: > 0 } oneOf => Pick(oneOf.Children[Choose(oneOf)]),
            _ => [],
        };

        int Choose(SoundOneOf container)
        {
            var count = container.Children.Count;
            if (container.Sequence)
            {
                var index = _next.GetValueOrDefault(container.Id) % count;
                _next[container.Id] = index + 1;
                return index;
            }

            if (!_played.TryGetValue(container.Id, out var played))
                _played[container.Id] = played = [];
            var avoided = played.TakeLast(Math.Min(container.AvoidRepeat, count - 1)).ToHashSet();
            if (container.Shuffle)
                avoided.UnionWith(played.TakeLast(played.Count % count));
            var allowed = Enumerable.Range(0, count).Where(x => !avoided.Contains(x)).ToList();
            var chosen = Weighted(allowed, container.Weights);
            played.Add(chosen);
            return chosen;
        }

        int Weighted(List<int> allowed, IReadOnlyList<int>? weights)
        {
            var total = weights == null ? 0 : allowed.Sum(x => (double)Math.Max(0, weights[x]));
            if (total <= 0)
                return allowed[random.Next(allowed.Count)];
            var at = random.NextDouble() * total;
            foreach (var index in allowed)
            {
                at -= Math.Max(0, weights![index]);
                if (at < 0)
                    return index;
            }
            return allowed[^1];
        }
    }

    public sealed record SwitchGroupSeen(uint GroupId, uint DefaultSwitch, IReadOnlyList<uint> Values);

    public sealed record SoundTriggerResult(string Tag, SoundNode? Sound, string? Silent, IReadOnlyList<SwitchGroupSeen> Switches);

    public sealed class SoundTriggerResolver(Func<string, AudioTagEvents?> tagRow, Func<uint, IEnumerable<HircItem>> hircs, Func<uint, bool> wemExists)
    {
        public const string NotSupported = "not supported: ";

        HircItem? Hirc(uint id) => hircs(id).FirstOrDefault();

        public SoundTriggerResult Resolve(string tag, bool campaign, IReadOnlyDictionary<uint, uint> switchChoices)
        {
            var row = tagRow(tag);
            if (row == null)
                return Silent(tag, "no audio_metadata_tags_tables row");

            var eventName = campaign ? row.CampaignStart : row.BattleStart;
            if (string.IsNullOrWhiteSpace(eventName))
                return Silent(tag, campaign ? "the row names no campaign event" : "the row names no battle event");

            var item = Hirc(WwiseHash.Compute(eventName));
            if (item == null)
                return Silent(tag, $"event {eventName} is in no loaded bank");
            if (item is not ICAkEvent actionEvent)
                return Silent(tag, NotSupported + item.HircType);

            var walk = new Walk(Hirc, wemExists, switchChoices);
            var plays = new List<SoundNode>();
            var playActions = 0;
            string? unsupported = null;
            uint? missing = null;
            foreach (var actionId in actionEvent.GetActionIds())
            {
                switch (Hirc(actionId))
                {
                    case null:
                        missing ??= actionId;
                        break;
                    case ICAkAction action when action.GetActionType() == AkActionType.Play:
                        playActions++;
                        if (walk.Visit(action.GetChildId()) is { } node)
                            plays.Add(node);
                        break;
                    case { } other when MayStartASound(other):
                        unsupported ??= Describe(other, actionEvent);
                        break;
                }
            }

            var sound = Combine(plays, oneOf: false);
            var silent = sound != null ? null
                : unsupported != null ? NotSupported + unsupported
                : walk.Problem
                    ?? (missing != null ? $"action {missing} of event {eventName} is in no loaded bank"
                    : playActions != 0 ? $"event {eventName} plays nothing found in the loaded banks"
                    : $"event {eventName} has no Play action");
            return new SoundTriggerResult(tag, sound, silent, walk.Switches);
        }

        static SoundTriggerResult Silent(string tag, string reason) => new(tag, null, reason, []);

        static bool MayStartASound(HircItem item) => item is not ICAkAction action || action.GetActionType() is var type && (!Enum.IsDefined(type)
            || type is AkActionType.PlayAndContinue or AkActionType.PlayEvent or AkActionType.Trigger or AkActionType.Trigger_O or AkActionType.Trigger_E or AkActionType.Trigger_E_O);

        string Describe(HircItem item, ICAkEvent owner, bool follow = true)
        {
            if (item is not ICAkAction action)
                return item.HircType.ToString();

            var type = action.GetActionType();
            var text = (Enum.IsDefined(type) ? type.ToString() : $"0x{(ushort)type:X4}") + " action";
            if (hircs(action.GetChildId()).FirstOrDefault(x => !ReferenceEquals(x, owner)) is not { } target)
                return text;
            text += " -> " + target.HircType;
            if (follow && target is ICAkEvent inner && inner.GetActionIds().Select(Hirc).OfType<HircItem>().FirstOrDefault() is { } first)
                text += " -> " + Describe(first, inner, follow: false);
            return text;
        }

        static SoundNode? Combine(List<SoundNode> children, bool oneOf) => children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => oneOf ? new SoundOneOf(children) : new SoundAll(children),
        };

        sealed class Walk(Func<uint, HircItem?> hirc, Func<uint, bool> wemExists, IReadOnlyDictionary<uint, uint> switchChoices)
        {
            readonly HashSet<uint> _path = [];

            public List<SwitchGroupSeen> Switches { get; } = [];
            public string? Problem { get; private set; }

            public SoundNode? Visit(uint id)
            {
                if (hirc(id) is not { } item || !_path.Add(id))
                    return null;

                try
                {
                    switch (item)
                    {
                        case ICAkSound sound:
                            var sourceId = sound.GetSourceId();
                            if (wemExists(sourceId))
                                return new SoundLeaf(sourceId);
                            Problem ??= $"WEM not found: {sourceId}.wem";
                            return null;
                        case ICAkSwitchCntr container:
                            return VisitSwitch(container);
                        case ICAkRanSeqCntr container:
                            return VisitRanSeq(container);
                        case ICAkLayerCntr container:
                            return Combine(VisitAll(container.GetChildren()), oneOf: false);
                        case ICAkActorMixer container:
                            return Combine(VisitAll(container.GetChildren()), oneOf: false);
                        default:
                            Problem ??= NotSupported + item.HircType;
                            return null;
                    }
                }
                finally
                {
                    _path.Remove(id);
                }
            }

            List<SoundNode> VisitAll(IEnumerable<uint> ids) => ids.Select(Visit).OfType<SoundNode>().ToList();

            // Weights always apply: bIsUsingWeight is clear on every CA container, even unequally weighted ones.
            SoundNode? VisitRanSeq(ICAkRanSeqCntr container)
            {
                var kept = container.GetChildren().Select((id, index) => (Node: Visit(id), Index: index)).Where(x => x.Node != null).ToList();
                if (kept.Count < 2 || container is not CAkRanSeqCntr_V136 rules)
                    return Combine(kept.Select(x => x.Node!).ToList(), oneOf: true);
                var sequence = rules.Mode == 1;
                return new SoundOneOf(kept.Select(x => x.Node!).ToList(), rules.Id, kept.Select(x => rules.CAkPlayList.Playlist[x.Index].Weight).ToList(),
                    rules.AvoidRepeatCount, Shuffle: rules.RandomMode == 1, Sequence: sequence);
            }

            SoundNode? VisitSwitch(ICAkSwitchCntr container)
            {
                var cases = container.SwitchList;
                if (Switches.All(x => x.GroupId != container.GroupId))
                    Switches.Add(new SwitchGroupSeen(container.GroupId, container.DefaultSwitch, cases.Select(x => x.SwitchId).ToList()));

                var chosen = switchChoices.TryGetValue(container.GroupId, out var choice) ? cases.FirstOrDefault(x => x.SwitchId == choice) : null;
                chosen ??= cases.FirstOrDefault(x => x.SwitchId == container.DefaultSwitch) ?? cases.FirstOrDefault();
                return chosen == null ? null : Combine(VisitAll(chosen.NodeIdList), oneOf: false);
            }
        }
    }
}
