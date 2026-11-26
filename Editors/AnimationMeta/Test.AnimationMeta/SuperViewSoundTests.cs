using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Editors.AnimationMeta.Presentation;
using Editors.AnimationMeta.SuperView;
using Editors.Audio.SoundTriggers;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using Microsoft.Xna.Framework;
using Moq;
using NAudio.Wave;
using Shared.Core.Commands;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    sealed class UiThread : SynchronizationContext
    {
        readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _posted = new();

        public override void Post(SendOrPostCallback d, object? state) => _posted.Enqueue((d, state));

        public bool RunPosted()
        {
            var ran = false;
            while (_posted.TryDequeue(out var item))
            {
                item.Callback(item.State);
                ran = true;
            }
            return ran;
        }

        public void Settle(SuperViewViewModel superView)
        {
            do
                Assert.That(superView.SoundDecoding.Wait(TimeSpan.FromMinutes(2)), Is.True, "the background work ended");
            while (RunPosted());
        }
    }

    public class SuperViewSoundTests
    {
        const string Decoding = "Decoding sounds...";
        const string ThrotVmd = @"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition";
        const string ThrotTable = @"animations/database/battle/bin/hu17_dlc16_throt.bin";
        const string SndMeta = "animations/derpy/throt_attack_5.snd.meta";

        const uint PersistLeaf = 11, ClipLeaf = 12, SlotLeaf = 13, PersistSndLeaf = 14, Dirt = 21, Stone = 22;

        AssetEditorTestRunner _runner = null!;
        SuperViewViewModel _superView = null!;
        Bank _bank = null!;
        readonly List<TaskCompletionSource<SoundTriggerResolver>> _loads = [];
        readonly List<uint> _played = [], _prepared = [];
        int _stops;
        UiThread _ui = null!;
        SynchronizationContext? _before;

        [SetUp]
        public void Setup()
        {
            _before = SynchronizationContext.Current;
            _ui = new UiThread();
            SynchronizationContext.SetSynchronizationContext(_ui);
            _loads.Clear();
            _played.Clear();
            _prepared.Clear();
            _stops = 0;
            _bank = new Bank();
            _bank.Tag("Derpy_Persist", PersistLeaf);
            _bank.Tag("Derpy_Clip", ClipLeaf);
            _bank.Tag("Derpy_Slot", SlotLeaf);
            _bank.Tag("Derpy_PersistSnd", PersistSndLeaf);

            _runner = new AssetEditorTestRunner();
            _runner.CreateCaContainer();
            _runner.LoadPackFile(PathHelper.GetDataFile("Throt.pack"), true);
            _superView = Open();
            _superView.Load(new AnimationToolInput()
            {
                Mesh = _runner.PackFileService.FindFile(ThrotVmd)!,
                FragmentName = ThrotTable,
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5")
            });
            Assert.That(Selection.FragmentSlotList.SelectedItem?.SlotName, Is.EqualTo("ATTACK_5"), "Throt ATTACK_5 did not load");
            Assert.That(_superView.PersistentMetaEditor.ParsedFile, Is.Not.Null, "Throt's persistent meta");
            Assert.That(_superView.MetaEditor.ParsedFile, Is.Not.Null, "ATTACK_5's meta");
            _stops = 0;
        }

        [TearDown]
        public void RestoreTheContext() => SynchronizationContext.SetSynchronizationContext(_before);

        void Settle() => _ui.Settle(_superView);

        SuperViewViewModel Open()
        {
            var superView = (SuperViewViewModel)_runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.SuperView_Editor);
            superView.LoadSounds = _ =>
            {
                var load = new TaskCompletionSource<SoundTriggerResolver>();
                _loads.Add(load);
                return load.Task;
            };
            superView.PrepareSound = id =>
            {
                _prepared.Add(id);
                return true;
            };
            superView.PlaySound = _played.Add;
            superView.StopSounds = () => _stops++;
            superView.SoundName = id => _bank.Names.GetValueOrDefault(id, id.ToString());
            superView.OpenMixer = () => new SoundTriggerMixer(openDevice: false);
            return superView;
        }

        void Finish(int? load = null)
        {
            _loads[load ?? _loads.Count - 1].SetResult(_bank.Resolver());
            Settle();
        }

        SceneObject Main => _superView.SceneObjects[0].Data;
        BinAnimationViewModel Selection => _superView.SceneObjects[0].FragAndSlotSelection;
        MetaDataFileParser Parser => _runner.GetRequiredServiceInCurrentEditorScope<MetaDataFileParser>();
        SceneObjectEditor SceneEditor => _runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();

        void PointSlotSoundAt(string path, bool replayClip = false)
        {
            Selection.SoundMetaName = path;
            Main.SoundMetaData = _runner.PackFileService.FindFile(path);
            if (replayClip)
                SceneEditor.SetAnimation(Main, Selection.FragmentSlotList.SelectedItem!.AnimationFile);
            SceneEditor.SetMetaFile(Main, Main.MetaData, Main.PersistMetaData);
        }

        void PointPersistentSoundAt(string path)
        {
            Selection.SoundMetaPersistName = path;
            Main.PersistSoundMetaData = _runner.PackFileService.FindFile(path);
            SceneEditor.SetMetaFile(Main, Main.MetaData, Main.PersistMetaData);
        }

        SoundTrigger_v11 Trigger(string soundEvent, float at)
        {
            var tag = (SoundTrigger_v11)Parser.CreateDefault("SOUND_TRIGGER_11");
            (tag.StartTime, tag.EndTime, tag.SoundEvent) = (at, at, soundEvent);
            return tag;
        }

        void AddTag(MetaDataEditorViewModel editor, string soundEvent, float at)
        {
            editor.ParsedFile!.Attributes.Add(Trigger(soundEvent, at));
            editor.UpdateView();
            _ui.RunPosted();
        }

        string WriteSndMeta(string path, string soundEvent, float at)
        {
            var file = new ParsedMetadataFile { Version = 2, Attributes = [Trigger(soundEvent, at)] };
            var pfs = _runner.PackFileService;
            pfs.AddFilesToPack(pfs.GetEditablePack()!, [new NewPackFileEntry(Path.GetDirectoryName(path)!, PackFile.CreateFromBytes(Path.GetFileName(path), Parser.GenerateBytes(2, file)))]);
            return path;
        }

        void AddATriggerToEachFile()
        {
            AddTag(_superView.PersistentMetaEditor, "Derpy_Persist", 0.1f);
            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            PointSlotSoundAt(WriteSndMeta(SndMeta, "Derpy_Slot", 0.3f));
        }

        int TriggersInTheThreeFiles() => new[] { _superView.PersistentMetaEditor.ParsedFile, _superView.MetaEditor.ParsedFile, Main.SoundMetaData is { } sound ? Parser.ParseFile(sound.DataSource.ReadData()) : null }
            .Sum(x => x?.Attributes.Count(a => a is SoundTrigger_v10 or SoundTrigger_v4) ?? 0);

        void PlayTo(float seconds)
        {
            var player = Main.Player;
            Assert.That(player.GetAnimationLengthUs(), Is.GreaterThan(seconds * 1_000_000), "the clip is long enough not to loop");
            player.Pause();
            player.CurrentFrame = 0;
            player.Play();
            while (player.GetTimeUs() < seconds * 1_000_000)
                player.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(20)));
        }

        string[] Status(params string[] then) => [.. _superView.SoundFileProblems, .. then];

        [Test]
        public void SoundOff_ByDefault_AndNothingLoads()
        {
            Assert.That(_superView.SoundOn, Is.False);
            Assert.That(_loads, Is.Empty);
            Assert.That(_superView.SoundStatus, Is.EqualTo(Status()));
        }

        [Test]
        public void SoundOn_ShowsLoading_ThenTheStatus()
        {
            AddATriggerToEachFile();
            var triggers = TriggersInTheThreeFiles();

            _superView.SoundOn = true;

            Assert.That(_superView.SoundStatus, Is.EqualTo(Status("Loading sounds...")));
            Finish();
            var summary = _superView.SoundStatus[_superView.SoundFileProblems.Count];
            Assert.That(summary, Does.StartWith($"{triggers} triggers:"));
            Assert.That(summary, Does.Contain(" 3 play,"), "the three added tags play");
        }

        [Test]
        public void Playing_FiresTagsFromAllThreeFiles()
        {
            AddATriggerToEachFile();
            _superView.SoundOn = true;
            Finish();

            PlayTo(0.35f);

            Assert.That(_played, Is.EqualTo(new[] { PersistLeaf, ClipLeaf, SlotLeaf }));
            Assert.That(_prepared, Is.SupersetOf(new[] { PersistLeaf, ClipLeaf, SlotLeaf }), "decoded ahead of the firing");
        }

        [Test]
        public void Playing_StartsEachSoundTheLeadEarly()
        {
            Assert.That(1.0 - SuperViewViewModel.SoundLeadSeconds, Is.InRange(0.96, 0.98), "the lead puts the tag's due time in the frame landing at 0.98 s");
            Assert.That(Main.Player.GetAnimationLengthUs(), Is.GreaterThan(1_000_000), "the clip is long enough not to loop");
            AddTag(_superView.MetaEditor, "Derpy_Clip", 1.0f);
            _superView.SoundOn = true;
            Finish();

            PlayTo(0.96f);
            var before = _played.ToList();
            Main.Player.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(20)));
            var atTheLead = _played.ToList();
            Main.Player.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(20)));

            Assert.Multiple(() =>
            {
                Assert.That(before, Is.Empty);
                Assert.That(atTheLead, Is.EqualTo(new[] { ClipLeaf }), "fired by the frame crossing 0.974 s");
                Assert.That(_played, Is.EqualTo(new[] { ClipLeaf }), "not again at 1.0 s");
            });
        }

        void Frames(int count)
        {
            for (var i = 0; i < count; i++)
                Main.Player.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(20)));
        }

        [Test]
        public void Looping_FiresTheTagsAtBothEnds_OncePerPass()
        {
            var player = Main.Player;
            var length = player.GetAnimationLengthUs() / 1_000_000d;
            var frame = length / player.FrameCount();
            var end = (float)length;
            if (end > length)
                end = MathF.BitDecrement(end);
            AddTag(_superView.MetaEditor, "Derpy_Persist", 0f);
            AddTag(_superView.MetaEditor, "Derpy_Clip", (float)(length - frame / 2));
            AddTag(_superView.MetaEditor, "Derpy_Slot", end);
            _superView.SoundOn = true;
            Finish();
            var loops = 0;
            _superView.Player.Looping += () => loops++;
            Assert.That(_superView.Player.LoopAnimation.Value, Is.True, "Loop is on");

            _superView.Player.SetAnimationNextFrame();
            _superView.Player.SetAnimationFirstFrame();
            _superView.Player.ToggleAnimationPausePlay();
            for (var i = 0; loops < 3 && i < 10_000; i++)
                Frames(1);

            Assert.That(loops, Is.EqualTo(3));
            Assert.That(_played.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count()),
                Is.EqualTo(new Dictionary<uint, int> { [PersistLeaf] = 4, [ClipLeaf] = 3, [SlotLeaf] = 3 }));
        }

        [Test]
        public void FirstFrameThenPlay_AndPlayAtTheEnd_FireATagAtZeroOnce()
        {
            AddTag(_superView.MetaEditor, "Derpy_Persist", 0f);
            _superView.SoundOn = true;
            Finish();
            PlayTo(1.0f);
            _played.Clear();

            _superView.Player.SetAnimationFirstFrame();
            _superView.Player.ToggleAnimationPausePlay();
            Frames(10);
            var firstFrame = _played.ToList();

            _superView.Player.LoopAnimation.Value = false;
            for (var i = 0; Main.Player.IsPlaying && i < 10_000; i++)
                Frames(1);
            _played.Clear();
            _superView.Player.ToggleAnimationPausePlay();
            Frames(10);

            Assert.Multiple(() =>
            {
                Assert.That(firstFrame, Is.EqualTo(new[] { PersistLeaf }), "First Frame, then Play");
                Assert.That(_played, Is.EqualTo(new[] { PersistLeaf }), "Play at the end");
            });
        }

        [Test]
        public void LoopOff_ATagAtZeroSoundsOnceAtEachPlay_AndNotAtTheEnd()
        {
            AddTag(_superView.MetaEditor, "Derpy_Persist", 0f);
            _superView.SoundOn = true;
            Finish();
            _superView.Player.LoopAnimation.Value = false;

            _superView.Player.SetAnimationNextFrame();
            _superView.Player.SetAnimationFirstFrame();
            _superView.Player.ToggleAnimationPausePlay();
            for (var i = 0; Main.Player.IsPlaying && i < 10_000; i++)
                Frames(1);
            var thePass = _played.ToList();
            _superView.Player.ToggleAnimationPausePlay();
            Frames(10);

            Assert.Multiple(() =>
            {
                Assert.That(Main.Player.GetTimeUs(), Is.LessThan(Main.Player.GetAnimationLengthUs()), "Play at the end started the clip again");
                Assert.That(thePass, Is.EqualTo(new[] { PersistLeaf }), "once, at the start of the pass");
                Assert.That(_played, Is.EqualTo(new[] { PersistLeaf, PersistLeaf }), "and once more at the next Play");
            });
        }

        [Test]
        public void LoopOff_ATagDueInTheStepThatReachesTheEnd_Sounds()
        {
            var lengthUs = Main.Player.GetAnimationLengthUs();
            var leadUs = (long)(SuperViewViewModel.SoundLeadSeconds * 1_000_000);
            var frameMs = Enumerable.Range(30, 20).First(ms => lengthUs - (lengthUs - 1) / (ms * 1000L) * ms * 1000L > leadUs + 2000);
            var end = (float)(lengthUs / 1_000_000d);
            if (end > lengthUs / 1_000_000d)
                end = MathF.BitDecrement(end);
            AddTag(_superView.MetaEditor, "Derpy_Slot", end);
            _superView.SoundOn = true;
            Finish();
            _superView.Player.LoopAnimation.Value = false;

            _superView.Player.SetAnimationNextFrame();
            _superView.Player.SetAnimationFirstFrame();
            _superView.Player.ToggleAnimationPausePlay();
            for (var i = 0; Main.Player.IsPlaying && i < 10_000; i++)
                Main.Player.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(frameMs)));

            Assert.That(Main.Player.GetTimeUs(), Is.EqualTo(lengthUs), "the clip ran out");
            Assert.That(_played, Is.EqualTo(new[] { SlotLeaf }));
        }

        [Test]
        public void ASlotReplay_FiresNothingOfTheLastClip_OnTheFrameItPutsBack()
        {
            var player = Main.Player;
            var frameUs = player.GetAnimationLengthUs() / player.FrameCount();
            var at = 0.30f;
            if ((long)(at * 1_000_000) % frameUs == 0)
                at = 0.32f;
            var restored = (long)(at * 1_000_000) / frameUs * frameUs / 1_000_000d;
            AddTag(_superView.MetaEditor, "Derpy_Clip", (float)(restored + SuperViewViewModel.SoundLeadSeconds - 5e-7));
            _superView.SoundOn = true;
            Finish();
            PlayTo(at);
            Assert.That(_played, Is.EqualTo(new[] { ClipLeaf }), "the tag fires on its own pass");
            _played.Clear();

            PointSlotSoundAt(WriteSndMeta(SndMeta, "Derpy_Slot", 3.0f), replayClip: true);

            Assert.That(player.GetTimeUs() / 1_000_000d, Is.EqualTo(restored).Within(1e-6), "the replay put the clip back on its frame");
            Assert.That(_played, Is.Empty);
        }

        [Test]
        public void Playing_FiresThePersistentRowsSndMeta()
        {
            _superView.SoundOn = true;
            Finish();
            PointPersistentSoundAt(WriteSndMeta("animations/derpy/throt_persistent.snd.meta", "Derpy_PersistSnd", 0.1f));
            Settle();

            PlayTo(0.15f);

            Assert.That(_played, Is.EqualTo(new[] { PersistSndLeaf }));
            Assert.That(_superView.SoundStatus, Has.None.Contains("persistent .snd.meta"), "it resolves, so no line names it");
        }

        [Test]
        public void AnEditWhileOn_PlaysOnTheNextPass()
        {
            _superView.SoundOn = true;
            Finish();

            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            Settle();
            PlayTo(0.25f);

            Assert.That(_played, Is.EqualTo(new[] { ClipLeaf }));
        }

        [Test]
        public void DeletingTheOnlyTag_StopsItPlaying()
        {
            var file = _superView.MetaEditor.ParsedFile!;
            file.Attributes.Clear();
            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            _superView.SoundOn = true;
            Finish();

            file.Attributes.Clear();
            _superView.MetaEditor.UpdateView();
            Settle();
            PlayTo(0.25f);

            Assert.That(_played, Is.Empty);
        }

        [Test]
        public void AFieldEditWhileOn_PlaysTheNewEvent()
        {
            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            _superView.SoundOn = true;
            Finish();

            _superView.MetaEditor.Tags.SelectMany(x => x.Variables).Single(x => x.ValueAsString == "Derpy_Clip").ValueAsString = "Derpy_Slot";
            Settle();
            PlayTo(0.25f);

            Assert.That(_played, Is.EqualTo(new[] { SlotLeaf }));
        }

        [Test]
        public void APickedAnimation_DropsTheSlotsSndMeta()
        {
            var clip = _runner.PackFileService.FindFile(Selection.FragmentSlotList.SelectedItem!.AnimationFile)!;
            PointSlotSoundAt(WriteSndMeta(SndMeta, "Derpy_Slot", 0.3f));
            _superView.SoundOn = true;
            Finish();

            Selection.SetAnimationFile(clip);
            Settle();
            PlayTo(0.35f);

            Assert.That(_played, Has.No.Member(SlotLeaf));
            Assert.That(Selection.SoundMetaName, Is.Null);
        }

        [Test]
        public void ANewMesh_DropsTheSndMetas()
        {
            PointSlotSoundAt(WriteSndMeta(SndMeta, "Derpy_Slot", 0.3f));
            _superView.SoundOn = true;
            Finish();
            Assert.That(_superView.SoundStatus[_superView.SoundFileProblems.Count], Does.StartWith("1 trigger:"));

            SceneEditor.SetMesh(Main, _runner.PackFileService.FindFile(ThrotVmd)!);
            Settle();

            Assert.That(_superView.SoundStatus[_superView.SoundFileProblems.Count], Does.StartWith("0 triggers"));
        }

        [Test]
        public void Decoding_ShowsUntilTheBackgroundWorkEnds()
        {
            _superView.SoundOn = true;
            Finish();
            Assert.That(_superView.SoundStatus, Has.None.EqualTo(Decoding));

            using var hold = new ManualResetEventSlim();
            _bank.Hold = hold;
            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            _ui.RunPosted();
            Assert.That((_superView.SoundDecoding.IsCompleted, _superView.SoundStatus[^1]), Is.EqualTo((false, Decoding)));

            hold.Set();
            Settle();
            Assert.That(_superView.SoundStatus, Has.None.EqualTo(Decoding));
            Assert.That(_superView.SoundStatus[_superView.SoundFileProblems.Count], Does.Contain(" 1 plays,"), "the added tag is in the status");
        }

        [Test]
        public void ANewSlot_PlaysNoneOfTheLastClipsTriggers_UntilItsRebuildLands()
        {
            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            _superView.SoundOn = true;
            Finish();
            PlayTo(0.25f);
            Assert.That(_played, Is.EqualTo(new[] { ClipLeaf }), "the tag fires on its own clip");
            _played.Clear();

            var slots = _superView.SceneObjects[0].FragAndSlotSelection.FragmentSlotList;
            slots.SelectedItem = slots.PossibleValues.First(x => x.SlotName.StartsWith("ATTACK_") && x.SlotName != "ATTACK_5" && _runner.PackFileService.FindFile(x.AnimationFile) != null);
            PlayTo(0.25f);

            Assert.That(_played, Is.Empty, "the last clip's trigger did not fire on this one");
            Assert.That(_superView.SoundStatus, Is.EqualTo(Status(Decoding)), "the last clip's summary is gone");
            Settle();
            PlayTo(0.25f);
            Assert.That(_played, Is.Empty, "and the new clip's own triggers do not have it");
        }

        [Test]
        public void AStaleWarmUp_StopsAtItsNextTag()
        {
            AddATriggerToEachFile();
            _superView.SoundOn = true;
            Finish();
            using var hold = new ManualResetEventSlim();
            _bank.Hold = hold;
            _bank.OffThread.Clear();

            _superView.SoundOn = false;
            _superView.SoundOn = true;
            _superView.SoundOn = false;
            _superView.SoundOn = true;
            hold.Set();
            Settle();

            var lastTag = WwiseHash.Compute("Derpy_Slot_Event");
            Assert.That(_bank.OffThread.Count(x => x == lastTag), Is.EqualTo(1), "only the latest warm-up reached the last tag");
        }

        [Test]
        public void AJobThatThrows_EndsCleanly_AndSaysWhy()
        {
            _superView.SoundOn = true;
            Finish();
            _bank.Throw = new InvalidOperationException("the bank is gone");

            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            Settle();

            Assert.That(_superView.SoundDecoding.IsFaulted, Is.False);
            Assert.That(_superView.SoundStatus, Is.EqualTo(Status("Sounds could not be prepared: the bank is gone")));
            _bank.Throw = null;
            _superView.SoundOn = false;
            _superView.SoundOn = true;
            Settle();
            Assert.That(_superView.SoundStatus, Has.None.StartsWith("Sounds could not be prepared"));
        }

        [Test]
        public void TheFirstWarmUpsFailure_ReplacesLoadingSounds()
        {
            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            _bank.Throw = new InvalidOperationException("the bank is gone");

            _superView.SoundOn = true;
            Finish();

            Assert.That(_superView.SoundStatus, Is.EqualTo(Status("Sounds could not be prepared: the bank is gone")));
        }

        [Test]
        public void AStaleJobsFailure_IsDropped()
        {
            AddTag(_superView.MetaEditor, "Derpy_Clip", 0.2f);
            _superView.SoundOn = true;
            Finish();
            using var hold = new ManualResetEventSlim();
            _bank.Hold = hold;
            _bank.Throw = new InvalidOperationException("the bank is gone");

            AddTag(_superView.MetaEditor, "Derpy_Slot", 0.3f);
            Assert.That(SpinWait.SpinUntil(() => _bank.Held > 0, TimeSpan.FromSeconds(30)), Is.True, "the first job is inside a lookup");
            _bank.Hold = null;
            AddTag(_superView.MetaEditor, "Derpy_Persist", 0.1f);
            hold.Set();
            Settle();

            Assert.That(_superView.SoundStatus, Has.None.StartsWith("Sounds could not be prepared"));
            Assert.That(_superView.SoundStatus[_superView.SoundFileProblems.Count], Does.Contain(" 3 play,"), "the later rebuild landed");
        }

        [Test]
        public void ANewSlot_HidesTheLastClipsPickers_UntilItsRebuildLands()
        {
            _bank.Rows["Derpy_Step"] = new AudioTagEvents(_bank.Event("Derpy_Step_Event", _bank.Switch("Surface", "Dirt", ("Dirt", _bank.Sound(Dirt)), ("Stone", _bank.Sound(Stone)))), "");
            AddTag(_superView.MetaEditor, "Derpy_Step", 0.1f);
            _superView.SoundOn = true;
            Finish();
            Assert.That(_superView.SoundSwitchPickers, Has.Count.EqualTo(1));

            var slots = _superView.SceneObjects[0].FragAndSlotSelection.FragmentSlotList;
            slots.SelectedItem = slots.PossibleValues.First(x => x.SlotName.StartsWith("ATTACK_") && x.SlotName != "ATTACK_5" && _runner.PackFileService.FindFile(x.AnimationFile) != null);

            Assert.That(_superView.SoundSwitchPickers, Is.Empty, "while the rebuild is on its way");
            Settle();
            Assert.That(_superView.SoundSwitchPickers, Is.Empty, "the new clip's triggers reach no switch");
        }

        [Test]
        public void SoundOffDuringLoad_PlaysNothing()
        {
            AddATriggerToEachFile();
            _superView.SoundOn = true;
            _superView.SoundOn = false;
            Finish(0);
            _superView.SoundOn = true;

            PlayTo(0.35f);

            Assert.That(_played, Is.Empty);
            Assert.That((_loads.Count, _superView.SoundStatus), Is.EqualTo((2, Status("Loading sounds..."))), "the stale load built no session");

            _superView.Close();
            Assert.DoesNotThrow(() => Finish(1));
            PlayTo(0.35f);
            Assert.That(_played, Is.Empty);
        }

        [Test]
        public void SlotChange_StopsSounds()
        {
            _superView.SoundOn = true;
            Finish();
            var slots = _superView.SceneObjects[0].FragAndSlotSelection.FragmentSlotList;

            slots.SelectedItem = slots.PossibleValues.First(x => x.SlotName != "ATTACK_5" && _runner.PackFileService.FindFile(x.AnimationFile) != null);
            Assert.That(_stops, Is.EqualTo(1));

            _superView.SoundOn = false;
            Assert.That(_stops, Is.EqualTo(2));
        }

        [Test]
        public void Picker_ChangesWhatPlays()
        {
            _bank.Rows["Derpy_Step"] = new AudioTagEvents(_bank.Event("Derpy_Step_Event", _bank.Switch("Surface", "Dirt", ("Dirt", _bank.Sound(Dirt)), ("Stone", _bank.Sound(Stone)))), "");
            AddTag(_superView.MetaEditor, "Derpy_Step", 0.1f);
            _superView.SoundOn = true;
            Finish();

            var picker = _superView.SoundSwitchPickers.Single();
            Assert.That((picker.GroupName, picker.Options.Select(x => x.ToString()), picker.Selected.Name),
                Is.EqualTo(("Surface", new[] { "Dirt (default)", "Stone" }, "Dirt")));
            PlayTo(0.15f);
            Assert.That(_played, Is.EqualTo(new[] { Dirt }));

            picker.Selected = picker.Options.Single(x => x.Name == "Stone");
            Settle();
            PlayTo(0.15f);

            Assert.That(_played, Is.EqualTo(new[] { Dirt, Stone }));
            Assert.That(_superView.SoundSwitchPickers.Single().Selected.Name, Is.EqualTo("Stone"), "the picker keeps the choice");
        }

        [Test]
        public void PickingASlot_PlaysTheSndMetaItNames()
        {
            var attack = Selection.FragmentSlotList.SelectedItem!;
            var persist = Selection.FragmentSlotList.PossibleValues.First(x => x.SlotName == "PERSISTENT_METADATA_ALIVE");
            Assert.That((Selection.SoundMetaName, Selection.SoundMetaPersistName), Is.EqualTo((attack.SoundFile, persist.SoundFile)));
            Assert.That(_superView.SoundFileProblems, Does.Contain($"Slot .snd.meta {attack.SoundFile} is in no loaded pack."));
            WriteSndMeta(attack.SoundFile, "Derpy_Slot", 0.3f);
            WriteSndMeta(persist.SoundFile, "Derpy_PersistSnd", 0.1f);

            Selection.FragmentSlotList.SelectedItem = Selection.FragmentSlotList.PossibleValues.First(x => x != attack && x != persist);
            Selection.FragmentSlotList.SelectedItem = attack;
            _superView.SoundOn = true;
            Finish();
            PlayTo(0.35f);

            Assert.That(_played, Is.EqualTo(new[] { PersistSndLeaf, SlotLeaf }));
            Assert.That(_superView.SoundFileProblems, Is.Empty);
        }

        [Test]
        public void NoAudioDevice_SaysSo_AndSoundStaysOff()
        {
            _superView.OpenMixer = () => throw new InvalidOperationException("no device here");

            _superView.SoundOn = true;

            Assert.That((_superView.SoundOn, _loads.Count), Is.EqualTo((false, 0)));
            Assert.That(_superView.SoundStatus, Is.EqualTo(Status("No audio device could be opened: no device here")));
        }

        [Test]
        public void ADeviceThatStops_SaysSo_AndSoundGoesOff()
        {
            var devices = new List<Mock<IWavePlayer>>();
            _superView.OpenMixer = () => new SoundTriggerMixer([() =>
            {
                devices.Add(new Mock<IWavePlayer>());
                return devices[^1].Object;
            }]);
            _superView.SoundOn = true;
            Finish();

            devices[0].Raise(x => x.PlaybackStopped += null, new StoppedEventArgs(new InvalidOperationException("the endpoint went away")));
            Settle();

            Assert.That(_superView.SoundOn, Is.False);
            Assert.That(_superView.SoundStatus, Is.EqualTo(Status("The sound device stopped: the endpoint went away")));
            devices[0].Verify(x => x.Dispose(), Times.Once);

            _superView.SoundOn = true;
            Assert.That((devices.Count, _superView.SoundOn), Is.EqualTo((2, true)), "a new device");

            devices[0].Raise(x => x.PlaybackStopped += null, new StoppedEventArgs(new InvalidOperationException("late")));
            Settle();
            Assert.That(_superView.SoundOn, Is.True, "the device let go of is no longer heard from");
        }

        [Test]
        public void Status_ShowsFileProblems_AlwaysAndTheTriggers_OnlyWithSoundOn()
        {
            const string Missing = "animations/derpy/nowhere.snd.meta";
            PointSlotSoundAt(Missing);
            Assert.That(_superView.SoundFileProblems, Does.Contain($"Slot .snd.meta {Missing} is in no loaded pack."));
            Assert.That(_superView.SoundStatus, Is.EqualTo(Status()));

            _superView.SoundOn = true;
            Assert.That(_superView.SoundStatus, Is.EqualTo(Status("Loading sounds...")));
            Finish();
            var problems = _superView.SoundFileProblems.Count;
            Assert.That(_superView.SoundStatus.Take(problems), Is.EqualTo(Status()));
            Assert.That(_superView.SoundStatus[problems], Does.Match("^[0-9]+ triggers?"));

            _superView.SoundOn = false;
            Assert.That(_superView.SoundStatus, Is.EqualTo(Status()));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void TheSoundStrip_IsBound()
        {
            _bank.Rows["Derpy_Step"] = new AudioTagEvents(_bank.Event("Derpy_Step_Event", _bank.Switch("Surface", "Dirt", ("Dirt", _bank.Sound(Dirt)), ("Stone", _bank.Sound(Stone)))), "");
            AddTag(_superView.MetaEditor, "Derpy_Step", 0.1f);
            var panel = new EditorView { DataContext = _superView };
            Lay(panel);

            Descendants(panel).OfType<CheckBox>().Single(x => AutomationProperties.GetAutomationId(x) == "SoundOn").IsChecked = true;
            Assert.That(_superView.SoundOn, Is.True);
            Finish();
            Lay(panel);
            Assert.That(Descendants(panel).OfType<TextBlock>().Select(x => x.Text), Is.SupersetOf(_superView.SoundStatus));
            var combo = Descendants(panel).OfType<ComboBox>().Single(x => AutomationProperties.GetAutomationId(x) == "SoundSwitch");
            combo.SelectedIndex = 1;
            Assert.That(_superView.SoundSwitchPickers.Single().Selected.Name, Is.EqualTo("Stone"));
            var picker = _superView.SoundSwitchPickers.Single();
            _superView.SoundOn = false;
            Lay(panel);
            Assert.That((_superView.SoundSwitchPickers.Count, picker.Selected?.Name), Is.EqualTo((0, "Stone")));
        }

        static void Lay(FrameworkElement element)
        {
            element.Measure(new Size(1400, 900));
            element.Arrange(new Rect(0, 0, 1400, 900));
            element.UpdateLayout();
        }

        static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                yield return child;
                foreach (var below in Descendants(child))
                    yield return below;
            }
        }

        sealed class Bank
        {
            uint _next = 1_000_000;

            public Dictionary<uint, HircItem> Items { get; } = [];
            public Dictionary<string, AudioTagEvents> Rows { get; } = new(StringComparer.Ordinal);
            public Dictionary<uint, string> Names { get; } = [];

            readonly int _thread = Environment.CurrentManagedThreadId;

            public ManualResetEventSlim? Hold { get; set; }

            public int Held => Volatile.Read(ref _held);
            int _held;

            public Exception? Throw { get => _throw; set => _throw = value; }
            Exception? _throw;

            public ConcurrentQueue<uint> OffThread { get; } = new();

            public SoundTriggerResolver Resolver() => new(tag => Rows.GetValueOrDefault(tag), id =>
            {
                if (Environment.CurrentManagedThreadId != _thread)
                {
                    if (Hold is { } hold)
                    {
                        Interlocked.Increment(ref _held);
                        hold.Wait(TimeSpan.FromSeconds(30));
                    }
                    if (Interlocked.Exchange(ref _throw, null) is { } thrown)
                        throw thrown;
                    OffThread.Enqueue(id);
                }
                return Items.TryGetValue(id, out var item) ? [item] : [];
            }, _ => true);

            uint Hash(string name)
            {
                var id = WwiseHash.Compute(name);
                Names[id] = name;
                return id;
            }

            public void Tag(string tag, uint leaf) => Rows[tag] = new AudioTagEvents(Event(tag + "_Event", Sound(leaf)), "");

            public string Event(string name, uint target)
            {
                var action = _next++;
                Items[action] = new CAkAction_V136 { Id = action, HircType = AkBkHircType.Action, ActionType = AkActionType.Play, IdExt = target };
                var id = Hash(name);
                Items[id] = new CAkEvent_V136 { Id = id, HircType = AkBkHircType.Event, Actions = [new CAkEvent_V136.Action_V136 { ActionId = action }] };
                return name;
            }

            public uint Sound(uint sourceId)
            {
                var id = _next++;
                Items[id] = new CAkSound_V136
                {
                    Id = id,
                    HircType = AkBkHircType.Sound,
                    AkBankSourceData = new AkBankSourceData_V136 { AkMediaInformation = new AkBankSourceData_V136.AkMediaInformation_V136 { SourceId = sourceId } },
                };
                return id;
            }

            public uint Switch(string group, string defaultCase, params (string Case, uint Child)[] cases)
            {
                var id = _next++;
                Items[id] = new CAkSwitchCntr_V136
                {
                    Id = id,
                    HircType = AkBkHircType.SwitchContainer,
                    GroupId = Hash(group),
                    DefaultSwitch = Hash(defaultCase),
                    SwitchList = cases
                        .Select(x => (ICAkSwitchCntr.ICAkSwitchPackage)new CAkSwitchCntr_V136.CAkSwitchPackage_V136 { SwitchId = Hash(x.Case), NodeIdList = [x.Child] })
                        .ToList(),
                };
                return id;
            }
        }
    }
}
