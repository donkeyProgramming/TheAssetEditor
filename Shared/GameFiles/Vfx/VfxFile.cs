using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace Shared.GameFormats.Vfx
{
    /// <summary>One property as written: its attributes (value, x/y/z/w, min/max, type, path) and, for a curve, its text.</summary>
    public record VfxProperty(string Name, IReadOnlyDictionary<string, string> Attributes, string? Text)
    {
        public string? Value => Attributes.GetValueOrDefault("value");
        public bool IsTrue => string.Equals(Value, "true", StringComparison.OrdinalIgnoreCase);

        public float Number(string attribute, float fallback = 0) =>
            Attributes.TryGetValue(attribute, out var text) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;

        /// <summary>A "float2(8.0,8.0)" value.</summary>
        public Vector2 Float2(Vector2 fallback)
        {
            var parts = Value?.Replace("float2(", "").TrimEnd(')').Split(',');
            return parts is { Length: 2 }
                && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                ? new Vector2(x, y) : fallback;
        }
    }

    /// <summary>One model a continuous_rigid emitter's particles may be: RigidData's rigid_entry.</summary>
    public record VfxRigidEntry(string ModelFile, Vector3 Pivot, string? AnimFile, float AnimSpeed = 1);

    /// <summary>How a particle event's effect turns with its particle: track_orientation_mode disabled or unset, orientation (the
    /// particle's own turn), or orientation_by_velocity.</summary>
    public enum VfxEventTurn { None, Particle, Velocity }

    /// <summary>property_event OnParticleSpawn or OnParticleDeath (689 and 61 vanilla events that name an effect): the effect a
    /// particle starts at its birth or its death, what of the particle it follows, and the event's time offset in seconds.</summary>
    public record VfxParticleEvent(bool OnDeath, string Effect, bool TrackPosition, bool TrackScale, bool TrackColour, bool TrackAlpha,
        bool TrackLifetime, VfxEventTurn Turn, float Offset);

    /// <summary>Rigid: RigidData's models, a continuous_rigid emitter's; null or empty for any other. Events: its particle events.</summary>
    public record VfxEmitter(string Id, string Type, IReadOnlyDictionary<string, VfxProperty> Properties, IReadOnlyList<VfxRigidEntry>? Rigid = null,
        IReadOnlyList<VfxParticleEvent>? Events = null);

    public record VfxModifier(string Fragment, IReadOnlyDictionary<string, VfxProperty> Properties, IReadOnlyDictionary<string, string> Textures);

    public record VfxBehaviourChain(string Id, IReadOnlyList<VfxModifier> Modifiers)
    {
        public VfxModifier? Find(string fragment) => Modifiers.FirstOrDefault(x => x.Fragment == fragment);
    }

    /// <summary>What a reference says about one emitter of the file it references, by name.</summary>
    public record VfxEmitterOverride(string Name, bool Enable, Vector4 TintMin, Vector4 TintMax,
        float ParticleLifetimeBiasMin, float ParticleLifetimeBiasMax, float EmitterLifetimeBiasMin, float EmitterLifetimeBiasMax,
        float SpawnRateScaleMin, float SpawnRateScaleMax)
    {
        public static readonly VfxEmitterOverride None = new("", true, Vector4.One, Vector4.One, 0, 0, 0, 0, 1, 1);
    }

    public record VfxReference(string Target, float ScaleMin, float ScaleMax, Vector3 Position, Vector3 RotationDegrees,
        float StartTimeMin, float StartTimeMax, bool InfiniteLife, float SimulationSpeed, IReadOnlyDictionary<string, VfxEmitterOverride> Overrides);

    /// <summary>
    /// One vfx/*.xml, read at one quality tier (0 is Ultra, the default). The file has several root elements
    /// (shader_library, vfx, vfx_references, editor_data), so it is parsed inside a wrapper element. Emitters, behaviour
    /// chains and render stacks share one id per emitter.
    /// </summary>
    public class VfxFile
    {
        public List<VfxEmitter> Emitters { get; } = [];
        public Dictionary<string, VfxBehaviourChain> Chains { get; } = [];
        /// <summary>Render stack id to its quality-0 scene_view_type, for the stacks that set one.</summary>
        public Dictionary<string, string> SceneViewTypes { get; } = [];
        /// <summary>Render stack ids with enable_trail_mode on: their emitter's particles are drawn as one strip.</summary>
        public HashSet<string> TrailModes { get; } = [];
        public List<VfxReference> References { get; } = [];
        /// <summary>The vfx element's ref_spawn_speed: 30 on 10 flamethrower and acid effects and 15 on one; null when -1, as on
        /// the other 7,459, or absent.</summary>
        public float? RefSpawnSpeed { get; private set; }

        public static VfxFile Parse(byte[] bytes, int tier = 0)
        {
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();
            if (text.StartsWith("<?xml"))
                text = text[(text.IndexOf("?>") + 2)..];
            var root = XElement.Parse("<root>" + text + "</root>");

            var file = new VfxFile();
            var vfx = root.Element("vfx");
            if (vfx != null && Num(vfx, "ref_spawn_speed", -1) is var refSpawnSpeed && refSpawnSpeed != -1)
                file.RefSpawnSpeed = refSpawnSpeed;

            foreach (var emitter in vfx?.Element("emitters")?.Elements("emitter") ?? [])
            {
                var properties = Properties(emitter.Elements().Where(x => x.Name.LocalName is "property" or "ranged_property"), tier);
                file.Emitters.Add(new VfxEmitter((string?)emitter.Attribute("id") ?? "", (string?)emitter.Attribute("type") ?? "", properties, Rigid(emitter, tier), Events(emitter, tier)));
            }

            foreach (var chain in vfx?.Element("behaviour_chains")?.Elements("behaviour_chain") ?? [])
            {
                var blocks = chain.Elements("quality").ToList();
                // A block has no fallback: with no block at the tier the
                // chain draws nothing there, Ultra included.
                var block = blocks.FirstOrDefault(x => (string?)x.Attribute("value") == tier.ToString(CultureInfo.InvariantCulture));
                if (block == null)
                    continue;
                var modifiers = block.Elements("modifier").Select(m => new VfxModifier(
                    (string?)m.Attribute("fragment") ?? "",
                    Properties(m.Elements("property")),
                    m.Elements("texture_property").GroupBy(t => (string?)t.Attribute("name") ?? "").ToDictionary(g => g.Key, g => (string?)g.First().Attribute("path") ?? ""))).ToList();
                var id = (string?)chain.Attribute("id") ?? "";
                file.Chains[id] = new VfxBehaviourChain(id, modifiers);
            }

            foreach (var stack in vfx?.Element("effect_render_stacks")?.Elements("effect_render_stack") ?? [])
            {
                var id = (string?)stack.Attribute("id") ?? "";
                var properties = Properties(stack.Elements("property"), tier);
                if (properties.TryGetValue("scene_view_type", out var view) && view.Value != null)
                    file.SceneViewTypes[id] = view.Value;
                if (properties.TryGetValue("enable_trail_mode", out var trail) && trail.IsTrue)
                    file.TrailModes.Add(id);
            }

            foreach (var reference in root.Element("vfx_references")?.Elements("vfx") ?? [])
            {
                var overrides = new Dictionary<string, VfxEmitterOverride>();
                foreach (var o in reference.Elements("emitter"))
                {
                    var name = (string?)o.Attribute("name") ?? "";
                    overrides[name] = new VfxEmitterOverride(name, (string?)o.Attribute("enable") != "false",
                        new Vector4(Num(o, "tint_min_r", 1), Num(o, "tint_min_g", 1), Num(o, "tint_min_b", 1), Num(o, "tint_min_a", 1)),
                        new Vector4(Num(o, "tint_max_r", 1), Num(o, "tint_max_g", 1), Num(o, "tint_max_b", 1), Num(o, "tint_max_a", 1)),
                        Num(o, "particle_lifetime_bias_min", 0), Num(o, "particle_lifetime_bias_max", 0),
                        Num(o, "emitter_lifetime_bias_min", 0), Num(o, "emitter_lifetime_bias_max", 0),
                        Num(o, "spawn_rate_scale_factor_min", 1), Num(o, "spawn_rate_scale_factor_max", 1));
                }

                file.References.Add(new VfxReference((string?)reference.Attribute("vfx_ref") ?? "",
                    Num(reference, "scale_min", 1), Num(reference, "scale_max", 1),
                    new Vector3(Num(reference, "pos_x", 0), Num(reference, "pos_y", 0), Num(reference, "pos_z", 0)),
                    new Vector3(Num(reference, "rot_x", 0), Num(reference, "rot_y", 0), Num(reference, "rot_z", 0)),
                    Num(reference, "start_time_min", 0), Num(reference, "start_time_max", 0),
                    (string?)reference.Attribute("infinite_life") == "true", Num(reference, "simulation_speed_multiplier", 1), overrides));
            }

            return file;
        }

        // Tier 0 (or no quality attribute) is the base; a property written at the asked tier replaces it. The first one
        // written at each wins.
        static Dictionary<string, VfxProperty> Properties(IEnumerable<XElement> elements, int tier = 0)
        {
            var output = new Dictionary<string, VfxProperty>();
            var overrides = new Dictionary<string, VfxProperty>();
            var asked = tier.ToString(CultureInfo.InvariantCulture);
            foreach (var element in elements)
            {
                var name = (string?)element.Attribute("name");
                var quality = (string?)element.Attribute("quality") ?? "0";
                var target = quality == "0" ? output : quality == asked ? overrides : null;
                if (name == null || target == null)
                    continue;
                var attributes = element.Attributes().Where(a => a.Name.LocalName is not ("name" or "quality")).ToDictionary(a => a.Name.LocalName, a => a.Value);
                target.TryAdd(name, new VfxProperty(name, attributes, element.Value.Length == 0 ? null : element.Value));
            }
            foreach (var (name, property) in overrides)
                output[name] = property;
            return output;
        }

        // property_event at the asked tier, else its base; one naming no effect ("" or "0") starts nothing.
        static List<VfxParticleEvent> Events(XElement emitter, int tier) => Properties(emitter.Elements("property_event"), tier).Values
            .Where(x => x.Name is "OnParticleSpawn" or "OnParticleDeath" && x.Attributes.GetValueOrDefault("vfx_name") is { Length: > 0 } name && name != "0")
            .Select(x =>
            {
                bool Flag(string attribute) => x.Attributes.GetValueOrDefault(attribute) == "true";
                var death = x.Name == "OnParticleDeath";
                var turn = x.Attributes.GetValueOrDefault("track_orientation_mode") switch
                {
                    "orientation_by_velocity" => VfxEventTurn.Velocity,
                    "orientation" => VfxEventTurn.Particle,
                    _ => VfxEventTurn.None,
                };
                return new VfxParticleEvent(death, x.Attributes["vfx_name"], Flag("track_position"), Flag("track_scale"), Flag("track_colour"), Flag("track_alpha"),
                    Flag("track_lifetime"), turn, x.Number(death ? "death_event_time_offset" : "spawn_event_time_offset"));
            })
            .ToList();

        // RigidData written at the asked tier, else its base.
        static List<VfxRigidEntry> Rigid(XElement emitter, int tier)
        {
            var blocks = emitter.Elements("property_rigid_data").ToList();
            var asked = tier.ToString(CultureInfo.InvariantCulture);
            var block = blocks.FirstOrDefault(x => (string?)x.Attribute("quality") == asked) ?? blocks.FirstOrDefault(x => ((string?)x.Attribute("quality") ?? "0") == "0");
            return block?.Elements("rigid_entry")
                .Select(entry => new VfxRigidEntry(
                    (string?)entry.Element("ModelFile")?.Attribute("value") ?? "",
                    entry.Element("PivotPoint") is { } pivot ? new Vector3(Num(pivot, "x", 0.5f), Num(pivot, "y", 0.5f), Num(pivot, "z", 0.5f)) : new Vector3(0.5f),
                    (string?)entry.Element("AnimFile")?.Attribute("value") is { Length: > 0 } anim ? anim : null,
                    entry.Element("AnimSpeed") is { } speed ? Num(speed, "value", 1) : 1))
                .Where(x => x.ModelFile.Length > 0)
                .ToList() ?? [];
        }

        static float Num(XElement element, string attribute, float fallback) =>
            float.TryParse((string?)element.Attribute(attribute), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }
}
