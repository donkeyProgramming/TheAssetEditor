using Editors.VfxEditor.Document;

namespace Test.VfxEditor
{
    class TierEditsTests
    {
        // The fixture emitter: A is 1 at Ultra and High and 5 at Medium; B is 7 at Ultra only; C is 3 at Medium only.
        static string Values(VfxDocument d, string name) =>
            string.Join(",", Enumerable.Range(0, 3).Select(t => TierEdits.Tiers(Fixtures.Emitter(d), name).GetValueOrDefault(t)?.Attr("value") ?? "-"));

        [Test]
        public void A_linked_edit_reaches_every_tier_that_held_the_old_value()
        {
            var d = Fixtures.Document();
            d.Apply(TierEdits.SetProperty(d, Fixtures.Emitter(d), "A", "value", 0, "2", thisTierOnly: false));
            Assert.That(Values(d, "A"), Is.EqualTo("2,2,5"));
        }

        [Test]
        public void A_linked_edit_of_an_override_leaves_the_base()
        {
            var d = Fixtures.Document();
            d.Apply(TierEdits.SetProperty(d, Fixtures.Emitter(d), "A", "value", 2, "9", false));
            Assert.That(Values(d, "A"), Is.EqualTo("1,1,9"));
        }

        [Test]
        public void A_linked_edit_of_an_inherited_value_changes_the_base()
        {
            var d = Fixtures.Document();
            d.Apply(TierEdits.SetProperty(d, Fixtures.Emitter(d), "B", "value", 1, "4", false));
            Assert.That(Values(d, "B"), Is.EqualTo("4,-,-"));
        }

        [Test]
        public void This_tier_only_at_ultra_keeps_the_old_value_on_the_tiers_that_inherited_it()
        {
            var d = Fixtures.Document();
            d.Apply(TierEdits.SetProperty(d, Fixtures.Emitter(d), "B", "value", 0, "8", true));
            Assert.That(Values(d, "B"), Is.EqualTo("8,7,7"));
            Assert.That(d.Text, Does.Contain("<property name=\"B\" quality=\"0\" value=\"8\"/>\r\n\t\t\t<property name=\"B\" quality=\"1\" value=\"7\"/>\r\n\t\t\t<property name=\"B\" quality=\"2\" value=\"7\"/>"));
        }

        [Test]
        public void This_tier_only_below_ultra_adds_an_override()
        {
            var d = Fixtures.Document();
            d.Apply(TierEdits.SetProperty(d, Fixtures.Emitter(d), "B", "value", 1, "3", true));
            Assert.That(Values(d, "B"), Is.EqualTo("7,3,-"));
        }

        [Test]
        public void A_property_written_only_at_medium_is_in_effect_only_there()
        {
            var d = Fixtures.Document();
            string Names(int tier) => string.Join(",", TierEdits.EffectiveProperties(Fixtures.Emitter(d), tier).Select(p => p.Attr("name")));
            Assert.That(Names(0), Is.EqualTo("A,B,LifeTime,Curve"));
            Assert.That(Names(2), Is.EqualTo("A,B,C,LifeTime,Curve"));
            d.Apply(TierEdits.SetProperty(d, Fixtures.Emitter(d), "C", "value", 2, "4", false));
            Assert.That(Values(d, "C"), Is.EqualTo("-,-,4"));
            d.Apply(TierEdits.SetProperty(d, Fixtures.Emitter(d), "B", "value", 0, "6", false));
            Assert.That(Values(d, "C"), Is.EqualTo("-,-,4"));
        }

        [Test]
        public void A_curve_is_edited_through_its_text()
        {
            var d = Fixtures.Document();
            var edited = Fixtures.CurveString.Replace("0.0,1.0,linear", "0.0,2.0,linear");
            d.Apply(TierEdits.SetProperty(d, Fixtures.Emitter(d), "Curve", null, 0, edited, false));
            Assert.That(d.TextOf(TierEdits.Effective(Fixtures.Emitter(d), "Curve", 0)!), Is.EqualTo(edited));
        }

        static string Alphas(VfxDocument d) => string.Join(",", Fixtures.Chain(d).ChildrenNamed("quality")
            .Select(b => TierEdits.PropertyIn(TierEdits.Modifier(b, "ps_texture", 0), "pre_multiplied_alpha")!.Attr("value")));

        [Test]
        public void A_linked_modifier_edit_reaches_the_blocks_that_matched()
        {
            var d = Fixtures.Document();
            d.Apply(TierEdits.SetModifierProperty(d, Fixtures.Chain(d), 0, "ps_texture", 0, "pre_multiplied_alpha", "value", "false", false));
            Assert.That(Alphas(d), Is.EqualTo("false,false,false"));
            d.Apply(TierEdits.SetModifierProperty(d, Fixtures.Chain(d), 1, "ps_texture", 0, "pre_multiplied_alpha", "value", "true", true));
            Assert.That(Alphas(d), Is.EqualTo("false,true,false"));
        }

        [Test]
        public void The_second_modifier_of_a_fragment_is_its_own()
        {
            var d = Fixtures.Document();
            d.Apply(TierEdits.SetModifierProperty(d, Fixtures.Chain(d), 0, "vs_colour", 1, "p", "value", "6", false));
            var block = TierEdits.Block(Fixtures.Chain(d), 2)!;
            Assert.That((TierEdits.PropertyIn(TierEdits.Modifier(block, "vs_colour", 0), "p")!.Attr("value"),
                TierEdits.PropertyIn(TierEdits.Modifier(block, "vs_colour", 1), "p")!.Attr("value")), Is.EqualTo(("1", "6")));
        }
    }
}
