using System.Globalization;
using System.Text;

namespace Editors.VfxEditor.Document
{
    /// <summary>An attribute and where its value sits: Raw is the text between the quotes as written.</summary>
    public sealed record SpanAttribute(string Name, string Raw, int ValueStart, int ValueEnd, char Quote)
    {
        public string Value => XmlText.Unescape(Raw);
    }

    /// <summary>An element's positions in the text: Start..End from its '&lt;' to one past its last '&gt;', StartTagClose at the
    /// start tag's '/&gt;' or '&gt;', ContentStart..ContentEnd between the tags. The root is synthetic and spans the file.</summary>
    public sealed class SpanElement
    {
        public string Name { get; internal set; } = "";
        public int Start { get; internal set; }
        public int NameEnd { get; internal set; }
        public int StartTagClose { get; internal set; }
        public int End { get; internal set; }
        public bool SelfClosing { get; internal set; }
        public int ContentStart { get; internal set; }
        public int ContentEnd { get; internal set; }
        public SpanElement? Parent { get; internal set; }
        public List<SpanAttribute> Attributes { get; } = [];
        public List<SpanElement> Children { get; } = [];

        public SpanAttribute? Attribute(string name) => Attributes.FirstOrDefault(a => a.Name == name);
        public string? Attr(string name) => Attribute(name)?.Value;
        public SpanElement? Child(string name) => Children.FirstOrDefault(c => c.Name == name);
        public IEnumerable<SpanElement> ChildrenNamed(string name) => Children.Where(c => c.Name == name);

        /// <summary>Child indices from the root, which find this element again in a re-read of the same structure.</summary>
        public int[] Path
        {
            get
            {
                var path = new List<int>();
                for (var element = this; element.Parent != null; element = element.Parent)
                    path.Add(element.Parent.Children.IndexOf(element));
                path.Reverse();
                return [.. path];
            }
        }
    }

    public sealed class VfxParseException(string message, int line, int column) : Exception($"{message} (line {line}, column {column})")
    {
        public int Line { get; } = line;
        public int Column { get; } = column;
    }

    /// <summary>Reads XML into SpanElements, keeping every position and stepping over comments, processing instructions,
    /// CDATA and text. Not a validating parser: it reads what CA writes and refuses what it cannot place.</summary>
    public static class XmlSpanParser
    {
        public static SpanElement Parse(string text)
        {
            var root = new SpanElement { ContentEnd = text.Length, End = text.Length };
            var open = new Stack<SpanElement>();
            open.Push(root);
            var i = 0;
            while ((i = text.IndexOf('<', i)) >= 0)
            {
                if (Starts(text, i, "<!--"))
                    i = Skip(text, i, "-->");
                else if (Starts(text, i, "<![CDATA["))
                    i = Skip(text, i, "]]>");
                else if (Starts(text, i, "<?"))
                    i = Skip(text, i, "?>");
                else if (Starts(text, i, "<!"))
                    i = Skip(text, i, ">");
                else if (Starts(text, i, "</"))
                    i = EndTag(text, i, open);
                else
                    i = StartTag(text, i, open);
            }
            if (open.Count > 1)
                throw Error(text, open.Peek().Start, $"<{open.Peek().Name}> is never closed");
            return root;
        }

        static int EndTag(string text, int lt, Stack<SpanElement> open)
        {
            var nameEnd = ReadName(text, lt + 2);
            var name = text[(lt + 2)..nameEnd];
            var gt = SkipSpace(text, nameEnd);
            if (gt >= text.Length || text[gt] != '>')
                throw Error(text, lt, $"</{name} does not close");
            var element = open.Peek();
            if (open.Count == 1 || element.Name != name)
                throw Error(text, lt, $"</{name}> does not match <{element.Name}>");
            element.ContentEnd = lt;
            element.End = gt + 1;
            open.Pop();
            return gt + 1;
        }

