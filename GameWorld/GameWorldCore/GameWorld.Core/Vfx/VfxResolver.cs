using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.GameFormats.Vfx;

namespace GameWorld.Core.Vfx
{
    /// <summary>An enabled emitter of an effect, with what every reference on the way to it said. InfiniteLife: no reference on
    /// the way says infinite_life="false", which ends an emitter that is infinite on its own (wh3_main_lib_lightsource's lights,
    /// under an explosion); "true" makes nothing infinite, 659 impact and explosion references saying it over emitters that
    /// all end. MaterialTexture: the colour texture its ps_custom_shader material names, if the material is in the packs and
    /// names one. VectorField: the volume its VectorFieldStrId names, if that is in the packs and reads.</summary>
    public record ResolvedEmitter(VfxEmitter Emitter, VfxBehaviourChain? Chain, string? SceneViewType, string File,
        Matrix ReferenceTransform, float StartOffset, float SpeedMultiplier, bool InfiniteLife, VfxEmitterOverride Override, bool TrailMode = false,
        string? MaterialTexture = null, VfxVectorField? VectorField = null, float? RefSpawnSpeed = null, IReadOnlyList<ResolvedEvent>? Events = null);

    /// <summary>A particle event with its effect resolved as a top-level one is.</summary>
    public record ResolvedEvent(VfxParticleEvent Event, IReadOnlyList<ResolvedEmitter> Emitters);

    public record VfxResolveResult(bool Found, List<ResolvedEmitter> Emitters, List<string> Problems);

    /// <summary>
    /// Loads vfx/&lt;name&gt;.xml through the pack file service (so project packs and folders count the
    /// same as CA's), follows its references with a cycle guard and applies their overrides; an outer
    /// reference's override wins. Range values (scale, start time) take one draw each from the random.
    /// </summary>
    public class VfxResolver
    {
        private readonly IPackFileService _packFileService;
        private readonly IReadOnlyDictionary<string, byte[]> _unsaved;
        private readonly int _tier;
        private readonly Dictionary<string, VfxVectorField?> _fields = [];
        // Particle event effects, resolved once per Resolve: every emitter of an effect and of each effect it reaches would
        // otherwise resolve the same ones again, level on level. Null while one is being resolved, so reaching it then is a cycle.
        private readonly Dictionary<string, IReadOnlyList<ResolvedEmitter>?> _eventEffects = [];

        /// <param name="unsaved">Bytes by Key(name) read in place of the pack's copy: an editor's unsaved file.</param>
        /// <param name="tier">The quality tier read, 0 (Ultra) by default.</param>
        public VfxResolver(IPackFileService packFileService, IReadOnlyDictionary<string, byte[]>? unsaved = null, int tier = 0)
        {
            _packFileService = packFileService;
            _unsaved = unsaved ?? new Dictionary<string, byte[]>();
            _tier = tier;
        }

        public static string PathOf(string vfxName)
        {
            var name = vfxName.Trim();
            return "vfx/" + (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ? name : name + ".xml");
        }

        public VfxResolveResult Resolve(string vfxName, Random random)
        {
            var result = new VfxResolveResult(false, [], []);
            _eventEffects.Clear();
            var top = Load(vfxName, result.Problems);
            if (top == null)
                return result;

            Collect(vfxName.Trim(), top, Matrix.Identity, 0, 1, true, new Dictionary<string, VfxEmitterOverride>(), [Key(vfxName)], random, result, []);
            return result with { Found = true };
        }

        public static string Key(string vfxName) => PathOf(vfxName).ToLowerInvariant();

        VfxFile? Load(string vfxName, List<string> problems)
        {
            var unsaved = _unsaved.GetValueOrDefault(Key(vfxName));
            var file = unsaved == null ? _packFileService.FindFile(PathOf(vfxName)) : null;
            if (unsaved == null && file == null)
                return null;
            try
            {
                return VfxFile.Parse(unsaved ?? file!.DataSource.ReadData(), _tier);
            }
            catch (XmlException e)
            {
                problems.Add($"{PathOf(vfxName)} did not parse: {e.Message}");
                return null;
            }
            catch (Exception e)   // a pack rebuilt or moved on disk while it is loaded; nothing throws into the viewport
            {
                problems.Add($"{PathOf(vfxName)} could not be read: {e.Message}");
                return null;
            }
        }

        // A vfx material's first texture in a slot named for a diffuse or base colour (t_xml_particle_diffuse_2048_only,
        // t_xml_diffuse_base_colour, ...). Null when the material is not in the packs or names none: the fire's carry only alpha masks.
        string? ColourTexture(string? material)
        {
            if (string.IsNullOrEmpty(material) || _packFileService.FindFile(material) is not { } file)
                return null;
            try
            {
                using var stream = new System.IO.MemoryStream(file.DataSource.ReadData());
                return XDocument.Load(stream).Descendants("texture")
                    .Where(t => t.Element("slot")?.Value is { } slot && (slot.Contains("diffuse", StringComparison.OrdinalIgnoreCase) || slot.Contains("base_colour", StringComparison.OrdinalIgnoreCase)))
                    .Select(t => t.Element("source")?.Value.Trim())
                    .FirstOrDefault(source => !string.IsNullOrEmpty(source));
            }
            catch (Exception)   // a broken material draws nothing, as one with no colour texture does
            {
                return null;
            }
        }

