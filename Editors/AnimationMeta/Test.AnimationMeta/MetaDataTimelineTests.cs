using Editors.AnimationMeta.SuperView;
using Editors.AnimationMeta.SuperView.Timeline;
using Editors.Shared.Core.Common.BaseControl;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.AnimationPack;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    // Classification ported from Szy-Cathay/TheAssetEditor-CN MetaDataInspectionIndexTests.
    public class MetaDataInspectionIndexTests
    {
        static FirePos_v10 FirePos(float start, float end) => new() { Name = "FIRE_POS", Version = 10, StartTime = start, EndTime = end };

        static MetaDataInspectionItem Classify(ParsedMetadataAttribute source, bool fieldsValid = true) =>
            MetaDataInspectionIndex.Create([new MetaDataInspectionSource(source, MetaDataDocumentOwner.Animation, fieldsValid)]).Items.Single();

        [Test]
        public void Instant() => Assert.That(Classify(FirePos(1, 1)).MarkerKind, Is.EqualTo(MetaDataTimelineMarkerKind.Instant));

        [Test]
        public void Range() => Assert.That(Classify(FirePos(1, 2)).MarkerKind, Is.EqualTo(MetaDataTimelineMarkerKind.Range));

        [Test]
        public void ZeroRange_OnAWholeAnimationTag_IsWholeAnimation() =>
            Assert.That(Classify(new Prop_v10 { Name = "PROP", Version = 10 }).MarkerKind, Is.EqualTo(MetaDataTimelineMarkerKind.WholeAnimation));

        [Test]
        public void ZeroRange_OnAnyOtherTag_IsAnInstantAtZero() =>
            Assert.That(Classify(FirePos(0, 0)).MarkerKind, Is.EqualTo(MetaDataTimelineMarkerKind.Instant));

        [Test]
        public void Effect_UsesItsStartAndEndTimes()
        {
            var item = Classify(new Effect_v11 { Name = "EFFECT", Version = 11, StartTime = 0.5f, EndTime = 1.5f });

            Assert.That(item.MarkerKind, Is.EqualTo(MetaDataTimelineMarkerKind.Range));
            Assert.That(item.TimeRange!.Value.StartTime, Is.EqualTo(0.5f));
        }

        [Test]
        public void EffectV1_HasNoTimes() =>
            Assert.That(Classify(new Effect_v1 { Name = "EFFECT", Version = 1 }).TimeRange, Is.Null);

        // Vanilla authors hundreds of tags a few milliseconds before 0; they still show.
        [Test]
        public void SlightlyNegativeTime_StillGetsAMarker() =>
            Assert.That(Classify(FirePos(-0.003f, -0.003f)).MarkerKind, Is.EqualTo(MetaDataTimelineMarkerKind.Instant));

        [Test]
        public void ReversedRange_IsAnInstantAtItsStart() =>
            Assert.That(Classify(FirePos(2, 1)).MarkerKind, Is.EqualTo(MetaDataTimelineMarkerKind.Instant));

        [Test]
        public void NonFiniteTime_HasNoMarker() =>
            Assert.That(Classify(FirePos(float.NaN, 1)).MarkerKind, Is.Null);

        [Test]
        public void InvalidFields_HaveNoMarker() =>
            Assert.That(Classify(FirePos(1, 2), fieldsValid: false).MarkerKind, Is.Null);

        [Test]
        public void UntimedTag_HasNoTimeRange()
        {
            var item = Classify(new FirePos_v0 { Name = "FIRE_POS", Version = 0 });

            Assert.That(item.TimeRange, Is.Null);
            Assert.That(item.MarkerKind, Is.Null);
        }

        [Test]
        public void SameLookingTagsInBothFiles_StayApart()
        {
            var persistent = FirePos(1, 2);
            var animation = FirePos(1, 2);

            var index = MetaDataInspectionIndex.Create([
                new MetaDataInspectionSource(persistent, MetaDataDocumentOwner.Persistent),
                new MetaDataInspectionSource(animation, MetaDataDocumentOwner.Animation)]);

            Assert.That(index.Items[0].Source, Is.SameAs(persistent));
            Assert.That(index.Items[0].Owner, Is.EqualTo(MetaDataDocumentOwner.Persistent));
            Assert.That(index.Items[1].Source, Is.SameAs(animation));
            Assert.That(index.Items[1].Owner, Is.EqualTo(MetaDataDocumentOwner.Animation));
        }

        [Test]
        public void TagColour_IsStablePerName()
        {
            Assert.That(MetaDataTimelineItem.TagColour("FIRE_POS"), Is.EqualTo(MetaDataTimelineItem.TagColour("fire_pos")));
            Assert.That(MetaDataTimelineItem.TagColour("FIRE_POS"), Is.Not.EqualTo(MetaDataTimelineItem.TagColour("SOUND_TRIGGER")));
        }
    }

    // The SuperView puts the META tags on the animation player's scrub bar.
    public class SuperViewTimelineTests
    {
        AssetEditorTestRunner _runner = null!;
        SuperViewViewModel _superView = null!;

        [SetUp]
        public void Setup()
        {
            _runner = new AssetEditorTestRunner();
            _runner.CreateCaContainer();
            _runner.LoadPackFile(PathHelper.GetDataFile("Throt.pack"), true);

            _superView = (SuperViewViewModel)_runner.ServiceProvider.GetRequiredService<IEditorManager>().Create(EditorEnums.SuperView_Editor)!;
            _superView.Load(new AnimationToolInput
            {
                Mesh = _runner.PackFileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition")!,
                FragmentName = @"animations/database/battle/bin/hu17_dlc16_throt.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5"),
            });
        }

        List<MetaDataTimelineItem> Markers => _superView.Player.TimelineItems.Cast<MetaDataTimelineItem>().ToList();

        [Test]
        public void EveryTimedTag_GetsAMarker()
        {
            var timedTags = _superView.PersistentMetaEditor.Tags.Concat(_superView.MetaEditor.Tags)
                .Count(x => ParsedMetadataTimeRange.TryCreate(x._input, out _));

            Assert.That(timedTags, Is.GreaterThan(0));
            Assert.That(Markers, Has.Count.EqualTo(timedTags));
            Assert.That(Markers.Where(x => x.Item.Owner == MetaDataDocumentOwner.Persistent).All(x => x.Lane == 0));
            Assert.That(Markers.Where(x => x.Item.Owner == MetaDataDocumentOwner.Animation).All(x => x.Lane == 1));
        }

        [Test]
        public void ClickingAMarker_OpensItsTag_AndSeeksToItsStart()
        {
            var marker = Markers.Where(x => x.Item.Owner == MetaDataDocumentOwner.Animation).OrderBy(x => x.Start).Last(x => x.Start > 0);
            _superView.SelectedTabControllerIndex = 0;

            _superView.Player.TimelineItemClickCommand.Execute(marker);

            Assert.That(_superView.SelectedTabControllerIndex, Is.EqualTo(1));
            Assert.That(_superView.MetaEditor.SelectedTag!._input, Is.SameAs(marker.Item.Source));
            Assert.That(_superView.Player.SelectedTimelineItem, Is.SameAs(marker));
            var player = _superView.SceneObjects[0].Data.Player;
            var frameTime = player.AnimationClip.MicrosecondsPerFrame / 1_000_000.0;
            Assert.That(player.GetTimeUs() / 1_000_000.0, Is.EqualTo(marker.Start).Within(frameTime));
        }

        [Test]
        public void SelectingATag_HighlightsItsMarker_OnlyOnItsOwnTab()
        {
            var tag = _superView.MetaEditor.Tags.First(x => ParsedMetadataTimeRange.TryCreate(x._input, out _));
            _superView.SelectedTabControllerIndex = 1;

            _superView.MetaEditor.SelectedTag = tag;
            Assert.That(((MetaDataTimelineItem)_superView.Player.SelectedTimelineItem!).Item.Source, Is.SameAs(tag._input));

            _superView.SelectedTabControllerIndex = 0;
            Assert.That(_superView.Player.SelectedTimelineItem, Is.Null);
        }

        [Test]
        public void EditingATagsTime_MovesItsMarker()
        {
            var tag = _superView.MetaEditor.Tags.First(x => x._input is DecodedMetaEntryBase or DecodedMetaEntryBase_v2);
            var startField = tag.Variables.First(x => x.FieldName == "Start Time");

            startField.ValueAsString = "0.123";

            Assert.That(Markers.Any(x => ReferenceEquals(x.Item.Source, tag._input) && Math.Abs(x.Start - 0.123) < 1e-6));
        }

        [Test]
        public void DeletingATag_RemovesItsMarker()
        {
            var tag = _superView.MetaEditor.Tags.First(x => ParsedMetadataTimeRange.TryCreate(x._input, out _));
            var before = Markers.Count;

            _superView.MetaEditor.ParsedFile!.Attributes.Remove(tag._input);
            _superView.MetaEditor.UpdateView();

            Assert.That(Markers, Has.Count.EqualTo(before - 1));
            Assert.That(Markers.Any(x => ReferenceEquals(x.Item.Source, tag._input)), Is.False);
        }

        [Test]
        public void WholeAnimationMarkers_SpanTheClip()
        {
            var length = _superView.SceneObjects[0].Data.Player.GetAnimationLengthUs() / 1_000_000.0;
            var whole = Markers.Where(x => x.Item.MarkerKind == MetaDataTimelineMarkerKind.WholeAnimation).ToList();

            Assert.That(length, Is.GreaterThan(0));
            Assert.That(whole.All(x => x.Start == 0 && x.End == length));
        }
    }
}
