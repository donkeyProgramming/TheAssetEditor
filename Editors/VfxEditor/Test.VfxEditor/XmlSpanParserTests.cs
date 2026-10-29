using Editors.VfxEditor.Document;

namespace Test.VfxEditor
{
    class XmlSpanParserTests
    {
        [Test]
        public void Records_spans_and_attributes()
        {
            const string text = "<a x=\"1\" y='2'>hi</a><b/>";
            var root = XmlSpanParser.Parse(text);
            Assert.That(root.Children.Select(c => c.Name), Is.EqualTo(new[] { "a", "b" }));
            var a = root.Children[0];
            Assert.That(a.Attributes.Select(x => (x.Name, x.Value, x.Quote)), Is.EqualTo(new[] { ("x", "1", '"'), ("y", "2", '\'') }));
            Assert.That(text[a.ContentStart..a.ContentEnd], Is.EqualTo("hi"));
            Assert.That(text[a.Start..a.End], Is.EqualTo("<a x=\"1\" y='2'>hi</a>"));
            Assert.That(text[a.Attributes[0].ValueStart..a.Attributes[0].ValueEnd], Is.EqualTo("1"));
            var b = root.Children[1];
            Assert.That(b.SelfClosing, Is.True);
            Assert.That((b.Start, b.StartTagClose, b.End), Is.EqualTo((text.Length - 4, text.Length - 2, text.Length)));
        }

        [Test]
        public void Skips_declarations_comments_and_cdata()
        {
            var root = XmlSpanParser.Parse("<?xml version=\"1.0\"?><!-- <c/> --><a><![CDATA[<x/>]]></a>");
            Assert.That(root.Children.Select(c => c.Name), Is.EqualTo(new[] { "a" }));
            Assert.That(root.Children[0].Children, Is.Empty);
        }

        [Test]
        public void A_start_tag_may_break_across_lines()
        {
            const string text = "<property name=\"X\" quality=\"0\"\r\n              >x_min</property>";
            var p = XmlSpanParser.Parse(text).Children[0];
            Assert.That(text[p.ContentStart..p.ContentEnd], Is.EqualTo("x_min"));
            Assert.That(p.Attr("quality"), Is.EqualTo("0"));
        }

        [Test]
        public void Values_are_unescaped_and_the_raw_text_kept()
        {
            var attribute = XmlSpanParser.Parse("<a v=\"&amp;&lt;&#65;&#x42;\"/>").Children[0].Attributes[0];
            Assert.That(attribute.Value, Is.EqualTo("&<AB"));
            Assert.That(attribute.Raw, Is.EqualTo("&amp;&lt;&#65;&#x42;"));
        }

        [TestCase("<a></b>", 1, 4)]
        [TestCase("<a>\n  </b>", 2, 3)]
        [TestCase("<a>", 1, 1)]
        [TestCase("<a v=\"1/>", 1, 6)]
        public void Refuses_what_it_cannot_place_with_line_and_column(string text, int line, int column)
        {
            var e = Assert.Throws<VfxParseException>(() => XmlSpanParser.Parse(text))!;
            Assert.That((e.Line, e.Column), Is.EqualTo((line, column)));
        }

        [Test]
        public void A_path_finds_the_element_again()
        {
            var root = XmlSpanParser.Parse("<a><b/><c><d/></c></a>");
            Assert.That(root.Children[0].Children[1].Children[0].Path, Is.EqualTo(new[] { 0, 1, 0 }));
        }
    }
}
