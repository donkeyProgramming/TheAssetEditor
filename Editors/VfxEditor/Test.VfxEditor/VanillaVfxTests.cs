using System.Xml.Linq;
using Editors.VfxEditor.Document;
using Editors.VfxEditor.Vocabulary;

namespace Test.VfxEditor
{
    class VanillaVfxTests
    {
        // Every vfx/*.xml CA ships saves back byte-identical unedited, and reads into the same elements, attributes and
        // text as the .NET XML parser reads.
        [Test, Explicit("reads vfx_desc.pack; set WH3_DATA")]
        public void Every_vanilla_file_round_trips_and_reads_like_the_xml_parser()
        {
            var problems = new List<string>();
            var curves = 0;
            var files = VanillaVfx.Files();
            foreach (var (path, bytes) in files)
            {
                var document = VfxDocument.Load(bytes);
                if (!document.Save().AsSpan().SequenceEqual(bytes))
                    problems.Add($"{path}: does not save byte-identical");
                // PreserveWhitespace, or a whitespace-only text such as <editor_data>\r\n</editor_data> reads as "".
                var xml = XElement.Parse("<root>" + document.Text + "</root>", LoadOptions.PreserveWhitespace);
                if (Difference(document, document.Root, xml) is { } difference)
                    problems.Add($"{path}: {difference}");
                // property only: a material's <PROPERTY name="Shader"> holds a path with a drive colon, not a curve.
                foreach (var element in All(document.Root).Where(e => e.Name == "property" && !e.SelfClosing && e.Children.Count == 0))
                {
                    var text = document.TextOf(element);
                    if (!text.Contains(':'))
                        continue;
                    curves++;
                    var curve = CurveText.Parse(text);
                    if (curve == null)
                        problems.Add($"{path}: {element.Attr("name")} is not read as a curve");
                    else if (curve.ToString() != text)
                        problems.Add($"{path}: {element.Attr("name")} does not rebuild");
                }
            }
            TestContext.Out.WriteLine($"{files.Count} files, {curves} curves, {problems.Count} problems");
            Assert.That(problems.Take(20), Is.Empty);
            Assert.That(files, Has.Count.EqualTo(8308));
        }

        // The vocabulary over CA's files: every effect at vfx/ root named, materials left out, and the fragments vanilla uses.
        [Test, Explicit("reads vfx_desc.pack; set WH3_DATA")]
        public void The_vocabulary_reads_every_vanilla_effect()
        {
            var vocabulary = VfxVocabulary.Build(VanillaVfx.Files().Select(f => (f.Path, VfxDocument.Load(f.Bytes))));
            TestContext.Out.WriteLine($"{vocabulary.EffectNames.Count} effects; fragments: {string.Join(", ", vocabulary.Fragments)}");
            Assert.That(vocabulary.EffectNames, Has.Count.EqualTo(7470));
            Assert.That(vocabulary.Fragments, Has.Count.EqualTo(18));
        }

        internal static IEnumerable<SpanElement> All(SpanElement element) => element.Children.SelectMany(All).Prepend(element);

        static string? Difference(VfxDocument document, SpanElement span, XElement xml)
        {
            var name = span.Parent == null ? "root" : span.Name;
            if (name != xml.Name.LocalName)
                return $"<{span.Name}> read as <{xml.Name.LocalName}>";
            if (!span.Attributes.Select(a => (a.Name, Normalize(a.Value))).SequenceEqual(xml.Attributes().Select(a => (a.Name.LocalName, a.Value))))
                return $"<{span.Name}> attributes differ";
            var children = xml.Elements().ToList();
            if (children.Count != span.Children.Count)
                return $"<{span.Name}> has {span.Children.Count} children, not {children.Count}";
            if (span.Parent != null && children.Count == 0 && !span.SelfClosing && !xml.Nodes().OfType<XComment>().Any()
                && document.TextOf(span).ReplaceLineEndings("\n") != xml.Value)
                return $"<{span.Name}> text differs";
            for (var i = 0; i < children.Count; i++)
            {
                if (Difference(document, span.Children[i], children[i]) is { } d)
                    return d;
            }
            return null;
        }

        // The XML parser turns a line break or tab inside an attribute value into a space.
        static string Normalize(string value) => value.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
    }
}
