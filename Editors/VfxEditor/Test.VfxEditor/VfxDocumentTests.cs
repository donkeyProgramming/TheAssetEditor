using System.Text;
using Editors.VfxEditor.Document;

namespace Test.VfxEditor
{
    class VfxDocumentTests
    {
        // Hand-edited the way Modding Files/source/vfx_edits/br_core_rifle_trail.xml is: UTF-8 with no BOM, LF, a start
        // tag broken across lines, trailing spaces, a comment.
        const string HandEdited = "<shader_library file=\"fx/particle_vfx_library.hlsl\"/><vfx revision=\"1\">\n\t<!-- EMITTERS-->\n\t<emitters>\n        <emitter type=\"continuous_mesh\" id=\"e\">\n\t\t\t<property name=\"ParticleLifeTime\" quality=\"0\"\n              >x_min,1,1;0.0,0.06,linear,linear,-0.05,0.0,0.05,0.0,0:x,0;0,1</property>         \n\t\t</emitter>\n\t</emitters>\n</vfx><vfx_references></vfx_references><editor_data></editor_data>";

        [Test]
        public void An_unedited_document_saves_byte_identical_in_its_own_encoding()
        {
            var utf16 = Fixtures.Utf16(Fixtures.Default);
            var utf8 = Encoding.UTF8.GetBytes(HandEdited);
            byte[] utf8Bom = [0xEF, 0xBB, 0xBF, .. utf8];
            Assert.That(VfxDocument.Load(utf16).Save(), Is.EqualTo(utf16));
            Assert.That(VfxDocument.Load(utf8).Save(), Is.EqualTo(utf8));
            Assert.That(VfxDocument.Load(utf8Bom).Save(), Is.EqualTo(utf8Bom));
            Assert.That(new[] { VfxDocument.Load(utf16).Encoding, VfxDocument.Load(utf8).Encoding, VfxDocument.Load(utf8Bom).Encoding },
                Is.EqualTo(new[] { VfxEncoding.Utf16Bom, VfxEncoding.Utf8, VfxEncoding.Utf8Bom }));
        }

        [Test]
        public void Bytes_that_are_not_text_in_their_encoding_are_refused() =>
            Assert.Throws<DecoderFallbackException>(() => VfxDocument.Load([0x3C, 0x61, 0xC3, 0x28, 0x2F, 0x3E]));

        [Test]
        public void Setting_a_value_changes_only_its_characters()
        {
            var document = Fixtures.Document();
            var before = document.Text;
            var b = Fixtures.Emitter(document).Children.First(c => c.Attr("name") == "B");
            var span = b.Attribute("value")!;
            document.Apply([document.SetAttribute(b, "value", "0.25")]);
            Assert.That(document.Text, Is.EqualTo(before[..span.ValueStart] + "0.25" + before[span.ValueEnd..]));
        }

        [TestCase("<p a=\"1\"/>", "<p a=\"1\" b=\"2\"/>")]
        [TestCase("<p/>", "<p b=\"2\"/>")]
        [TestCase("<p a=\"1\" />", "<p a=\"1\" b=\"2\" />")]
        [TestCase("<p a=\"1\"\r\n   >t</p>", "<p a=\"1\" b=\"2\"\r\n   >t</p>")]
        public void A_missing_attribute_is_added_after_the_last_one(string text, string expected)
        {
            var document = VfxDocument.FromText(text);
            document.Apply([document.SetAttribute(document.Root.Children[0], "b", "2")]);
            Assert.That(document.Text, Is.EqualTo(expected));
        }

        [Test]
        public void Values_are_escaped_for_their_quote()
        {
            var document = VfxDocument.FromText("<p a='1' b=\"2\"/>");
            var p = document.Root.Children[0];
            document.Apply([document.SetAttribute(p, "a", "x'\"<&"), document.SetAttribute(p, "b", "x'\"")]);
            Assert.That(document.Text, Is.EqualTo("<p a='x&apos;\"&lt;&amp;' b=\"x'&quot;\"/>"));
            Assert.That(document.Root.Children[0].Attr("a"), Is.EqualTo("x'\"<&"));
        }

        [Test]
        public void Text_is_replaced_between_the_tags()
        {
            var document = VfxDocument.FromText("<p n=\"1\"\r\n  >old</p>   ");
            document.Apply([document.SetText(document.Root.Children[0], "a<b")]);
            Assert.That(document.Text, Is.EqualTo("<p n=\"1\"\r\n  >a&lt;b</p>   "));
            Assert.That(document.TextOf(document.Root.Children[0]), Is.EqualTo("a<b"));
        }

        [Test]
        public void An_inserted_element_takes_its_siblings_line_break()
        {
            var document = VfxDocument.FromText("<l>\r\n\t<a/>\r\n</l>");
            document.Apply([document.InsertAfter(document.Root.Children[0].Children[0], "<b/>")]);
            Assert.That(document.Text, Is.EqualTo("<l>\r\n\t<a/>\r\n\t<b/>\r\n</l>"));
        }

