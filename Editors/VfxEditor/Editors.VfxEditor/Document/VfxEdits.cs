using System.Globalization;

namespace Editors.VfxEditor.Document
{
    /// <summary>Adding and removing as splices; a linked edit reaches every tier, matching modifiers by
    /// fragment and occurrence. Refusals throw ArgumentException (a bad id) or InvalidOperationException (not at this tier).</summary>
    public static class VfxEdits
    {
        static readonly string[] s_emitterSections = ["emitters", "behaviour_chains", "effect_render_stacks"];

        /// <summary>The emitter types vanilla uses: 11,726 continuous_mesh and 2,925 continuous_rigid, nothing else.</summary>
        public static readonly string[] EmitterTypes = ["continuous_mesh", "continuous_rigid"];

        static SpanElement Required(VfxDocument document, string section) =>
            document.Section(section) ?? throw new InvalidOperationException($"the file has no <{section}>");

        // editor_data: the list of the file's parts that CA's effect editor (Terry) writes, a <component id> per emitter and per reference's inst_name. The game
        // does not read it; edits keep it in step so the file stays one Terry would write.
        static SpanElement? Components(VfxDocument document) => document.Root.Child("editor_data")?.Child("effect_components");

        static SpanElement? Component(VfxDocument document, string id) =>
            Components(document)?.ChildrenNamed("component").FirstOrDefault(c => c.Attr("id") == id);

        static string ComponentXml(string id) => $"<component id=\"{XmlText.EscapeAttribute(id, '"')}\"/>";

        // Properties of an emitter or render stack

        /// <summary>After the container's last child: at tier 0 a base the other tiers inherit, otherwise that tier's override.</summary>
        public static Splice AddProperty(VfxDocument document, SpanElement container, string propertyXml, int tier = 0) =>
            document.InsertInto(container, tier == 0 ? propertyXml
                : VfxDocument.Rewrite(propertyXml, (d, e) => [d.SetAttribute(e, "quality", tier.ToString(CultureInfo.InvariantCulture))]));

        /// <summary>Linked: every tier of it. This tier only: the tier's own element; at tier 0 the tiers that inherited the
        /// base first get an override holding it. A tier that inherits cannot drop the base's value alone.</summary>
        public static List<Splice> RemoveProperty(VfxDocument document, SpanElement container, string name, int tier, bool thisTierOnly)
        {
            var tiers = TierEdits.Tiers(container, name);
            if (!thisTierOnly)
                return tiers.Values.Select(document.Remove).ToList();
            if (!tiers.TryGetValue(tier, out var own))
                throw new InvalidOperationException($"{name} at this tier is Ultra's value, so it cannot be removed at this tier alone");
            var splices = new List<Splice>();
            if (tier == 0)
                splices.AddRange(new[] { 1, 2 }.Where(t => !tiers.ContainsKey(t)).Select(t => TierEdits.InsertTierCopy(document, tiers, own, t, null, null)));
            splices.Add(document.Remove(own));
            return splices;
        }

        // Properties of a modifier

        public static List<Splice> AddModifierProperty(VfxDocument document, SpanElement chain, int tier, string fragment, int occurrence, string propertyXml, bool thisTierOnly)
        {
            var name = VfxDocument.FromText(propertyXml).Root.Children[0].Attr("name") ?? "";
            return TierEdits.LinkedModifiers(chain, tier, fragment, occurrence, thisTierOnly)
                .Where(m => TierEdits.PropertyIn(m, name) == null)
                .Select(m => document.InsertInto(m, propertyXml)).ToList();
        }

        public static List<Splice> RemoveModifierProperty(VfxDocument document, SpanElement chain, int tier, string fragment, int occurrence, string name, bool thisTierOnly) =>
            TierEdits.LinkedModifiers(chain, tier, fragment, occurrence, thisTierOnly)
                .Select(m => TierEdits.PropertyIn(m, name)).OfType<SpanElement>()
                .Select(document.Remove).ToList();

        // Modifiers

        /// <summary>Into every block the chain has, or the tier's own; each property on its own line.</summary>
        public static List<Splice> AddModifier(VfxDocument document, SpanElement chain, int tier, string fragment, IReadOnlyList<string> properties, bool thisTierOnly)
        {
            var blocks = thisTierOnly
                ? new List<SpanElement> { TierEdits.Block(chain, tier) ?? throw new InvalidOperationException("this tier has no block; copy Ultra's block to it first") }
                : chain.ChildrenNamed("quality").ToList();
            var start = $"<modifier fragment=\"{XmlText.EscapeAttribute(fragment, '"')}\">";
            return blocks.Select(b => document.InsertInto(b, VfxDocument.Wrap(start, properties, "</modifier>", document.ChildBreak(b)))).ToList();
        }

        public static List<Splice> RemoveModifier(VfxDocument document, SpanElement chain, int tier, string fragment, int occurrence, bool thisTierOnly) =>
            TierEdits.LinkedModifiers(chain, tier, fragment, occurrence, thisTierOnly).Select(document.Remove).ToList();

