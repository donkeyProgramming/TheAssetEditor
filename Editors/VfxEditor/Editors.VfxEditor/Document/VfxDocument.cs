using System.Text;

namespace Editors.VfxEditor.Document
{
    public enum VfxEncoding { Utf16Bom, Utf8Bom, Utf8 }

    /// <summary>Replaces Text[Start..End) with Text; Start == End inserts.</summary>
    public readonly record struct Splice(int Start, int End, string Text);

    /// <summary>A vfx/*.xml as text read into SpanElements. An edit splices the text and re-reads it, so untouched bytes
    /// never change and an unedited document saves byte-identical, in the encoding it was read in.</summary>
    public sealed class VfxDocument
    {
        static readonly UnicodeEncoding s_utf16 = new(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
        static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        const int UndoDepth = 100;   // each step is a full copy of the text
        readonly LinkedList<string> _undo = new();
        readonly Stack<string> _redo = new();

        VfxDocument(string text, VfxEncoding encoding)
        {
            Root = XmlSpanParser.Parse(text);
            Text = text;
            Encoding = encoding;
        }

        public VfxEncoding Encoding { get; }
        public string Text { get; private set; }
        public SpanElement Root { get; private set; }
        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;
        public bool IsEffect => Root.Child("vfx") != null || Root.Child("vfx_references") != null;
        public event Action? Changed;

        /// <summary>Throws VfxParseException on XML it cannot place, DecoderFallbackException on bytes that are not text.</summary>
        public static VfxDocument Load(byte[] bytes)
        {
            if (bytes is [0xFF, 0xFE, ..])
                return new VfxDocument(s_utf16.GetString(bytes, 2, bytes.Length - 2), VfxEncoding.Utf16Bom);
            if (bytes is [0xEF, 0xBB, 0xBF, ..])
                return new VfxDocument(s_utf8.GetString(bytes, 3, bytes.Length - 3), VfxEncoding.Utf8Bom);
            return new VfxDocument(s_utf8.GetString(bytes), VfxEncoding.Utf8);
        }

        public static VfxDocument FromText(string text) => new(text, VfxEncoding.Utf8);

        public byte[] Save() => Encoding switch
        {
            VfxEncoding.Utf16Bom => [0xFF, 0xFE, .. s_utf16.GetBytes(Text)],
            VfxEncoding.Utf8Bom => [0xEF, 0xBB, 0xBF, .. s_utf8.GetBytes(Text)],
            _ => s_utf8.GetBytes(Text),
        };

        // Reading

        public SpanElement At(int[] path)
        {
            var element = Root;
            foreach (var index in path)
                element = element.Children[index];
            return element;
        }

        /// <summary>emitters, behaviour_chains or effect_render_stacks under &lt;vfx&gt;; vfx_references at the top.</summary>
        public SpanElement? Section(string name) => name == "vfx_references" ? Root.Child(name) : Root.Child("vfx")?.Child(name);

        public SpanElement? ById(string section, string id) => Section(section)?.Children.FirstOrDefault(c => c.Attr("id") == id);

        public string TextOf(SpanElement element) => XmlText.Unescape(Text[element.ContentStart..element.ContentEnd]);

        public string RawOf(SpanElement element) => Text[element.Start..element.End];

        /// <summary>The whitespace before an element from its last line break on ("\r\n\t\t"), or "" when the element shares
        /// a line with what precedes it.</summary>
        public string LineBreakBefore(SpanElement element)
        {
            var runStart = element.Start;
            while (runStart > 0 && char.IsWhiteSpace(Text[runStart - 1]))
                runStart--;
            if (runStart == element.Start)
                return "";
            var newline = Text.LastIndexOf('\n', element.Start - 1, element.Start - runStart);
            if (newline < 0)
                return "";
            var from = newline - 1 >= runStart && Text[newline - 1] == '\r' ? newline - 1 : newline;
            return Text[from..element.Start];
        }

        /// <summary>The line break a new last child of `parent` takes: its last child's, else the parent's own one tab deeper.</summary>
        public string ChildBreak(SpanElement parent)
        {
            if (parent.Children.Count > 0)
                return LineBreakBefore(parent.Children[^1]);
            var outer = LineBreakBefore(parent);
            return outer.Length > 0 ? outer + "\t" : "";
        }

        // Splices, built against the current text and applied together by Apply

        public Splice SetAttribute(SpanElement element, string name, string value)
        {
            if (element.Attribute(name) is { } attribute)
                return new Splice(attribute.ValueStart, attribute.ValueEnd, XmlText.EscapeAttribute(value, attribute.Quote));
            var at = element.Attributes.Count > 0 ? element.Attributes[^1].ValueEnd + 1 : element.NameEnd;
            return new Splice(at, at, $" {name}=\"{XmlText.EscapeAttribute(value, '"')}\"");
        }

        public Splice SetText(SpanElement element, string text) => element.SelfClosing
            ? throw new InvalidOperationException($"<{element.Name}> has no text to set")
            : new Splice(element.ContentStart, element.ContentEnd, XmlText.EscapeText(text));

        public Splice InsertAfter(SpanElement sibling, string xml) => new(sibling.End, sibling.End, LineBreakBefore(sibling) + xml);

        /// <summary>Appends as the last child, on its own line one tab inside the parent when the parent sits on its own line.</summary>
        public Splice InsertInto(SpanElement parent, string xml)
        {
            if (parent.Children.Count > 0)
                return InsertAfter(parent.Children[^1], xml);
            var inner = ChildBreak(parent);
            var outer = LineBreakBefore(parent);
            if (parent.SelfClosing)
                return new Splice(parent.StartTagClose, parent.End, $">{inner}{xml}{outer}</{parent.Name}>");
            if (string.IsNullOrWhiteSpace(Text[parent.ContentStart..parent.ContentEnd]))
                return new Splice(parent.ContentStart, parent.ContentEnd, inner + xml + outer);
            var at = parent.ContentEnd;   // after a comment, before the whitespace that closes the parent
            while (at > parent.ContentStart && char.IsWhiteSpace(Text[at - 1]))
                at--;
            return new Splice(at, at, inner + xml);
        }

        /// <summary>Removes the element and the line break before it, so no blank line is left.</summary>
        public Splice Remove(SpanElement element)
        {
            var from = element.Start - LineBreakBefore(element).Length;
            if (from == element.Start)
            {
                while (from > 0 && Text[from - 1] is ' ' or '\t')
                    from--;
            }
            return new Splice(from, element.End, "");
        }

        /// <summary>Applies the splices as one undo step. Splices must not overlap; inserts at one point keep their order.
        /// A result that does not re-read is refused and the document is left as it was.</summary>
        public void Apply(IEnumerable<Splice> splices)
        {
            var ordered = splices.Select((splice, index) => (splice, index)).OrderBy(x => x.splice.Start).ThenBy(x => x.index).ToList();
            if (ordered.Count == 0)
                return;
            for (var k = 1; k < ordered.Count; k++)
            {
                if (ordered[k].splice.Start < ordered[k - 1].splice.End)
                    throw new InvalidOperationException("two edits overlap, so neither was made");
            }
            var builder = new StringBuilder(Text);
            for (var k = ordered.Count - 1; k >= 0; k--)
            {
                var s = ordered[k].splice;
                builder.Remove(s.Start, s.End - s.Start).Insert(s.Start, s.Text);
            }
            var text = builder.ToString();
            SpanElement root;
            try
            {
                root = XmlSpanParser.Parse(text);
            }
            catch (VfxParseException e)
            {
                throw new InvalidOperationException($"the edit would break the file, so it was not made: {e.Message}", e);
            }
            PushUndo(Text);
            _redo.Clear();
            Text = text;
            Root = root;
            Changed?.Invoke();
        }

        public void Undo()
        {
            if (_undo.Count == 0)
                return;
            _redo.Push(Text);
            var text = _undo.Last!.Value;
            _undo.RemoveLast();
            Restore(text);
        }

        public void Redo()
        {
            if (_redo.Count == 0)
                return;
            PushUndo(Text);
            Restore(_redo.Pop());
        }

        void PushUndo(string text)
        {
            _undo.AddLast(text);
            if (_undo.Count > UndoDepth)
                _undo.RemoveFirst();
        }

        void Restore(string text)
        {
            Root = XmlSpanParser.Parse(text);
            Text = text;
            Changed?.Invoke();
        }

        /// <summary>An element's text with edits applied to it alone: the copy that becomes "the base, at quality 1".</summary>
        public static string Rewrite(string elementXml, Func<VfxDocument, SpanElement, IEnumerable<Splice>> edits)
        {
            var copy = FromText(elementXml);
            copy.Apply(edits(copy, copy.Root.Children[0]));
            return copy.Text;
        }

        /// <summary>An element around children, each on its own line one tab inside `lineBreak`, or all on one line when it is "".</summary>
        public static string Wrap(string startTag, IReadOnlyList<string> children, string endTag, string lineBreak)
        {
            var inner = lineBreak.Length > 0 ? lineBreak + "\t" : "";
            return startTag + string.Concat(children.Select(c => inner + c)) + (children.Count > 0 ? lineBreak : "") + endTag;
        }
    }
}
