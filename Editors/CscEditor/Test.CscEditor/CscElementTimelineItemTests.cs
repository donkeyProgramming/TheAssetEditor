using Editors.CscEditor.Data;
using Editors.CscEditor.ViewModels;

namespace Test.CscEditor
{
    // The element rows above the curve editor: one bar per element's active window.
    public class CscElementTimelineItemTests
    {
        static (CscElementTimelineItem Item, CscElement Element) Create(float begin, float end, string mode, double sceneDuration = 8)
        {
            var element = new CscElement { Id = 3, Kind = CscElementKind.Model, Begin = begin, End = end, TimingMode = mode };
            var item = new CscElementTimelineItem(new CscElementViewModel(element, (_, _) => { }), () => sceneDuration);
            return (item, element);
        }

        [TestCase("duration")]
        [TestCase("pulse")]
        public void Window_IsBeginToEnd(string mode)
        {
            var (item, _) = Create(1, 3, mode);

            Assert.That(item.Start, Is.EqualTo(1));
            Assert.That(item.End, Is.EqualTo(3));
        }

        [Test]
        public void Infinite_RunsToTheSceneEnd()
        {
            var (item, _) = Create(1, 3, "infinite", sceneDuration: 8);

            Assert.That(item.End, Is.EqualTo(8));
            Assert.That(item.Tooltip, Does.Contain("1 - ∞ s"));
        }

        [Test]
        public void Infinite_BeginningAfterTheSceneEnd_HasNoNegativeSpan()
        {
            var (item, _) = Create(9, 3, "infinite", sceneDuration: 8);

            Assert.That(item.End, Is.EqualTo(9));
        }

        [Test]
        public void EndBeforeBegin_IsDrawnAsAnInstant()
        {
            var (item, _) = Create(3, 1, "duration");

            Assert.That(item.End, Is.Null);
        }

        // Out-of-scene times are shown as authored, like the curve editor does.
        [Test]
        public void NegativeBegin_IsKept()
        {
            var (item, _) = Create(-0.5f, 2, "duration");

            Assert.That(item.Start, Is.EqualTo(-0.5));
        }

        [Test]
        public void Refresh_ReportsOnlyWhatChanged()
        {
            var (item, element) = Create(1, 3, "duration");
            var changed = new List<string?>();
            item.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            item.Refresh();
            Assert.That(changed, Is.Empty);

            element.End = 4;
            item.Refresh();
            Assert.That(item.End, Is.EqualTo(4));
            Assert.That(changed, Is.EquivalentTo(new[] { nameof(item.End), nameof(item.Tooltip) }));
        }
    }
}