        static int StartTag(string text, int lt, Stack<SpanElement> open)
        {
            var nameEnd = ReadName(text, lt + 1);
            if (nameEnd == lt + 1)
                throw Error(text, lt, "a '<' is not followed by a name");
            var element = new SpanElement { Name = text[(lt + 1)..nameEnd], Start = lt, NameEnd = nameEnd, Parent = open.Peek() };
            var p = nameEnd;
            while (true)
            {
                p = SkipSpace(text, p);
                if (p >= text.Length)
                    throw Error(text, lt, $"<{element.Name} does not close");
                if (text[p] == '>' || (text[p] == '/' && p + 1 < text.Length && text[p + 1] == '>'))
                    break;
                var attributeEnd = ReadName(text, p);
                if (attributeEnd == p)
                    throw Error(text, p, $"'{text[p]}' in <{element.Name}>");
                var attribute = text[p..attributeEnd];
                p = SkipSpace(text, attributeEnd);
                if (p >= text.Length || text[p] != '=')
                    throw Error(text, p, $"attribute {attribute} has no '='");
                p = SkipSpace(text, p + 1);
                if (p >= text.Length || text[p] is not ('"' or '\''))
                    throw Error(text, p, $"attribute {attribute} has no quoted value");
                var close = text.IndexOf(text[p], p + 1);
                if (close < 0)
                    throw Error(text, p, $"the value of {attribute} never closes");
                element.Attributes.Add(new SpanAttribute(attribute, text[(p + 1)..close], p + 1, close, text[p]));
                p = close + 1;
            }
            element.StartTagClose = p;
            element.SelfClosing = text[p] == '/';
            element.End = element.ContentStart = element.ContentEnd = p + (element.SelfClosing ? 2 : 1);
            open.Peek().Children.Add(element);
            if (!element.SelfClosing)
                open.Push(element);
            return element.End;
        }

        static bool Starts(string text, int i, string token) => string.CompareOrdinal(text, i, token, 0, token.Length) == 0;

        static int Skip(string text, int i, string end)
        {
            var at = text.IndexOf(end, i, StringComparison.Ordinal);
            if (at < 0)
                throw Error(text, i, $"{text.Substring(i, Math.Min(4, text.Length - i))} is never closed");
            return at + end.Length;
        }

        static int ReadName(string text, int i)
        {
            while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '-' or '.' or ':'))
                i++;
            return i;
        }

        static int SkipSpace(string text, int i)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
            return i;
        }

        static VfxParseException Error(string text, int at, string message)
        {
            int line = 1, lineStart = 0;
            for (var k = 0; k < at && k < text.Length; k++)
            {
                if (text[k] == '\n')
                {
                    line++;
                    lineStart = k + 1;
                }
            }
            return new VfxParseException(message, line, at - lineStart + 1);
        }
    }

    public static class XmlText
    {
        public static string EscapeText(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        public static string EscapeAttribute(string value, char quote) =>
            quote == '"' ? EscapeText(value).Replace("\"", "&quot;") : EscapeText(value).Replace("'", "&apos;");

        public static string Unescape(string raw)
        {
            if (!raw.Contains('&'))
                return raw;
            var output = new StringBuilder(raw.Length);
            for (var i = 0; i < raw.Length; i++)
            {
                var semicolon = raw[i] == '&' ? raw.IndexOf(';', i) : -1;
                var decoded = semicolon < 0 ? null : Entity(raw[(i + 1)..semicolon]);
                if (decoded == null)
                {
                    output.Append(raw[i]);
                    continue;
                }
                output.Append(decoded);
                i = semicolon;
            }
            return output.ToString();
        }

        static string? Entity(string name)
        {
            switch (name)
            {
                case "amp": return "&";
                case "lt": return "<";
                case "gt": return ">";
                case "quot": return "\"";
                case "apos": return "'";
            }
            if (!name.StartsWith('#'))
                return null;
            var hex = name.StartsWith("#x", StringComparison.OrdinalIgnoreCase);
            return int.TryParse(hex ? name[2..] : name[1..], hex ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture, out var code)
                && code is >= 0 and <= 0x10FFFF and not (>= 0xD800 and <= 0xDFFF)
                ? char.ConvertFromUtf32(code)
                : null;
        }
    }
}