        // One read per field per resolve: 1,205 vanilla emitters share the turbulence field.
        VfxVectorField? VectorField(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (_fields.TryGetValue(path.ToLowerInvariant(), out var known))
                return known;
            VfxVectorField? field = null;
            try
            {
                if (_packFileService.FindFile(path) is { } file)
                    field = VfxVectorField.Read(file.DataSource.ReadData());
            }
            catch (Exception)   // a pack moved on disk: the emitter runs without its field, and says so
            {
            }
            return _fields[path.ToLowerInvariant()] = field;
        }

        // A particle event's effect, resolved as a top-level one is; one already on the way here is a cycle, and not followed.
        List<ResolvedEvent> Events(string name, VfxEmitter emitter, HashSet<string> stack, Random random, VfxResolveResult result)
        {
            var events = new List<ResolvedEvent>();
            foreach (var particleEvent in emitter.Events ?? [])
            {
                var key = Key(particleEvent.Effect);
                var known = _eventEffects.TryGetValue(key, out var resolved);
                if (stack.Contains(key) || known && resolved == null)
                {
                    result.Problems.Add($"{name}: particle event cycle through {particleEvent.Effect}, skipped");
                    continue;
                }
                if (!known)
                {
                    _eventEffects[key] = null;
                    var target = Load(particleEvent.Effect, result.Problems);
                    if (target == null)
                        result.Problems.Add($"{name}: particle event effect {PathOf(particleEvent.Effect)} not found, skipped");
                    var child = new VfxResolveResult(true, [], result.Problems);
                    if (target != null)
                        Collect(particleEvent.Effect.Trim(), target, Matrix.Identity, 0, 1, true, new Dictionary<string, VfxEmitterOverride>(), [.. stack, key], random, child, []);
                    _eventEffects[key] = resolved = child.Emitters;
                }
                if (resolved!.Count > 0)
                    events.Add(new ResolvedEvent(particleEvent, resolved));
            }
            return events;
        }

        // named: the reference that reached this file lists emitters, and so plays only those, as the game does. CA's editor
        // saves every emitter a library has into the reference, so one the library gained later is unnamed and silent.
        void Collect(string name, VfxFile file, Matrix transform, float startOffset, float speed, bool infinite,
            IReadOnlyDictionary<string, VfxEmitterOverride> outer, HashSet<string> stack, Random random, VfxResolveResult result, HashSet<string> seen, bool named = false)
        {
            foreach (var emitter in file.Emitters)
            {
                seen.Add(emitter.Id);
                if (!outer.TryGetValue(emitter.Id, out var o) && named)
                    continue;
                o ??= VfxEmitterOverride.None;
                var switchedOn = !emitter.Properties.TryGetValue("Enable", out var enable) || enable.IsTrue;
                if (!o.Enable || !switchedOn)
                    continue;
                file.Chains.TryGetValue(emitter.Id, out var chain);
                file.SceneViewTypes.TryGetValue(emitter.Id, out var sceneView);
                var material = chain?.Find("ps_custom_shader")?.Properties.GetValueOrDefault("material")?.Attributes.GetValueOrDefault("path");
                result.Emitters.Add(new ResolvedEmitter(emitter, chain, sceneView, name, transform, startOffset, speed, infinite, o, file.TrailModes.Contains(emitter.Id),
                    ColourTexture(material), VectorField(emitter.Properties.GetValueOrDefault("VectorFieldStrId")?.Value), file.RefSpawnSpeed,
                    Events(name, emitter, stack, random, result)));
            }

            foreach (var reference in file.References)
            {
                var key = Key(reference.Target);
                if (stack.Contains(key))
                {
                    result.Problems.Add($"{name}: reference cycle through {reference.Target}, skipped");
                    continue;
                }
                var target = Load(reference.Target, result.Problems);
                if (target == null)
                {
                    result.Problems.Add($"{name}: referenced effect {PathOf(reference.Target)} not found, skipped");
                    continue;
                }

                var overrides = new Dictionary<string, VfxEmitterOverride>(reference.Overrides);
                foreach (var (emitterName, o) in outer)
                    overrides[emitterName] = o;

                var scale = MathHelper.Lerp(reference.ScaleMin, reference.ScaleMax, (float)random.NextDouble());
                var start = MathHelper.Lerp(reference.StartTimeMin, reference.StartTimeMax, (float)random.NextDouble());
                var local = VfxConventions.ReferenceMatrix(scale, reference.RotationDegrees, reference.Position);

                // An override reaches any emitter under its reference; one naming an id nothing there has does nothing.
                var below = new HashSet<string>();
                stack.Add(key);
                Collect(reference.Target, target, local * transform, startOffset + start, speed * reference.SimulationSpeed,
                    infinite && reference.InfiniteLife, overrides, stack, random, result, below, reference.Overrides.Count > 0);
                stack.Remove(key);
                seen.UnionWith(below);
                foreach (var missing in reference.Overrides.Keys.Where(x => !below.Contains(x)))
                    result.Problems.Add($"{name}: {reference.Target} has no emitter {missing}, so its override does nothing");
            }
        }
    }
}