        /// <summary>A tier with no block draws nothing; this gives it a copy of another tier's block.</summary>
        public static Splice CopyBlock(VfxDocument document, SpanElement chain, int fromTier, int toTier)
        {
            if (TierEdits.Block(chain, toTier) != null)
                throw new InvalidOperationException($"tier {toTier} already has a block");
            var source = TierEdits.Block(chain, fromTier) ?? throw new InvalidOperationException($"tier {fromTier} has no block to copy");
            var copy = VfxDocument.Rewrite(document.RawOf(source), (d, e) => [d.SetAttribute(e, "value", toTier.ToString(CultureInfo.InvariantCulture))]);
            return document.InsertAfter(chain.ChildrenNamed("quality").Last(), copy);
        }

        // Emitters

        /// <summary>An emitter, its behaviour chain and its render stack share one id; all three are copied under the new one,
        /// each after its original.</summary>
        public static List<Splice> DuplicateEmitter(VfxDocument document, string id, string newId)
        {
            CheckNewId(document, id, newId);
            var splices = s_emitterSections.Select(s => document.ById(s, id)).OfType<SpanElement>()
                .Select(r => document.InsertAfter(r, VfxDocument.Rewrite(document.RawOf(r), (d, e) => [d.SetAttribute(e, "id", newId)])))
                .ToList();
            if (Component(document, id) is { } component)
                splices.Add(document.InsertAfter(component, ComponentXml(newId)));
            else if (Components(document) is { } components)
                splices.Add(document.InsertInto(components, ComponentXml(newId)));
            return splices;
        }

        /// <summary>The emitter, its chain, its render stack and its editor component, all under the new id. A reference to
        /// this effect in another file still names the old one.</summary>
        public static List<Splice> RenameEmitter(VfxDocument document, string id, string newId)
        {
            CheckNewId(document, id, newId);
            return s_emitterSections.Select(s => document.ById(s, id)).Append(Component(document, id)).OfType<SpanElement>()
                .Select(r => document.SetAttribute(r, "id", newId)).ToList();
        }

        static void CheckNewId(VfxDocument document, string id, string newId)
        {
            if (document.ById("emitters", id) == null)
                throw new ArgumentException($"there is no emitter {id}");
            CheckFreeId(document, newId);
        }

        static void CheckFreeId(VfxDocument document, string newId)
        {
            if (string.IsNullOrWhiteSpace(newId) || newId != newId.Trim())
                throw new ArgumentException("an id cannot be empty or start or end with a space");
            if (s_emitterSections.Any(s => document.ById(s, newId) != null) || Component(document, newId) != null)
                throw new ArgumentException($"{newId} is already an id in this file");
        }

        public static Splice SetEmitterType(VfxDocument document, string id, string type) => EmitterTypes.Contains(type)
            ? document.SetAttribute(document.ById("emitters", id) ?? throw new ArgumentException($"there is no emitter {id}"), "type", type)
            : throw new ArgumentException($"{type} is not an emitter type vanilla uses: {string.Join(" or ", EmitterTypes)}");

