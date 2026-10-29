using Editors.VfxEditor.Document;

namespace Test.VfxEditor
{
    class CurveTextTests
    {
        const string Text = "  x_min,8405056,1;0.0,0.1,linear,log3,-0.05,0.0,0.05,0.0,0;1.0,0.75,sine,sine_recip,-0.05,0.0,0.05,0.0,0|x_max,16728128,1;0.0,0.1,linear,log3,-0.05,0.0,0.05,0.0,0:x,1;0,1 ";

        [Test]
        public void Rebuilds_the_text_it_read()
        {
            var curve = CurveText.Parse(Text)!;
            Assert.That(curve.ToString(), Is.EqualTo(Text));
            Assert.That(curve.Channels.Select(c => (c.Name, c.Keys.Count)), Is.EqualTo(new[] { ("x_min", 2), ("x_max", 1) }));
            Assert.That(curve.Mapping, Is.EqualTo("x,1;0,1"));
        }

        [TestCase("x_min,1,1;0.0,1.0,linear:x,0;0,0")]
        [TestCase("no mapping here")]
        public void Text_that_is_not_a_curve_is_refused(string text) => Assert.That(CurveText.Parse(text), Is.Null);

        // 46 vanilla curves have channels with no keys (SpawnAreaCurve, SpawnAreaBox, EmitterPosition, ParticleScale); this one
        // is cine_wh2_dlc11_lib_ghost_ship.xml's SpawnAreaCurve.
        [Test]
        public void A_channel_without_keys_is_kept_and_takes_a_first_key()
        {
            const string text = "inner_radius_min,8405056,1|inner_radius_max,16728128,1|outer_radius_min,4227136,1|outer_radius_max,4259648,1:inner_radius,0;0,1|outer_radius,0;2,3";
            var curve = CurveText.Parse(text)!;
            Assert.That(curve.ToString(), Is.EqualTo(text));
            curve.InsertKeyAfter(curve.Channels[0], -1);
            Assert.That(curve.Channels[0].Keys.Single(), Is.EqualTo(new[] { "0.0", "0.0", "linear", "linear", "-0.05", "0.0", "0.05", "0.0", "0" }));
        }

        [Test]
        public void A_new_key_copies_the_picked_one_halfway_to_the_next()
        {
            var curve = CurveText.Parse(Text)!;
            curve.InsertKeyAfter(curve.Channels[0], 0);
            Assert.That(curve.Channels[0].Keys[1], Is.EqualTo(new[] { "0.5", "0.1", "linear", "log3", "-0.05", "0.0", "0.05", "0.0", "0" }));
            Assert.That(curve.ToString(), Does.EndWith(":x,1;0,1 "));
        }

        [Test]
        public void A_new_key_after_the_last_goes_halfway_to_the_end()
        {
            var curve = CurveText.Parse(Text)!;
            curve.InsertKeyAfter(curve.Channels[1], 0);
            Assert.That(curve.Channels[1].Keys[1][0], Is.EqualTo("0.5"));
        }

        // A copy goes to the end, so no mapping index moves; a removal is refused while an output reads the channel, and moves
        // the indices past it down.
        [Test]
        public void A_channel_is_copied_to_the_end_and_one_no_output_reads_is_removed()
        {
            var curve = CurveText.Parse(Text)!;
            curve.AddChannel(0);
            Assert.That(curve.Channels.Select(c => c.Name), Is.EqualTo(new[] { "x_min", "x_max", "x_min_copy" }));
            Assert.That(curve.Channels[2].Keys, Is.EqualTo(curve.Channels[0].Keys).AsCollection.And.Not.SameAs(curve.Channels[0].Keys));
            var refused = Assert.Throws<InvalidOperationException>(() => curve.RemoveChannel(1));
            Assert.That(refused!.Message, Does.Contain("x reads x_max"));
            curve.SetMapping("x,1;0,2");
            curve.RemoveChannel(1);
            Assert.That((curve.Channels.Count, curve.Mapping), Is.EqualTo((2, "x,1;0,1")));
        }

        [TestCase("x,1;0,1|y,0;1,1", true)]
        [TestCase("x,1;0,2", false)]
        [TestCase("x,2;0,1", false)]
        [TestCase("x;0,1", false)]
        [TestCase(",1;0,1", false)]
        [TestCase("", false)]
        public void A_mapping_names_channels_the_curve_has(string mapping, bool valid)
        {
            var curve = CurveText.Parse(Text)!;
            if (valid)
            {
                curve.SetMapping(mapping);
                Assert.That(curve.ToString(), Does.EndWith(":" + mapping + " "));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => curve.SetMapping(mapping));
                Assert.That(curve.Mapping, Is.EqualTo("x,1;0,1"));
            }
        }

        [TestCase(0, "0.25", true)]
        [TestCase(0, "abc", false)]
        [TestCase(1, "1e39", false)]
        [TestCase(2, "bezier_c", true)]
        [TestCase(3, "cubic", false)]
        public void Fields_are_checked_by_kind(int field, string value, bool valid) =>
            Assert.That(CurveText.IsValidField(field, value), Is.EqualTo(valid));
    }
}
