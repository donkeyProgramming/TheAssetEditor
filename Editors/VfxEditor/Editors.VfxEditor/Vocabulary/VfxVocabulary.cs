using System.Globalization;
using Editors.VfxEditor.Document;

namespace Editors.VfxEditor.Vocabulary
{
    /// <summary>What vanilla writes, from tier-0 elements and block-0 modifiers: property names, common forms
    /// and fragment shapes per context ("emitter:&lt;type&gt;", "stack", "fragment:&lt;name&gt;"), dropdown words, effect names.</summary>
    public sealed class VfxVocabulary
    {
        // The most common form among the first 64 distinct ones seen per property; exact counts if a template ever looks wrong
        const int MaxForms = 64;
        const int MaxChoices = 24;

        readonly Dictionary<string, Dictionary<string, Dictionary<string, int>>> _forms = [];
        readonly Dictionary<(string Property, string Attribute), Dictionary<string, int>?> _values = [];
        readonly Dictionary<string, Dictionary<string, (int Count, List<string> Children)>> _shapes = [];
        readonly SortedSet<string> _effects = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, (int Elements, Dictionary<string, int> Names)> _presence = [];

        public IReadOnlyList<string> EffectNames { get; private set; } = [];
        public IReadOnlyList<string> Fragments { get; private set; } = [];

        public IReadOnlyList<string> PropertyNames(string context) =>
            _forms.TryGetValue(context, out var names) ? names.Keys.Order(StringComparer.Ordinal).ToList() : [];

        /// <summary>The names at least half of vanilla's elements in a context carry, most common first: an emitter's tier-0
        /// properties ("emitter:&lt;type&gt;"), its chain's block-0 fragments ("chain:&lt;type&gt;"), its render stack's
        /// properties ("stack:&lt;type&gt;"). What a new emitter starts with.</summary>
        public IReadOnlyList<string> Usual(string context) => _presence.TryGetValue(context, out var presence)
            ? presence.Names.Where(x => x.Value * 2 >= presence.Elements).OrderByDescending(x => x.Value).Select(x => x.Key).ToList()
            : [];

        public string? PropertyTemplate(string context, string name) =>
            _forms.GetValueOrDefault(context)?.GetValueOrDefault(name)?.MaxBy(x => x.Value).Key;

        public IReadOnlyList<string> ModifierTemplate(string fragment) =>
            _shapes.GetValueOrDefault(fragment)?.Values.MaxBy(x => x.Count).Children ?? [];

        /// <summary>The words vanilla writes for this attribute, most common first; null for numbers, flags, single values and
        /// sets too large to pick from (texture paths).</summary>
        public IReadOnlyList<string>? Choices(string property, string attribute) =>
            _values.GetValueOrDefault((property, attribute)) is { Count: > 1 } values
                ? values.OrderByDescending(x => x.Value).Select(x => x.Key).ToList()
                : null;

        /// <summary>"vfx\a.xml" is effect "a"; files in vfx's subfolders (materials) are not effects.</summary>
        public static string? EffectName(string path)
        {
            var p = path.Replace('/', '\\');
            if (!p.StartsWith("vfx\\", StringComparison.OrdinalIgnoreCase) || !p.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                return null;
            var name = p[4..^4];
            return name.Contains('\\') ? null : name;
        }

        public static VfxVocabulary Build(IEnumerable<(string Path, VfxDocument Document)> files)
        {
            var vocabulary = new VfxVocabulary();
            foreach (var (path, document) in files)
                vocabulary.Add(path, document);
            vocabulary.EffectNames = [.. vocabulary._effects];
            vocabulary.Fragments = vocabulary._shapes.Keys.Order(StringComparer.Ordinal).ToList();
            return vocabulary;
        }

        void Add(string path, VfxDocument document)
        {
            if (EffectName(path) is { } name)
                _effects.Add(name);
            foreach (var emitter in document.Section("emitters")?.ChildrenNamed("emitter") ?? [])
            {
                var type = emitter.Attr("type");
                var properties = emitter.Children.Where(c => TierEdits.TierOf(c) == 0).ToList();
                AddProperties(document, "emitter:" + type, properties);
                Count("emitter:" + type, properties.Select(p => p.Attr("name")));
                var id = emitter.Attr("id") ?? "";
                if (document.ById("behaviour_chains", id) is { } chain && TierEdits.Block(chain, 0) is { } block)
                    Count("chain:" + type, block.ChildrenNamed("modifier").Select(m => m.Attr("fragment")));
                if (document.ById("effect_render_stacks", id) is { } stack)
                    Count("stack:" + type, stack.Children.Where(c => TierEdits.TierOf(c) == 0).Select(p => p.Attr("name")));
            }
            foreach (var stack in document.Section("effect_render_stacks")?.Children ?? [])
                AddProperties(document, "stack", stack.Children.Where(c => TierEdits.TierOf(c) == 0));
            foreach (var chain in document.Section("behaviour_chains")?.Children ?? [])
            {
                if (TierEdits.Block(chain, 0) is not { } block)
                    continue;
                foreach (var modifier in block.ChildrenNamed("modifier"))
                {
                    var fragment = modifier.Attr("fragment") ?? "";
                    var properties = modifier.Children.Where(c => c.Attr("name") != null).ToList();
                    AddProperties(document, "fragment:" + fragment, properties);
                    if (!_shapes.TryGetValue(fragment, out var shapes))
                        _shapes[fragment] = shapes = [];
                    var key = string.Join('|', properties.Select(p => p.Attr("name")));
                    shapes[key] = shapes.TryGetValue(key, out var shape) ? (shape.Count + 1, shape.Children) : (1, properties.Select(document.RawOf).ToList());
                }
            }
        }

        void Count(string context, IEnumerable<string?> names)
        {
            var (elements, counts) = _presence.TryGetValue(context, out var presence) ? presence : (0, new Dictionary<string, int>());
            foreach (var name in names.OfType<string>().Distinct())
                counts[name] = counts.GetValueOrDefault(name) + 1;
            _presence[context] = (elements + 1, counts);
        }

        void AddProperties(VfxDocument document, string context, IEnumerable<SpanElement> properties)
        {
            if (!_forms.TryGetValue(context, out var names))
                _forms[context] = names = [];
            foreach (var property in properties)
            {
                if (property.Attr("name") is not { } name)
                    continue;
                if (!names.TryGetValue(name, out var forms))
                    names[name] = forms = [];
                var raw = document.RawOf(property);
                if (forms.TryGetValue(raw, out var count))
                    forms[raw] = count + 1;
                else if (forms.Count < MaxForms)
                    forms[raw] = 1;
                foreach (var attribute in property.Attributes.Where(a => a.Name is not ("name" or "quality")))
                    AddValue(name, attribute.Name, attribute.Value);
            }
        }

        void AddValue(string property, string attribute, string value)
        {
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase)
                || float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return;
            var key = (property, attribute);
            if (_values.TryGetValue(key, out var values) && values == null)
                return;
            values ??= _values[key] = [];
            values[value] = values.GetValueOrDefault(value) + 1;
            if (values.Count > MaxChoices)
                _values[key] = null;
        }
    }
}