        [TestCase("<r>\r\n\t<l></l>\r\n</r>", "<r>\r\n\t<l>\r\n\t\t<b/>\r\n\t</l>\r\n</r>")]
        [TestCase("<r>\r\n\t<l>\r\n\t</l>\r\n</r>", "<r>\r\n\t<l>\r\n\t\t<b/>\r\n\t</l>\r\n</r>")]
        [TestCase("<r>\r\n\t<l/>\r\n</r>", "<r>\r\n\t<l>\r\n\t\t<b/>\r\n\t</l>\r\n</r>")]
        [TestCase("<r><l><!-- c --></l></r>", "<r><l><!-- c --><b/></l></r>")]
        [TestCase("<r>\r\n\t<l>\r\n\t\t<a/>\r\n\t</l>\r\n</r>", "<r>\r\n\t<l>\r\n\t\t<a/>\r\n\t\t<b/>\r\n\t</l>\r\n</r>")]
        public void An_element_is_appended_into_its_parent(string text, string expected)
        {
            var document = VfxDocument.FromText(text);
            document.Apply([document.InsertInto(document.Root.Children[0].Children[0], "<b/>")]);
            Assert.That(document.Text, Is.EqualTo(expected));
        }

        [TestCase(0, "<l>\r\n\t<b/>\r\n</l>")]
        [TestCase(1, "<l>\r\n\t<a/>\r\n</l>")]
        public void A_removed_element_leaves_no_blank_line(int index, string expected)
        {
            var document = VfxDocument.FromText("<l>\r\n\t<a/>\r\n\t<b/>\r\n</l>");
            document.Apply([document.Remove(document.Root.Children[0].Children[index])]);
            Assert.That(document.Text, Is.EqualTo(expected));
        }

        // An element sharing a line with the one before it takes the spaces and tabs between them along.
        [Test]
        public void A_removed_element_on_a_shared_line_leaves_no_trailing_space()
        {
            var document = VfxDocument.FromText("<l><a/> \t<b/></l>");
            document.Apply([document.Remove(document.Root.Children[0].Children[1])]);
            Assert.That(document.Text, Is.EqualTo("<l><a/></l>"));
        }

        [Test]
        public void Two_inserts_at_one_point_keep_their_order()
        {
            var document = VfxDocument.FromText("<l><a/></l>");
            var a = document.Root.Children[0].Children[0];
            document.Apply([document.InsertAfter(a, "<b/>"), document.InsertAfter(a, "<c/>")]);
            Assert.That(document.Text, Is.EqualTo("<l><a/><b/><c/></l>"));
        }

        [Test]
        public void Overlapping_edits_are_refused_and_change_nothing()
        {
            var document = VfxDocument.FromText("<a v=\"1\"/>");
            Assert.Throws<InvalidOperationException>(() => document.Apply([new Splice(0, 5, ""), new Splice(3, 6, "")]));
            Assert.That((document.Text, document.CanUndo), Is.EqualTo(("<a v=\"1\"/>", false)));
        }

        [Test]
        public void An_edit_that_would_break_the_file_is_refused()
        {
            var document = VfxDocument.FromText("<a/>");
            Assert.Throws<InvalidOperationException>(() => document.Apply([new Splice(2, 3, "")]));
            Assert.That((document.Text, document.CanUndo), Is.EqualTo(("<a/>", false)));
        }

        [Test]
        public void Undo_keeps_the_last_hundred_edits()
        {
            var document = VfxDocument.FromText("<a v=\"0\"/>");
            for (var i = 1; i <= 150; i++)
                document.Apply([document.SetAttribute(document.Root.Children[0], "v", i.ToString())]);
            var undone = 0;
            for (; document.CanUndo; undone++)
                document.Undo();
            Assert.That((undone, document.Text), Is.EqualTo((100, "<a v=\"50\"/>")));
        }

        [Test]
        public void Undo_and_redo_step_through_edits()
        {
            var document = VfxDocument.FromText("<a v=\"1\"/>");
            var changes = 0;
            document.Changed += () => changes++;
            document.Apply([document.SetAttribute(document.Root.Children[0], "v", "2")]);
            document.Undo();
            Assert.That(document.Text, Is.EqualTo("<a v=\"1\"/>"));
            document.Redo();
            Assert.That(document.Text, Is.EqualTo("<a v=\"2\"/>"));
            document.Undo();
            document.Apply([document.SetAttribute(document.Root.Children[0], "v", "3")]);
            Assert.That((document.CanRedo, changes), Is.EqualTo((false, 5)));
        }

        [Test]
        public void A_file_without_vfx_or_references_is_not_an_effect()
        {
            Assert.That(VfxDocument.FromText("<PROPERTY_SET/>").IsEffect, Is.False);
            Assert.That(Fixtures.Document().IsEffect, Is.True);
        }

        [Test]
        public void Sections_and_ids_are_found_by_name()
        {
            var document = Fixtures.Document();
            Assert.That(document.ById("behaviour_chains", "e1")?.Name, Is.EqualTo("behaviour_chain"));
            Assert.That(document.Section("vfx_references")?.Children.Count, Is.EqualTo(1));
            Assert.That(document.ById("emitters", "nope"), Is.Null);
        }
    }
}
