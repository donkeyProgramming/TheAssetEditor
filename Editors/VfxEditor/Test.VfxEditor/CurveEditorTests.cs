using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Editors.VfxEditor.Document;
using Editors.VfxEditor.ViewModels;

namespace Test.VfxEditor
{
    class CurveEditorTests
    {
        const string Text = "x_min,8405056,1;0.0,0.1,linear,log3,-0.05,0.0,0.05,0.0,0;1.0,0.75,sine,sine_recip,-0.05,0.0,0.05,0.0,0|x_max,16728128,1;0.0,0.1,linear,log3,-0.05,0.0,0.05,0.0,0:x,1;0,1";

        // The document re-reads the curve after every key edit. The channel picker, bound as the view binds it, must not
        // drop the picked channel on the way, or the key rows are rebuilt and the box being typed in loses its focus.
        [Test, Apartment(ApartmentState.STA)]
        public void A_reload_keeps_the_key_rows_the_boxes_are_bound_to()
        {
            var editor = new CurveEditor(CurveText.Parse(Text)!, _ => { }, _ => { });
            var picker = new ComboBox { DataContext = editor, DisplayMemberPath = "Name" };
            picker.SetBinding(ItemsControl.ItemsSourceProperty, "Channels");
            picker.SetBinding(Selector.SelectedItemProperty, "SelectedChannel");
            var rows = editor.Keys.ToList();

            editor.Reload(Text.Replace("0.75", "0.8"));

            Assert.That(editor.Keys, Is.EqualTo(rows).AsCollection, "the same row objects");
            Assert.That(editor.Keys[1].Value, Is.EqualTo("0.8"));
        }

        // Each channel across the 300 by 80 box, scaled to the curve's range: the ramp climbs from the bottom-left corner to
        // the top-right, and the flat channel holds its value halfway up.
        [Test]
        public void The_graph_draws_each_channel_across_the_curves_range()
        {
            const string ramp = "a,1,1;0.0,0.0,linear,linear,-0.05,0.0,0.05,0.0,0;1.0,1.0,linear,linear,-0.05,0.0,0.05,0.0,0|b,2,1;0.0,0.5,linear,linear,-0.05,0.0,0.05,0.0,0:x,1;0,1";
            var editor = new CurveEditor(CurveText.Parse(ramp)!, _ => { }, _ => { });
            var climb = editor.Lines[0].Points.Split(' ');
            Assert.That((climb[0], climb[^1]), Is.EqualTo(("0,80", "300,0")));
            Assert.That(editor.Lines[1].Points.Split(' ').Select(p => p.Split(',')[1]).Distinct(), Is.EqualTo(new[] { "40" }));
            Assert.That(editor.Range, Is.EqualTo("0 to 1"));
        }

        [Test]
        public void A_mapping_the_curve_cannot_read_is_reported_and_not_written()
        {
            var commits = new List<string>();
            string? report = null;
            var editor = new CurveEditor(CurveText.Parse(Text)!, commits.Add, m => report = m);
            editor.Mapping = "x,1;0,5";
            Assert.That((commits.Count, editor.Mapping, report != null), Is.EqualTo((0, "x,1;0,1", true)));
            editor.Mapping = "x,0;1,1";
            Assert.That(commits.Single(), Does.EndWith(":x,0;1,1"));
        }

        [Test]
        public void Channels_are_copied_and_removed_from_the_picker()
        {
            var commits = new List<string>();
            string? report = null;
            var editor = new CurveEditor(CurveText.Parse(Text)!, commits.Add, m => report = m);
            editor.AddChannelCommand.Execute(null);
            Assert.That(commits.Single(), Does.Contain("|x_min_copy,"));
            editor.SelectedChannel = editor.Channels[1];
            editor.RemoveChannelCommand.Execute(null);
            Assert.That(report, Does.Contain("x reads x_max"));
        }
    }
}
