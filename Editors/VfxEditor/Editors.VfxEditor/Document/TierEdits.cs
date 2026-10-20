using System.Globalization;

namespace Editors.VfxEditor.Document
{
    /// <summary>Quality tiers: a property's quality="0" is the base and 1 and 2 override it, while a behaviour
    /// chain has one quality block per tier with no fallback. A field is an attribute, or the element's text when null.</summary>
    public static class TierEdits
    {
        public static int TierOf(SpanElement property) => int.TryParse(property.Attr("quality"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var tier) ? tier : 0;

        /// <summary>The elements named `name` in a container, by tier; the first written at a tier wins.</summary>
        public static Dictionary<int, SpanElement> Tiers(SpanElement container, string name)
        {
            var tiers = new Dictionary<int, SpanElement>();
            foreach (var child in container.Children.Where(c => c.Attr("name") == name))
                tiers.TryAdd(TierOf(child), child);
            return tiers;
        }

        public static SpanElement? Effective(SpanElement container, string name, int tier)
        {
            var tiers = Tiers(container, name);
            return tiers.GetValueOrDefault(tier) ?? tiers.GetValueOrDefault(0);
        }

        /// <summary>The element in effect at a tier for each property name, in the order the names first appear.</summary>
        public static List<SpanElement> EffectiveProperties(SpanElement container, int tier) =>
            container.Children.Select(c => c.Attr("name")).OfType<string>().Distinct()
                .Select(name => Effective(container, name, tier)).OfType<SpanElement>().ToList();

        public static string? Field(VfxDocument document, SpanElement element, string? attribute) =>
            attribute == null ? document.TextOf(element) : element.Attr(attribute);

        static Splice SetField(VfxDocument document, SpanElement element, string? attribute, string value) =>
            attribute == null ? document.SetText(element, value) : document.SetAttribute(element, attribute, value);

        /// <summary>Linked: every tier holding the old value takes the new one. This tier only: tier 0 gives each inheriting
        /// tier an override holding the old value, then changes the base; tier 1 or 2 sets its own override.</summary>
        public static List<Splice> SetProperty(VfxDocument document, SpanElement container, string name, string? attribute, int tier, string value, bool thisTierOnly)
        {
            var tiers = Tiers(container, name);
            var effective = tiers.GetValueOrDefault(tier) ?? tiers.GetValueOrDefault(0)
                ?? throw new ArgumentException($"{name} has no value at this tier");
            var old = Field(document, effective, attribute);
            if (!thisTierOnly)
                return tiers.Values.Where(e => Field(document, e, attribute) == old).Select(e => SetField(document, e, attribute, value)).ToList();

            if (!tiers.TryGetValue(tier, out var own))
                return [InsertTierCopy(document, tiers, effective, tier, attribute, value)];
            var splices = new List<Splice> { SetField(document, own, attribute, value) };
            if (tier == 0)
                splices.AddRange(new[] { 1, 2 }.Where(t => !tiers.ContainsKey(t)).Select(t => InsertTierCopy(document, tiers, own, t, null, null)));
            return splices;
        }

        /// <summary>A copy of `source` at `tier`, with `attribute` (or the text) set to `value` when value is not null,
        /// inserted after the last element of its name.</summary>
        public static Splice InsertTierCopy(VfxDocument document, Dictionary<int, SpanElement> tiers, SpanElement source, int tier, string? attribute, string? value)
        {
            var copy = VfxDocument.Rewrite(document.RawOf(source), (d, e) =>
            {
                var edits = new List<Splice> { d.SetAttribute(e, "quality", tier.ToString(CultureInfo.InvariantCulture)) };
                if (value != null)
                    edits.Add(attribute == null ? d.SetText(e, value) : d.SetAttribute(e, attribute, value));
                return edits;
            });
            return document.InsertAfter(tiers.Values.OrderBy(e => e.Start).Last(), copy);
        }

        // Behaviour chains

        public static SpanElement? Block(SpanElement chain, int tier) =>
            chain.ChildrenNamed("quality").FirstOrDefault(q => q.Attr("value") == tier.ToString(CultureInfo.InvariantCulture));

        /// <summary>A block's modifiers, each with its occurrence among the block's modifiers of the same fragment.</summary>
        public static IEnumerable<(SpanElement Modifier, string Fragment, int Occurrence)> Modifiers(SpanElement block)
        {
            var seen = new Dictionary<string, int>();
            foreach (var modifier in block.ChildrenNamed("modifier"))
            {
                var fragment = modifier.Attr("fragment") ?? "";
                var occurrence = seen.GetValueOrDefault(fragment);
                seen[fragment] = occurrence + 1;
                yield return (modifier, fragment, occurrence);
            }
        }

        /// <summary>The same modifier in any block: the one with the same fragment at the same occurrence.</summary>
        public static SpanElement? Modifier(SpanElement? block, string fragment, int occurrence) =>
            block?.ChildrenNamed("modifier").Where(m => m.Attr("fragment") == fragment).ElementAtOrDefault(occurrence);

        public static SpanElement? PropertyIn(SpanElement? modifier, string name) => modifier?.Children.FirstOrDefault(c => c.Attr("name") == name);

        /// <summary>The tiers whose block has a different number of this fragment than the tier's own, so its occurrences
        /// cannot be matched to them; a linked edit leaves those blocks alone.</summary>
        public static List<int> UnmatchedTiers(SpanElement chain, int tier, string fragment)
        {
            if (Block(chain, tier) is not { } own)
                return [];
            var count = CountOf(own, fragment);
            return chain.ChildrenNamed("quality").Where(b => CountOf(b, fragment) != count)
                .Select(b => int.TryParse(b.Attr("value"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var t) ? t : -1).ToList();
        }

        static int CountOf(SpanElement block, string fragment) => block.ChildrenNamed("modifier").Count(m => m.Attr("fragment") == fragment);

        public static IEnumerable<SpanElement> LinkedModifiers(SpanElement chain, int tier, string fragment, int occurrence, bool thisTierOnly)
        {
            var own = Block(chain, tier);
            var count = own == null ? -1 : CountOf(own, fragment);
            return chain.ChildrenNamed("quality").Where(b => thisTierOnly ? b == own : CountOf(b, fragment) == count)
                .Select(b => Modifier(b, fragment, occurrence)).OfType<SpanElement>();
        }

        public static List<Splice> SetModifierProperty(VfxDocument document, SpanElement chain, int tier, string fragment, int occurrence,
            string name, string? attribute, string value, bool thisTierOnly)
        {
            var own = PropertyIn(Modifier(Block(chain, tier), fragment, occurrence), name)
                ?? throw new ArgumentException($"{fragment} has no {name} at this tier");
            var old = Field(document, own, attribute);
            return LinkedModifiers(chain, tier, fragment, occurrence, thisTierOnly)
                .Select(m => PropertyIn(m, name)).OfType<SpanElement>()
                .Where(p => Field(document, p, attribute) == old)
                .Select(p => SetField(document, p, attribute, value)).ToList();
        }
    }
}