        /// <summary>A new emitter with these properties, a behaviour chain holding these modifiers at all three tiers, a render
        /// stack with these properties, and its editor component.</summary>
        public static List<Splice> AddEmitter(VfxDocument document, string id, string type, IReadOnlyList<string> properties,
            IReadOnlyList<(string Fragment, IReadOnlyList<string> Properties)> modifiers, IReadOnlyList<string> stackProperties)
        {
            CheckFreeId(document, id);
            if (!EmitterTypes.Contains(type))
                throw new ArgumentException($"{type} is not an emitter type vanilla uses: {string.Join(" or ", EmitterTypes)}");
            var (emitters, chains, stacks) = (Required(document, "emitters"), Required(document, "behaviour_chains"), Required(document, "effect_render_stacks"));
            var name = XmlText.EscapeAttribute(id, '"');
            var chainBreak = document.ChildBreak(chains);
            var blockBreak = chainBreak.Length > 0 ? chainBreak + "\t" : "";
            var modifierBreak = blockBreak.Length > 0 ? blockBreak + "\t" : "";
            var block = modifiers.Select(m => VfxDocument.Wrap($"<modifier fragment=\"{XmlText.EscapeAttribute(m.Fragment, '"')}\">", m.Properties, "</modifier>", modifierBreak)).ToList();
            var blocks = new[] { 0, 1, 2 }.Select(t => VfxDocument.Wrap($"<quality value=\"{t}\">", block, "</quality>", blockBreak)).ToList();
            var splices = new List<Splice>
            {
                document.InsertInto(emitters, VfxDocument.Wrap($"<emitter type=\"{type}\" id=\"{name}\">", properties, "</emitter>", document.ChildBreak(emitters))),
                document.InsertInto(chains, VfxDocument.Wrap($"<behaviour_chain id=\"{name}\">", blocks, "</behaviour_chain>", chainBreak)),
                document.InsertInto(stacks, VfxDocument.Wrap($"<effect_render_stack id=\"{name}\">", stackProperties, "</effect_render_stack>", document.ChildBreak(stacks))),
            };
            if (Components(document) is { } components)
                splices.Add(document.InsertInto(components, ComponentXml(id)));
            return splices;
        }

        public static List<Splice> RemoveEmitter(VfxDocument document, string id) =>
            s_emitterSections.Select(s => document.ById(s, id)).Append(Component(document, id)).OfType<SpanElement>().Select(document.Remove).ToList();

        // References

        /// <summary>CA's attributes for a reference, in CA's order, with the most common value of each across vfx_desc.pack
        /// (measured 2026-10-11).</summary>
        public static readonly (string Name, string Value)[] ReferenceDefaults =
        [
            ("inst_name", "xref"), ("vfx_ref", ""), ("notes", ""), ("scale_min", "1.00"), ("scale_max", "1.00"),
            ("pos_x", "0.00"), ("pos_y", "0.00"), ("pos_z", "0.00"), ("rot_x", "0.00"), ("rot_y", "0.00"), ("rot_z", "0.00"),
            ("start_time_min", "0.00"), ("start_time_max", "0.00"), ("infinite_life", "true"), ("simulation_speed_multiplier", "1.00"),
        ];

        /// <summary>An override row in CA's attribute order and neutral values: no tint, no bias, the spawn rate as written.</summary>
        public static string OverrideRow(string emitterName, bool enable = true) =>
            $"<emitter name=\"{XmlText.EscapeAttribute(emitterName, '"')}\" enable=\"{(enable ? "true" : "false")}\" " +
            "tint_min_r=\"1.00\" tint_min_g=\"1.00\" tint_min_b=\"1.00\" tint_min_a=\"1.00\" tint_max_r=\"1.00\" tint_max_g=\"1.00\" tint_max_b=\"1.00\" tint_max_a=\"1.00\" " +
            "particle_lifetime_bias_min=\"0.00\" particle_lifetime_bias_max=\"0.00\" emitter_lifetime_bias_min=\"0.00\" emitter_lifetime_bias_max=\"0.00\" " +
            "spawn_rate_scale_factor_min=\"1.00\" spawn_rate_scale_factor_max=\"1.00\" sound_record=\"\"/>";

        /// <summary>A reference to `target` with a row, switched on, for each of its emitters (planning ruling: CA's most
        /// common row is switched off, but on shows the target at once).</summary>
        /// <summary>A file with no &lt;vfx_references&gt; gains one after &lt;vfx&gt;, where every vanilla file has it.</summary>
        public static List<Splice> AddReference(VfxDocument document, string target, IReadOnlyList<string> targetEmitters)
        {
            var attributes = ReferenceDefaults.Select(a => a.Name == "vfx_ref" ? (a.Name, target) : a);
            var start = "<vfx " + string.Join(' ', attributes.Select(a => $"{a.Name}=\"{XmlText.EscapeAttribute(a.Item2, '"')}\"")) + ">";
            var rows = targetEmitters.Select(e => OverrideRow(e)).ToList();
            var splices = new List<Splice>();
            if (document.Section("vfx_references") is { } references)
                splices.Add(document.InsertInto(references, VfxDocument.Wrap(start, rows, "</vfx>", document.ChildBreak(references))));
            else
            {
                var vfx = document.Root.Child("vfx") ?? throw new InvalidOperationException("the file has no <vfx>");
                var nl = document.Text.Contains("\r\n") ? "\r\n" : "\n";
                splices.Add(new Splice(vfx.End, vfx.End, $"{nl}<vfx_references>{nl}\t{VfxDocument.Wrap(start, rows, "</vfx>", nl + "\t")}{nl}</vfx_references>"));
            }
            var instName = ReferenceDefaults[0].Value;
            if (Components(document) is { } components && Component(document, instName) == null)
                splices.Add(document.InsertInto(components, ComponentXml(instName)));
            return splices;
        }

        /// <summary>The reference, and its editor component when no other reference or emitter shares its name.</summary>
        public static List<Splice> RemoveReference(VfxDocument document, SpanElement reference)
        {
            var splices = new List<Splice> { document.Remove(reference) };
            var name = reference.Attr("inst_name") ?? "";
            var shared = document.Section("vfx_references")!.ChildrenNamed("vfx").Count(r => r.Attr("inst_name") == name) > 1 || document.ById("emitters", name) != null;
            if (!shared && Component(document, name) is { } component)
                splices.Add(document.Remove(component));
            return splices;
        }

        public static Splice AddOverride(VfxDocument document, SpanElement reference, string emitterName) => document.InsertInto(reference, OverrideRow(emitterName));

        public static List<Splice> SetAllOverrides(VfxDocument document, SpanElement reference, bool enable) =>
            reference.ChildrenNamed("emitter").Select(r => document.SetAttribute(r, "enable", enable ? "true" : "false")).ToList();
    }
}
