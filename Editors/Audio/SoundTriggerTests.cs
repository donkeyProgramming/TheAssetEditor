using Editors.Audio.SoundTriggers;
using Moq;
using NAudio.Wave;
using Shared.GameFormats.DB;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using Shared.GameFormats.Wwise.Wem.V132;
using Shared.GameFormats.Wwise.Wem.V132.Encoding;

namespace Test.Audio
{
    [TestFixture]
    internal class SoundTriggerTests
    {
        static DbRow Tag(string key, string battleStart, string campaignStart = "") => new(
            new Dictionary<string, object>
            {
                ["key"] = key,
                ["sound_event_battle_start"] = battleStart,
                ["sound_event_campaign_start"] = campaignStart,
            }, 22, "test_pack", "db/audio_metadata_tags_tables/test");

        [Test]
        public void TagTable_FirstRowWins_ExactCase()
        {
            var tags = AudioMetadataTags.From([Tag("Drop_Weapon_Rest", "A", "CA"), Tag("Drop_Weapon_Rest", "B", "CB"), Tag("drop_weapon_rest", "C", "CC")]);

            Assert.Multiple(() =>
            {
                Assert.That(tags.Find("Drop_Weapon_Rest")!.BattleStart, Is.EqualTo("A"));
                Assert.That(tags.Find("Drop_Weapon_Rest")!.CampaignStart, Is.EqualTo("CA"));
                Assert.That(tags.Find("drop_weapon_rest"), Is.EqualTo(new AudioTagEvents("C", "CC")));
                Assert.That(tags.Find("DROP_WEAPON_REST"), Is.Null);
                Assert.That(tags.Count, Is.EqualTo(2));
            });
        }

        sealed class Bank
        {
            const uint SoundIdOffset = 10000;
            uint _next = 1000000;

            public Dictionary<uint, HircItem> Items { get; } = [];
            public HashSet<uint> Wems { get; } = [];
            public Dictionary<string, AudioTagEvents> Rows { get; } = new(StringComparer.Ordinal);

            public List<HircItem> SameIds { get; } = [];

            public SoundTriggerResolver Resolver() => new(
                tag => Rows.GetValueOrDefault(tag),
                id => [.. Items.TryGetValue(id, out var item) ? [item] : Array.Empty<HircItem>(), .. SameIds.Where(x => x.Id == id)],
                Wems.Contains);

            public Bank Row(string tag, string battleEvent, string campaignEvent = "")
            {
                Rows[tag] = new AudioTagEvents(battleEvent, campaignEvent);
                return this;
            }

            public uint Sound(uint sourceId, bool wemExists = true)
            {
                var id = sourceId + SoundIdOffset;
                Items[id] = new CAkSound_V136
                {
                    Id = id,
                    HircType = AkBkHircType.Sound,
                    AkBankSourceData = new AkBankSourceData_V136 { AkMediaInformation = new AkBankSourceData_V136.AkMediaInformation_V136 { SourceId = sourceId } },
                };
                if (wemExists)
                    Wems.Add(sourceId);
                return id;
            }

            public uint Event(string name, params uint[] actionIds)
            {
                var id = WwiseHash.Compute(name);
                Items[id] = new CAkEvent_V136
                {
                    Id = id,
                    HircType = AkBkHircType.Event,
                    Actions = actionIds.Select(x => new CAkEvent_V136.Action_V136 { ActionId = x }).ToList(),
                };
                return id;
            }

            public uint Action(AkActionType type, uint targetId)
            {
                var id = _next++;
                Items[id] = new CAkAction_V136 { Id = id, HircType = AkBkHircType.Action, ActionType = type, IdExt = targetId };
                return id;
            }

            public uint Play(uint targetId) => Action(AkActionType.Play, targetId);

            public uint Random(params uint[] children)
            {
                var id = _next++;
                Items[id] = new CAkRanSeqCntr_V136
                {
                    Id = id,
                    HircType = AkBkHircType.RandomSequenceContainer,
                    CAkPlayList = new CAkRanSeqCntr_V136.CAkPlayList_V136
                    {
                        Playlist = children.Select(x => new CAkRanSeqCntr_V136.CAkPlayList_V136.AkPlaylistItem_V136 { PlayId = x, Weight = 50000 }).ToList(),
                    },
                };
                return id;
            }

            public uint Container(byte mode, byte randomMode, ushort avoidRepeat, params (uint Child, int Weight)[] playlist)
            {
                var id = _next++;
                Items[id] = new CAkRanSeqCntr_V136
                {
                    Id = id,
                    HircType = AkBkHircType.RandomSequenceContainer,
                    Mode = mode,
                    RandomMode = randomMode,
                    AvoidRepeatCount = avoidRepeat,
                    BitVector = 0x12,
                    CAkPlayList = new CAkRanSeqCntr_V136.CAkPlayList_V136
                    {
                        Playlist = playlist.Select(x => new CAkRanSeqCntr_V136.CAkPlayList_V136.AkPlaylistItem_V136 { PlayId = x.Child, Weight = x.Weight }).ToList(),
                    },
                };
                return id;
            }

            public uint Blend(params uint[] children)
            {
                var id = _next++;
                Items[id] = new CAkLayerCntr_V136 { Id = id, HircType = AkBkHircType.LayerContainer, Children = new Children_V136 { ChildIds = [.. children] } };
                return id;
            }

            public uint Mixer(params uint[] children)
            {
                var id = _next++;
                Items[id] = new CAkActorMixer_V136 { Id = id, HircType = AkBkHircType.ActorMixer, Children = new Children_V136 { ChildIds = [.. children] } };
                return id;
            }

            public uint Switch(string group, string defaultCase, params (string Case, uint Child)[] cases)
            {
                var id = _next++;
                Items[id] = new CAkSwitchCntr_V136
                {
                    Id = id,
                    HircType = AkBkHircType.SwitchContainer,
                    GroupId = WwiseHash.Compute(group),
                    DefaultSwitch = WwiseHash.Compute(defaultCase),
                    SwitchList = cases
                        .Select(x => (ICAkSwitchCntr.ICAkSwitchPackage)new CAkSwitchCntr_V136.CAkSwitchPackage_V136 { SwitchId = WwiseHash.Compute(x.Case), NodeIdList = [x.Child] })
                        .ToList(),
                };
                return id;
            }
        }

        static readonly IReadOnlyDictionary<uint, uint> NoChoices = new Dictionary<uint, uint>();
        static uint Hash(string name) => WwiseHash.Compute(name);

        static SoundTriggerResult Resolve(Bank bank, string tag = "Tag", bool campaign = false, IReadOnlyDictionary<uint, uint>? choices = null) =>
            bank.Resolver().Resolve(tag, campaign, choices ?? NoChoices);

        static uint[] Leaves(SoundTriggerResult result) => SoundNodes.Leaves(result.Sound!).ToArray();

        [Test]
        public void Resolve_UsesTheTableRow_NotTheTagName()
        {
            var bank = new Bank().Row("Drop_Weapon_Rest", "Battle_Individual_Melee_Weapon_Drop_Weapon_Rest");
            bank.Event("Battle_Individual_Melee_Weapon_Drop_Weapon_Rest", bank.Play(bank.Sound(101)));
            bank.Event("Battle_Individual_Drop_Weapon_Rest", bank.Play(bank.Sound(999)));

            var result = Resolve(bank, "Drop_Weapon_Rest");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tag, Is.EqualTo("Drop_Weapon_Rest"));
                Assert.That(result.Silent, Is.Null);
                Assert.That(Leaves(result), Is.EqualTo(new uint[] { 101 }));
            });
        }

        [Test]
        public void Resolve_NoRow_IsSilent()
        {
            var result = Resolve(new Bank(), "Nothing");

            Assert.Multiple(() =>
            {
                Assert.That(result.Silent, Is.EqualTo("no audio_metadata_tags_tables row"));
                Assert.That(result.Sound, Is.Null);
                Assert.That(result.Switches, Is.Empty);
            });
        }

        [Test]
        public void Resolve_EmptyEvent_AndMissingEvent_AreSilent()
        {
            var bank = new Bank()
                .Row("EmptyBattle", "", "Campaign_Event")
                .Row("EmptyCampaign", "Battle_Event", "  ")
                .Row("Missing", "Battle_Event_Nobody_Loaded");
            bank.Event("Campaign_Event", bank.Play(bank.Sound(1)));

            Assert.Multiple(() =>
            {
                Assert.That(Resolve(bank, "EmptyBattle").Silent, Is.EqualTo("the row names no battle event"));
                Assert.That(Resolve(bank, "EmptyCampaign", campaign: true).Silent, Is.EqualTo("the row names no campaign event"));
                Assert.That(Resolve(bank, "Missing").Silent, Is.EqualTo("event Battle_Event_Nobody_Loaded is in no loaded bank"));
                Assert.That(Resolve(bank, "EmptyBattle").Sound, Is.Null);
            });
        }

        [Test]
        public void Resolve_AnEventWithNoActions_HasNoPlayAction()
        {
            var bank = new Bank().Row("Tag", "Empty_Event");
            bank.Event("Empty_Event");

            Assert.That(Resolve(bank).Silent, Is.EqualTo("event Empty_Event has no Play action"));
        }

        [TestCase(AkActionType.PlayAndContinue, "PlayAndContinue")]
        [TestCase(AkActionType.PlayEvent, "PlayEvent")]
        [TestCase(AkActionType.Trigger, "Trigger")]
        [TestCase(AkActionType.Trigger_O, "Trigger_O")]
        [TestCase(AkActionType.Trigger_E, "Trigger_E")]
        [TestCase(AkActionType.Trigger_E_O, "Trigger_E_O")]
        [TestCase((AkActionType)0x2303, "0x2303")]
        [TestCase(AkActionType.Stop_E_O, null)]
        [TestCase(AkActionType.StopEvent, null)]
        [TestCase(AkActionType.Pause_E_O, null)]
        [TestCase(AkActionType.Resume_E_O, null)]
        [TestCase(AkActionType.Seek_E_O, null)]
        [TestCase(AkActionType.Release, null)]
        [TestCase(AkActionType.Break_E_O, null)]
        [TestCase(AkActionType.SetVolume_O, null)]
        [TestCase(AkActionType.ResetPitch_O, null)]
        [TestCase(AkActionType.Mute_O, null)]
        public void Resolve_OnlyActionsThatCanStartASound_AreNotSupported(AkActionType type, string? named)
        {
            var bank = new Bank().Row("Tag", "Event_A");
            bank.Event("Event_A", bank.Action(type, bank.Sound(7)));

            Assert.That(Resolve(bank).Silent, Is.EqualTo(named == null ? "event Event_A has no Play action" : $"not supported: {named} action -> Sound"));
        }

        [Test]
        public void Resolve_AStopBesideAPlay_LeavesThePlaysProblemAsTheReason()
        {
            var bank = new Bank().Row("Tag", "Event_A");
            bank.Event("Event_A", bank.Action(AkActionType.Stop_E_O, bank.Sound(7)), bank.Play(bank.Sound(8, wemExists: false)));

            Assert.That(Resolve(bank).Silent, Is.EqualTo("WEM not found: 8.wem"));
        }

        [Test]
        public void Resolve_MusicObjectAndMissingWem_TheFirstMetIsTheReason()
        {
            var bank = new Bank().Row("WemFirst", "Event_A").Row("MusicFirst", "Event_B");
            var segment = new CAkMusicSegment_V136 { Id = 5, HircType = AkBkHircType.Music_Segment };
            bank.Items[segment.Id] = segment;
            var missing = bank.Sound(8, wemExists: false);
            bank.Event("Event_A", bank.Play(bank.Random(missing, segment.Id)));
            bank.Event("Event_B", bank.Play(bank.Random(segment.Id, missing)));

            Assert.Multiple(() =>
            {
                Assert.That(Resolve(bank, "WemFirst").Silent, Is.EqualTo("WEM not found: 8.wem"));
                Assert.That(Resolve(bank, "MusicFirst").Silent, Is.EqualTo("not supported: Music_Segment"));
            });
        }

        [Test]
        public void Resolve_UnknownActionToAnEvent_IsNotSupported_AndNamesTheDialogueEvent()
        {
            const AkActionType Unknown = (AkActionType)0x2303;
            Assert.That(Enum.IsDefined(Unknown), Is.False, "0x2303 has no name in the enum");
            var bank = new Bank().Row("Hit", "Hit_Event");
            const uint Shared = 77;
            bank.Items[Shared] = new CAkEvent_V136 { Id = Shared, HircType = AkBkHircType.Event, Actions = [new CAkEvent_V136.Action_V136 { ActionId = bank.Action(Unknown, Shared) }] };
            bank.SameIds.Add(new CAkDialogueEvent_V136 { Id = Shared, HircType = AkBkHircType.Dialogue_Event });
            bank.Event("Hit_Event", bank.Action(Unknown, Shared));

            var result = Resolve(bank, "Hit");

            Assert.Multiple(() =>
            {
                Assert.That(result.Sound, Is.Null);
                Assert.That(result.Silent, Is.EqualTo("not supported: 0x2303 action -> Event -> 0x2303 action -> Dialogue_Event"));
            });
        }

        [Test]
        public void Resolve_ActionInNoLoadedBank_SaysSo()
        {
            var bank = new Bank().Row("Tag", "Event_A");
            bank.Event("Event_A", 404);

            Assert.That(Resolve(bank).Silent, Is.EqualTo("action 404 of event Event_A is in no loaded bank"));
        }

        [Test]
        public void Resolve_PlayActionWhoseTargetIsNotLoaded_IsDropped()
        {
            var bank = new Bank().Row("Tag", "Event_A").Row("OnlyBad", "Event_B");
            bank.Event("Event_A", bank.Play(404), bank.Play(bank.Sound(11)));
            bank.Event("Event_B", bank.Play(404));

            Assert.Multiple(() =>
            {
                Assert.That(Leaves(Resolve(bank, "Tag")), Is.EqualTo(new uint[] { 11 }));
                Assert.That(Resolve(bank, "OnlyBad").Silent, Is.EqualTo("event Event_B plays nothing found in the loaded banks"));
            });
        }

        [Test]
        public void Resolve_PlayActionsThatReachNothing_AreNotTheSameAsNoPlayAction()
        {
            var bank = new Bank().Row("EmptyContainer", "Event_C").Row("StopAndUnloaded", "Event_D").Row("WemWins", "Event_E");
            bank.Event("Event_C", bank.Play(bank.Random()));
            bank.Event("Event_D", bank.Action(AkActionType.Stop_E_O, bank.Sound(7)), bank.Play(404));
            bank.Event("Event_E", bank.Play(404), bank.Play(bank.Sound(8, wemExists: false)));

            Assert.Multiple(() =>
            {
                Assert.That(Resolve(bank, "EmptyContainer").Silent, Is.EqualTo("event Event_C plays nothing found in the loaded banks"));
                Assert.That(Resolve(bank, "StopAndUnloaded").Silent, Is.EqualTo("event Event_D plays nothing found in the loaded banks"), "a Stop cannot start a sound");
                Assert.That(Resolve(bank, "WemWins").Silent, Is.EqualTo("WEM not found: 8.wem"));
            });
        }

        [Test]
        public void Resolve_SeveralPlayActions_PlayTogether()
        {
            var bank = new Bank().Row("Tag", "Event_A");
            bank.Event("Event_A", bank.Play(bank.Sound(21)), bank.Action(AkActionType.Stop_E_O, bank.Sound(99)), bank.Play(bank.Sound(22)));

            var result = Resolve(bank);

            Assert.Multiple(() =>
            {
                Assert.That(result.Sound, Is.InstanceOf<SoundAll>());
                Assert.That(Leaves(result), Is.EqualTo(new uint[] { 21, 22 }));
                Assert.That(SoundNodes.Pick(result.Sound!, new Random(1)), Is.EqualTo(new uint[] { 21, 22 }));
            });
        }

        [Test]
        public void Resolve_EventIdThatIsNotAnEvent_AndMusicObject_AreNotSupported()
        {
            var bank = new Bank().Row("NotAnEvent", "Mixer_Name").Row("Music", "Music_Event");
            var mixerId = Hash("Mixer_Name");
            bank.Items[mixerId] = new CAkActorMixer_V136 { Id = mixerId, HircType = AkBkHircType.ActorMixer };
            var segment = new CAkMusicSegment_V136 { Id = 5, HircType = AkBkHircType.Music_Segment };
            bank.Items[segment.Id] = segment;
            bank.Event("Music_Event", bank.Play(segment.Id));

            Assert.Multiple(() =>
            {
                Assert.That(Resolve(bank, "NotAnEvent").Silent, Is.EqualTo("not supported: ActorMixer"));
                Assert.That(Resolve(bank, "Music").Silent, Is.EqualTo("not supported: Music_Segment"));
            });
        }

        [Test]
        public void Resolve_Switch_PlaysDefault_ThenTheChoice()
        {
            var bank = new Bank().Row("Tag", "Step");
            var grass = bank.Sound(201);
            var stone = bank.Sound(202);
            bank.Event("Step", bank.Play(bank.Switch("Surface", "Grass", ("Grass", grass), ("Stone", stone))));

            var byDefault = Resolve(bank);
            var chosen = Resolve(bank, choices: new Dictionary<uint, uint> { [Hash("Surface")] = Hash("Stone") });

            Assert.Multiple(() =>
            {
                Assert.That(Leaves(byDefault), Is.EqualTo(new uint[] { 201 }));
                Assert.That(byDefault.Switches, Has.Count.EqualTo(1));
                Assert.That(byDefault.Switches[0].GroupId, Is.EqualTo(Hash("Surface")));
                Assert.That(byDefault.Switches[0].DefaultSwitch, Is.EqualTo(Hash("Grass")));
                Assert.That(byDefault.Switches[0].Values, Is.EqualTo(new[] { Hash("Grass"), Hash("Stone") }));
                Assert.That(Leaves(chosen), Is.EqualTo(new uint[] { 202 }));
            });
        }

        [Test]
        public void Resolve_Switch_ChoiceNoCaseHas_FallsBackToTheDefault()
        {
            var bank = new Bank().Row("Tag", "Step");
            bank.Event("Step", bank.Play(bank.Switch("Surface", "Stone", ("Grass", bank.Sound(201)), ("Stone", bank.Sound(202)))));

            var result = Resolve(bank, choices: new Dictionary<uint, uint> { [Hash("Surface")] = Hash("Snow") });

            Assert.That(Leaves(result), Is.EqualTo(new uint[] { 202 }));
        }

        [Test]
        public void Resolve_SwitchWithNoDefault_PlaysTheFirstCase()
        {
            var bank = new Bank().Row("Tag", "Step");
            bank.Event("Step", bank.Play(bank.Switch("Surface", "None", ("Grass", bank.Sound(201)), ("Stone", bank.Sound(202)))));

            var result = Resolve(bank);

            Assert.Multiple(() =>
            {
                Assert.That(Leaves(result), Is.EqualTo(new uint[] { 201 }));
                Assert.That(result.Switches.Single().DefaultSwitch, Is.EqualTo(Hash("None")));
            });
        }

        [Test]
        public void Resolve_SameSwitchGroupInTwoContainers_ListsItOnce()
        {
            var bank = new Bank().Row("Tag", "Step");
            var inner = bank.Switch("Surface", "Mud", ("Mud", bank.Sound(301)), ("Ice", bank.Sound(302)));
            var outer = bank.Switch("Surface", "Grass", ("Grass", inner), ("Stone", bank.Sound(303)));
            bank.Event("Step", bank.Play(outer));

            var result = Resolve(bank);

            Assert.Multiple(() =>
            {
                Assert.That(result.Switches, Has.Count.EqualTo(1));
                Assert.That(result.Switches[0].DefaultSwitch, Is.EqualTo(Hash("Grass")));
                Assert.That(result.Switches[0].Values, Is.EqualTo(new[] { Hash("Grass"), Hash("Stone") }));
            });
        }

        [Test]
        public void Resolve_Random_IsOneOf_Blend_IsAll()
        {
            var bank = new Bank().Row("Rand", "Rand_Event").Row("Layer", "Layer_Event").Row("Mix", "Mix_Event");
            bank.Event("Rand_Event", bank.Play(bank.Random(bank.Sound(301), bank.Sound(302))));
            bank.Event("Layer_Event", bank.Play(bank.Blend(bank.Sound(401), bank.Sound(402))));
            bank.Event("Mix_Event", bank.Play(bank.Mixer(bank.Sound(411), bank.Sound(412))));

            var random = Resolve(bank, "Rand");
            var blend = Resolve(bank, "Layer");
            var mixer = Resolve(bank, "Mix");
            var seeded = new Random(7);
            var picks = Enumerable.Range(0, 40).Select(_ => SoundNodes.Pick(random.Sound!, seeded)).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(random.Sound, Is.InstanceOf<SoundOneOf>());
                Assert.That(Leaves(random), Is.EqualTo(new uint[] { 301, 302 }));
                Assert.That(picks.All(x => x.Count == 1), Is.True);
                Assert.That(picks.Select(x => x.Single()).Distinct().Order(), Is.EqualTo(new uint[] { 301, 302 }));
                Assert.That(blend.Sound, Is.InstanceOf<SoundAll>());
                Assert.That(SoundNodes.Pick(blend.Sound!, new Random(7)), Is.EqualTo(new uint[] { 401, 402 }));
                Assert.That(mixer.Sound, Is.InstanceOf<SoundAll>());
                Assert.That(SoundNodes.Pick(mixer.Sound!, new Random(7)), Is.EqualTo(new uint[] { 411, 412 }));
            });
        }

        static List<uint> Picks(Bank bank, int firings, string tag = "Tag")
        {
            var sound = Resolve(bank, tag).Sound!;
            var picker = new SoundPicker(new Random(1));
            return Enumerable.Range(0, firings).Select(_ => picker.Pick(sound).Single()).ToList();
        }

        static (uint, int)[] Equal(params uint[] children) => children.Select(x => (x, 50000)).ToArray();

        [Test]
        public void Sequence_PlaysThePlaylistInOrder_AndStartsOver()
        {
            var bank = new Bank().Row("Tag", "Seq");
            bank.Event("Seq", bank.Play(bank.Container(1, 0, 0, Equal(bank.Sound(1), bank.Sound(2), bank.Sound(3)))));
            Assert.That(Picks(bank, 7), Is.EqualTo(new uint[] { 1, 2, 3, 1, 2, 3, 1 }));
        }

        [Test]
        public void Random_WeighsItsChildren_AndADroppedChildTakesItsWeight()
        {
            var bank = new Bank().Row("Tag", "Weighted").Row("Gap", "Gapped");
            bank.Event("Weighted", bank.Play(bank.Container(0, 0, 0, (bank.Sound(1), 90000), (bank.Sound(2), 10000))));
            bank.Event("Gapped", bank.Play(bank.Container(0, 0, 0, (bank.Sound(3), 10000), (bank.Sound(4, wemExists: false), 90000), (bank.Sound(5), 10000))));
            var weighted = Picks(bank, 2000);
            var gapped = Picks(bank, 2000, "Gap");
            Assert.Multiple(() =>
            {
                Assert.That(weighted.Count(x => x == 1) / 2000.0, Is.EqualTo(0.9).Within(0.03));
                Assert.That(gapped.Count(x => x == 3) / 2000.0, Is.EqualTo(0.5).Within(0.05));
            });
        }

        [TestCase((ushort)1, 2)]
        [TestCase((ushort)2, 3)]
        [TestCase((ushort)5, 3)]
        public void Random_AvoidsRepeatingItsLastPicks(ushort avoid, int window)
        {
            var bank = new Bank().Row("Tag", "Avoid");
            bank.Event("Avoid", bank.Play(bank.Container(0, 0, avoid, Equal(bank.Sound(1), bank.Sound(2), bank.Sound(3)))));
            var picks = Picks(bank, 300);
            Assert.That(Enumerable.Range(0, picks.Count - window + 1).All(i => picks.Skip(i).Take(window).Distinct().Count() == window), Is.True);
        }

        [Test]
        public void Shuffle_PlaysEachChildOnceARound()
        {
            var bank = new Bank().Row("Tag", "Shuffle");
            bank.Event("Shuffle", bank.Play(bank.Container(0, 1, 1, Equal(bank.Sound(1), bank.Sound(2), bank.Sound(3), bank.Sound(4)))));
            var picks = Picks(bank, 400);
            var rounds = picks.Chunk(4).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(rounds.All(x => x.Order().SequenceEqual(new uint[] { 1, 2, 3, 4 })), Is.True);
                Assert.That(rounds.Select(x => string.Join(",", x)).Distinct().Count(), Is.GreaterThan(5), "the order changes");
                Assert.That(Enumerable.Range(1, picks.Count - 1).All(i => picks[i] != picks[i - 1]), Is.True, "no repeat at a round's turn");
            });
        }

        [Test]
        public void Session_SequenceGoesOn_AcrossFiringsAndRebuilds()
        {
            var bank = new Bank().Row("Tag", "Seq");
            bank.Event("Seq", bank.Play(bank.Container(1, 0, 0, Equal(bank.Sound(1), bank.Sound(2), bank.Sound(3)))));
            var run = new SessionRun(bank).Rebuild(Trigger("Tag", 0.5f)).Frames(0, 200);
            run.Rebuild(Trigger("Tag", 0.5f)).Frames(200, 100);
            Assert.That(run.Played, Is.EqualTo(new uint[] { 1, 2, 3 }));
        }

        [Test]
        public void Resolve_NestedContainers_PickOneThenPlayAll()
        {
            var bank = new Bank().Row("Tag", "Nested");
            var oneOf = bank.Random(bank.Sound(1), bank.Sound(2));
            bank.Event("Nested", bank.Play(bank.Blend(bank.Sound(3), oneOf)));

            var result = Resolve(bank);
            var seeded = new Random(3);
            var picks = Enumerable.Range(0, 40).Select(_ => SoundNodes.Pick(result.Sound!, seeded)).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(Leaves(result), Is.EqualTo(new uint[] { 3, 1, 2 }));
                Assert.That(picks.All(x => x.Count == 2 && x[0] == 3), Is.True);
                Assert.That(picks.Select(x => x[1]).Distinct().Order(), Is.EqualTo(new uint[] { 1, 2 }));
            });
        }

        [Test]
        public void Resolve_MissingWems_AreDropped_AllMissing_IsSilent()
        {
            var some = new Bank().Row("Tag", "Event_A");
            some.Event("Event_A", some.Play(some.Random(some.Sound(501), some.Sound(502, wemExists: false))));
            var none = new Bank().Row("Tag", "Event_A");
            none.Event("Event_A", none.Play(none.Random(none.Sound(501, wemExists: false), none.Sound(502, wemExists: false))));

            var partial = Resolve(some);
            var allMissing = Resolve(none);

            Assert.Multiple(() =>
            {
                Assert.That(partial.Silent, Is.Null);
                Assert.That(partial.Sound, Is.EqualTo(new SoundLeaf(501)));
                Assert.That(Leaves(partial), Is.EqualTo(new uint[] { 501 }));
                Assert.That(allMissing.Sound, Is.Null);
                Assert.That(allMissing.Silent, Is.EqualTo("WEM not found: 501.wem"));
            });
        }

        [Test]
        public void Resolve_ContainerThatContainsItself_DoesNotLoopForever()
        {
            var bank = new Bank().Row("Tag", "Loop");
            var mixerId = 777u;
            bank.Items[mixerId] = new CAkActorMixer_V136 { Id = mixerId, HircType = AkBkHircType.ActorMixer, Children = new Children_V136 { ChildIds = [mixerId, bank.Sound(1)] } };
            bank.Event("Loop", bank.Play(mixerId));

            var result = Resolve(bank);

            Assert.That(Leaves(result), Is.EqualTo(new uint[] { 1 }));
        }

        [Test]
        public void Resolve_CampaignFlag_UsesTheCampaignEvent()
        {
            var bank = new Bank().Row("Tag", "Battle_Event", "Campaign_Event");
            bank.Event("Battle_Event", bank.Play(bank.Sound(1)));
            bank.Event("Campaign_Event", bank.Play(bank.Sound(2)));

            Assert.Multiple(() =>
            {
                Assert.That(Leaves(Resolve(bank, campaign: false)), Is.EqualTo(new uint[] { 1 }));
                Assert.That(Leaves(Resolve(bank, campaign: true)), Is.EqualTo(new uint[] { 2 }));
            });
        }

        const double ClipLength = 2.0;

        static (double From, double To, bool Wrapped) StepOf(double previous, double now, double elapsed, double speed = 1) =>
            SoundTriggerCrossing.Step(previous, now, ClipLength, elapsed, speed);

        static bool Crosses(double start, (double From, double To, bool Wrapped) step) => SoundTriggerCrossing.Crossed(start, step, ClipLength);

        [Test]
        public void Step_Playback_FiresTagsInTheSpanCovered()
        {
            var step = StepOf(0.50, 0.52, 0.02);

            Assert.Multiple(() =>
            {
                Assert.That(step.Wrapped, Is.False);
                Assert.That(Crosses(0.51, step), Is.True);
                Assert.That(Crosses(0.50, step), Is.False);
                Assert.That(Crosses(0.53, step), Is.False);
            });
        }

        [Test]
        public void Step_LoopWrap_FiresBothSides()
        {
            var step = StepOf(1.99, 0.01, 0.02);

            Assert.Multiple(() =>
            {
                Assert.That(step.Wrapped, Is.True);
                Assert.That(Crosses(1.995, step), Is.True);
                Assert.That(Crosses(0.0, step), Is.True);
                Assert.That(Crosses(0.005, step), Is.True);
                Assert.That(Crosses(1.98, step), Is.False);
                Assert.That(Crosses(0.02, step), Is.False);
            });
        }

        [Test]
        public void Step_Jump_FiresOnlyTheLastFrame()
        {
            var forward = StepOf(0.2, 1.5, 0.02);
            var back = StepOf(1.5, 0.2, 0.02);

            Assert.Multiple(() =>
            {
                Assert.That(forward.Wrapped, Is.False);
                Assert.That(Crosses(1.0, forward), Is.False);
                Assert.That(Crosses(1.49, forward), Is.True);
                Assert.That(back.Wrapped, Is.False);
                Assert.That(Crosses(1.8, back), Is.False);
                Assert.That(Crosses(0.1, back), Is.False);
                Assert.That(Crosses(0.19, back), Is.True);
            });
        }

        [Test]
        public void Step_ElapsedIsCapped_AndScaledBySpeed()
        {
            var resume = StepOf(1.0, 0.016, 30);
            var normal = StepOf(0.5, 0.56, 0.02);
            var slow = StepOf(0.5, 0.56, 0.02, speed: 0.25);

            Assert.Multiple(() =>
            {
                Assert.That(resume.Wrapped, Is.False);
                Assert.That(Crosses(1.5, resume), Is.False);
                Assert.That(Crosses(1.99, resume), Is.False);
                Assert.That(Crosses(0.0, resume), Is.True);
                Assert.That(normal, Is.EqualTo((0.5, 0.56, false)));
                Assert.That(slow.Wrapped, Is.False);
                Assert.That(slow.From, Is.EqualTo(0.555).Within(0.001));
                Assert.That(Crosses(0.51, slow), Is.False);
            });
        }

        [Test]
        public void Step_FromNoFrame_IsAJump()
        {
            var mid = StepOf(double.NaN, 0.5, 0.02);
            var start = StepOf(double.NaN, 0.01, 0.02);

            Assert.Multiple(() =>
            {
                Assert.That(mid.Wrapped, Is.False);
                Assert.That(mid.From, Is.EqualTo(0.48).Within(1e-5));
                Assert.That(Crosses(0.49, mid), Is.True);
                Assert.That(Crosses(0.47, mid), Is.False);
                Assert.That(Crosses(0.0, start), Is.True);
            });
        }

        [Test]
        public void Step_WrapAndJump_AtAnotherSpeed()
        {
            var fastWrap = StepOf(1.95, 0.08, 0.05, speed: 2);
            var slowJump = StepOf(1.95, 0.08, 0.05, speed: 1);
            var fastJump = StepOf(0.5, 0.9, 0.05, speed: 2);

            Assert.Multiple(() =>
            {
                Assert.That(fastWrap, Is.EqualTo((1.95, 0.08, true)));
                Assert.That(slowJump.Wrapped, Is.False);
                Assert.That(Crosses(1.99, slowJump), Is.False);
                Assert.That(fastJump.From, Is.EqualTo(0.8).Within(1e-5), "a jump reaches back one frame of clip time at double speed");
                Assert.That(Crosses(0.82, fastJump), Is.True);
                Assert.That(Crosses(0.78, fastJump), Is.False);
            });
        }

        [Test]
        public void Crossed_TagOutsideTheClip_NeverFires()
        {
            var steps = new[] { StepOf(0.5, 0.52, 0.02), StepOf(1.99, 0.01, 0.02), StepOf(1.0, 0.016, 30), StepOf(0.2, 1.99, 0.02) };

            Assert.Multiple(() =>
            {
                foreach (var step in steps)
                {
                    Assert.That(Crosses(-0.1, step), Is.False);
                    Assert.That(Crosses(-0.05, step), Is.False);
                    Assert.That(Crosses(2.1, step), Is.False);
                }
            });
        }

        static Bank Sounding(params (string Tag, uint Source)[] tags)
        {
            var bank = new Bank();
            foreach (var (tag, source) in tags)
            {
                bank.Row(tag, tag + "_Event");
                bank.Event(tag + "_Event", bank.Play(bank.Sound(source)));
            }
            return bank;
        }

        static SoundTriggerTag Trigger(string tag, float start) => new(tag, start, "slot .snd.meta");

        sealed class SessionRun
        {
            public List<uint> Played { get; } = [];
            public List<uint> Prepared { get; } = [];
            public SoundTriggerSession Session { get; }

            public SessionRun(Bank bank, bool prepareResult = true) =>
                Session = new SoundTriggerSession(bank.Resolver(), id => { Prepared.Add(id); return prepareResult; }, Played.Add, new Random(1));

            public SessionRun Rebuild(params SoundTriggerTag[] tags)
            {
                Session.Rebuild(tags, campaign: false);
                return this;
            }

            public SessionRun Advance(double now, double elapsed = 0.02, bool playing = true, double speed = 1, bool loop = true)
            {
                Session.Advance(now, playing, elapsed, speed, ClipLength, loop);
                return this;
            }

            public SessionRun Frames(int first, int count, bool loop = true)
            {
                foreach (var i in Enumerable.Range(first, count))
                    Advance(i % 100 / 50.0, loop: loop);
                return this;
            }
        }

        [Test]
        public void Session_PlayStartAtZero_FiresTagAtZero()
        {
            var run = new SessionRun(Sounding(("Start", 1), ("Late", 2))).Rebuild(Trigger("Start", 0f), Trigger("Late", 1.5f));
            run.Advance(1.3, playing: false).Advance(0.016, elapsed: 5);

            var resumed = new SessionRun(Sounding(("Tag", 3))).Rebuild(Trigger("Tag", 0.9f));
            resumed.Advance(1.0, elapsed: 0.016).Advance(1.016, elapsed: 30);

            Assert.Multiple(() =>
            {
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }));
                Assert.That(resumed.Played, Is.Empty);
            });
        }

        [Test]
        public void Session_NotPlaying_FiresNothing_ButRemembersTheTime()
        {
            var run = new SessionRun(Sounding(("Before", 1), ("AtThePause", 2))).Rebuild(Trigger("Before", 0.95f), Trigger("AtThePause", 1.0f));

            run.Advance(1.0, playing: false);
            var whilePaused = run.Played.ToList();
            run.Advance(1.02, elapsed: 0.1);

            Assert.Multiple(() =>
            {
                Assert.That(whilePaused, Is.Empty);
                Assert.That(run.Played, Is.EqualTo(new uint[] { 2 }));
            });
        }

        [TestCase(0.0)]
        [TestCase(0.03)]
        public void Session_FirstFrameThenPlay_FiresATagAtZero_Once(double offset)
        {
            var run = new SessionRun(Sounding(("Start", 1))).Rebuild(Trigger("Start", 0f));
            run.Session.OffsetSeconds = offset;

            run.Frames(30, 10);
            run.Advance(0, playing: false);
            run.Frames(1, 10);

            Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }));
        }

        [TestCase(0.0, true)]
        [TestCase(0.0, false)]
        [TestCase(0.03, true)]
        [TestCase(0.03, false)]
        public void Session_Loop_FiresTheTagsAtBothEnds_OncePerPass(double offset, bool afterTheLoop)
        {
            var run = new SessionRun(Sounding(("Start", 1), ("LastFrame", 2), ("End", 3))).Rebuild(Trigger("Start", 0f), Trigger("LastFrame", 1.97f), Trigger("End", 2.0f));
            run.Session.OffsetSeconds = offset;

            run.Advance(0, playing: false);
            foreach (var _ in Enumerable.Range(0, 3))
            {
                run.Frames(1, 97);
                if (!afterTheLoop)
                    run.Advance(1.96);
                run.Session.Loop(1, ClipLength);
                run.Advance(0, playing: false);
                if (afterTheLoop)
                    run.Advance(0, elapsed: 0);
            }
            run.Advance(0.02);

            Assert.That(run.Played.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count()), Is.EqualTo(new Dictionary<uint, int> { [1] = 4, [2] = 3, [3] = 3 }));
        }

        [Test]
        public void Session_Status_CountsWhatIsNotSupported_ApartFromWhatIsSilentInGame()
        {
            var bank = Sounding(("A", 1)).Row("Continue", "Continue_Event");
            bank.Event("Continue_Event", bank.Action(AkActionType.PlayAndContinue, bank.Sound(7)));
            var run = new SessionRun(bank).Rebuild(Trigger("A", 0.1f), Trigger("Continue", 0.2f), Trigger("X", 0.3f));

            Assert.That(run.Session.StatusLines, Is.EqualTo(new[]
            {
                "3 triggers: 1 plays, 1 silent in game, 1 not supported",
                "Continue (slot .snd.meta): not supported: PlayAndContinue action -> Sound",
                "X (slot .snd.meta): no audio_metadata_tags_tables row",
            }));
        }

        [Test]
        public void Session_FiresEachTagOncePerPass()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 1.0f));

            run.Frames(48, 4);
            var firstPass = run.Played.ToList();
            run.Frames(52, 100);

            Assert.Multiple(() =>
            {
                Assert.That(firstPass, Is.EqualTo(new uint[] { 1 }));
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1, 1 }));
            });
        }

        [Test]
        public void Session_UnsavedEdit_FiresAtTheNewTime()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 0.5f)).Rebuild(Trigger("Tag", 1.0f));

            run.Frames(23, 5);
            var throughOldTime = run.Played.ToList();
            run.Frames(28, 25);

            Assert.Multiple(() =>
            {
                Assert.That(throughOldTime, Is.Empty);
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }));
            });
        }

        [Test]
        public void Session_Rebuild_KeepsThePlayheadTime()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild();
            run.Advance(0.50);
            run.Rebuild(Trigger("Tag", 0.51f));
            run.Advance(0.52, elapsed: 0.001);

            Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }));
        }

        [Test]
        public void Session_AdvanceBeforeRebuild_DoesNothing()
        {
            var run = new SessionRun(Sounding(("Tag", 1)));

            Assert.DoesNotThrow(() => run.Advance(0.48).Advance(0.52));
            run.Rebuild(Trigger("Tag", 0.5f)).Advance(0.52);

            Assert.That(run.Played, Is.Empty);
        }

        [TestCase(0.0)]
        [TestCase(0.03)]
        public void Session_LoopOff_ATagAtZeroFiresOnceAtEachPlay_AndNotAtTheEnd(double offset)
        {
            var run = new SessionRun(Sounding(("Start", 1))).Rebuild(Trigger("Start", 0f));
            run.Session.OffsetSeconds = offset;

            run.Advance(0, playing: false, loop: false);
            run.Frames(1, 10, loop: false);
            var atPlay = run.Played.ToList();
            run.Frames(11, 89, loop: false);
            run.Advance(2.0, playing: false, loop: false);
            var throughThePass = run.Played.ToList();
            run.Advance(0, playing: false, loop: false);
            run.Frames(1, 10, loop: false);

            Assert.Multiple(() =>
            {
                Assert.That(atPlay, Is.EqualTo(new uint[] { 1 }));
                Assert.That(throughThePass, Is.EqualTo(new uint[] { 1 }), "nothing more by the end");
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1, 1 }), "once more at the next Play");
            });
        }

        [Test]
        public void Session_LoopOff_TheStepThatReachesTheEnd_FiresWhatIsDueInIt()
        {
            var run = new SessionRun(Sounding(("LastStep", 1), ("End", 2), ("Paused", 3))).Rebuild(Trigger("LastStep", 1.99f), Trigger("End", 2.0f), Trigger("Paused", 0.99f));

            run.Frames(40, 9, loop: false);
            run.Advance(1.0, playing: false, loop: false);
            var pausedMidClip = run.Played.ToList();
            run.Frames(51, 48, loop: false);
            run.Advance(2.0, playing: false, loop: false);
            var atTheEnd = run.Played.ToList();
            run.Advance(2.0, playing: false, loop: false);

            Assert.Multiple(() =>
            {
                Assert.That(pausedMidClip, Is.Empty, "a frame step onto 1.0 fires nothing");
                Assert.That(atTheEnd, Is.EqualTo(new uint[] { 1, 2 }));
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1, 2 }), "a second paused frame there fires nothing more");
            });
        }

        [Test]
        public void Session_LoopOff_LateOffset_FiresATagAtTheEnd_OnTheLastStep()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 1.99f));
            run.Session.OffsetSeconds = -0.03;

            run.Frames(95, 4, loop: false);
            var before = run.Played.ToList();
            run.Advance(2.0, playing: false, loop: false);

            Assert.Multiple(() =>
            {
                Assert.That(before, Is.Empty);
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }));
            });
        }

        [Test]
        public void Session_LoopOn_AHitchThatStopsAtTheEnd_LeavesATagAtZeroForTheNextPlay()
        {
            var run = new SessionRun(Sounding(("Start", 1), ("LastStep", 2))).Rebuild(Trigger("Start", 0f), Trigger("LastStep", 1.995f));
            run.Session.OffsetSeconds = 0.03;

            run.Advance(0, playing: false);
            run.Frames(1, 98);
            var atPlay = run.Played.ToList();
            run.Advance(2.0, playing: false);
            var atTheEnd = run.Played.ToList();
            run.Advance(0, playing: false);
            run.Frames(1, 10);

            Assert.Multiple(() =>
            {
                Assert.That(atPlay, Is.EqualTo(new uint[] { 1 }));
                Assert.That(atTheEnd, Is.EqualTo(new uint[] { 1, 2 }), "only the tag due in the last step");
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1, 2, 1 }), "the tag at 0.0 once more, at the next Play");
            });
        }

        [Test]
        public void Session_OffsetLongerThanTheClip_StillFiresOncePerPass()
        {
            const double Length = 0.04;
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 0.03f));
            run.Session.OffsetSeconds = 0.1;

            foreach (var i in Enumerable.Range(1, 32))
                run.Session.Advance(i % 4 * 0.01, true, 0.01, 1, Length, true);

            Assert.That(run.Played, Has.Count.EqualTo(8));
        }

        [TestCase(0.5, 0.01, 1.98, 1.99)]
        [TestCase(2.0, 0.02, 1.92, 1.94)]
        public void Session_EarlyOffset_WrapsByTheClipTimeAtTheFramesSpeed(double speed, double step, double lastWithout, double firing)
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 0f));
            run.Session.OffsetSeconds = 0.03;
            var frame = step / speed;

            for (var at = 1.80; at < lastWithout + 1e-9; at += step)
                run.Advance(Math.Round(at, 2), elapsed: frame, speed: speed);
            var before = run.Played.ToList();
            run.Advance(firing, elapsed: frame, speed: speed);

            Assert.Multiple(() =>
            {
                Assert.That(before, Is.Empty, $"nothing up to {lastWithout}");
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }), $"fired by the frame landing at {firing}");
            });
        }

        [Test]
        public void Session_Status_SummaryThenOneLinePerSilentTag()
        {
            var run = new SessionRun(Sounding(("A", 1), ("B", 2)));
            run.Rebuild(Trigger("A", 0.1f), Trigger("Derpy_Swing_01", 0.2f), Trigger("B", 0.3f));

            Assert.That(run.Session.StatusLines, Is.EqualTo(new[]
            {
                "3 triggers: 2 play, 1 silent in game",
                "Derpy_Swing_01 (slot .snd.meta): no audio_metadata_tags_tables row",
            }));
        }

        [Test]
        public void Session_Status_NoTags_OneTag_AndAllSilent()
        {
            var run = new SessionRun(Sounding(("A", 1)));

            Assert.Multiple(() =>
            {
                Assert.That(run.Rebuild().Session.StatusLines, Is.EqualTo(new[] { "0 triggers" }));
                Assert.That(run.Rebuild(Trigger("A", 0.1f)).Session.StatusLines, Is.EqualTo(new[] { "1 trigger: 1 plays, 0 silent in game" }));
                Assert.That(run.Rebuild(Trigger("X", 0.1f)).Session.StatusLines[0], Is.EqualTo("1 trigger: 0 play, 1 silent in game"));
                Assert.That(run.Rebuild(Trigger("X", 0.1f), Trigger("Y", 0.2f)).Session.StatusLines, Has.Count.EqualTo(3));
                Assert.That(run.Session.StatusLines[0], Is.EqualTo("2 triggers: 0 play, 2 silent in game"));
            });
        }

        [Test]
        public void Session_Status_AgreesInNumber_OnBothCounts()
        {
            var run = new SessionRun(Sounding(("A", 1), ("B", 2)));

            Assert.Multiple(() =>
            {
                Assert.That(run.Rebuild(Trigger("A", 0.1f), Trigger("X", 0.2f), Trigger("Y", 0.3f)).Session.StatusLines[0], Is.EqualTo("3 triggers: 1 plays, 2 silent in game"));
                Assert.That(run.Rebuild(Trigger("A", 0.1f), Trigger("B", 0.2f), Trigger("X", 0.3f)).Session.StatusLines[0], Is.EqualTo("3 triggers: 2 play, 1 silent in game"));
                Assert.That(run.Rebuild(Trigger("A", 0.1f), Trigger("B", 0.2f)).Session.StatusLines[0], Is.EqualTo("2 triggers: 2 play, 0 silent in game"));
            });
        }

        [Test]
        public void Session_FailedPrepare_StillCountsAsPlayable()
        {
            var run = new SessionRun(Sounding(("A", 1)), prepareResult: false).Rebuild(Trigger("A", 0.1f));

            Assert.Multiple(() =>
            {
                Assert.That(run.Session.StatusLines, Is.EqualTo(new[] { "1 trigger: 1 plays, 0 silent in game" }));
                Assert.That(run.Prepared, Is.EqualTo(new uint[] { 1 }));
            });
        }

        [Test]
        public void Session_LeavesAndSwitches_AreDistinctAcrossTags()
        {
            var bank = new Bank().Row("A", "Ea").Row("B", "Eb");
            var grass = bank.Sound(201);
            var stone = bank.Sound(202);
            var shared = bank.Sound(300);
            bank.Event("Ea", bank.Play(bank.Switch("Surface", "Grass", ("Grass", grass), ("Stone", stone))), bank.Play(shared));
            bank.Event("Eb", bank.Play(bank.Switch("Surface", "Stone", ("Stone", stone), ("Grass", grass))), bank.Play(shared));
            var run = new SessionRun(bank).Rebuild(Trigger("A", 0.1f), Trigger("B", 0.2f));

            Assert.Multiple(() =>
            {
                Assert.That(run.Session.Leaves, Is.EqualTo(new uint[] { 201, 300, 202 }));
                Assert.That(run.Prepared, Is.EqualTo(new uint[] { 201, 300, 202 }));
                Assert.That(run.Session.Switches, Has.Count.EqualTo(1));
                Assert.That(run.Session.Switches[0].DefaultSwitch, Is.EqualTo(Hash("Grass")));
            });
        }

        [Test]
        public void Session_SwitchChoice_SurvivesRebuild()
        {
            var bank = new Bank().Row("Tag", "Step");
            bank.Event("Step", bank.Play(bank.Switch("Surface", "Grass", ("Grass", bank.Sound(201)), ("Stone", bank.Sound(202)))));
            var run = new SessionRun(bank);
            run.Session.SwitchChoices[Hash("Surface")] = Hash("Stone");

            run.Rebuild(Trigger("Tag", 0.5f)).Rebuild(Trigger("Tag", 0.5f)).Advance(0.5);

            Assert.Multiple(() =>
            {
                Assert.That(run.Session.Leaves, Is.EqualTo(new uint[] { 202 }));
                Assert.That(run.Played, Is.EqualTo(new uint[] { 202 }));
            });
        }

        [Test]
        public void Session_Offset_FiresEarly()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 0.50f));
            run.Session.OffsetSeconds = 0.03;

            run.Advance(0.46);
            var before = run.Played.ToList();
            run.Advance(0.48);

            Assert.Multiple(() =>
            {
                Assert.That(before, Is.Empty);
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }));
            });
        }

        [Test]
        public void Session_Offset_IsRealTime_SoAtHalfSpeedItIsHalfAsMuchOfTheClip()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 0.50f));
            run.Session.OffsetSeconds = 0.05;

            foreach (var i in Enumerable.Range(40, 8))
                run.Advance(i / 100.0, speed: 0.5);
            var before = run.Played.ToList();
            run.Advance(0.48, speed: 0.5);

            Assert.Multiple(() =>
            {
                Assert.That(before, Is.Empty, "nothing up to the frame at 0.47");
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }), "fired by the frame crossing 0.475");
            });
        }

        [Test]
        public void Session_EarlyOffset_FiresATagAtZeroBeforeTheLoop_Once()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 0f));
            run.Session.OffsetSeconds = 0.03;

            run.Advance(1.96);
            var before = run.Played.ToList();
            run.Advance(1.98);
            var early = run.Played.ToList();
            run.Advance(1.99).Advance(0.01);
            var acrossTheWrap = run.Played.ToList();
            run.Frames(1, 99);

            Assert.Multiple(() =>
            {
                Assert.That(before, Is.Empty);
                Assert.That(early, Is.EqualTo(new uint[] { 1 }));
                Assert.That(acrossTheWrap, Is.EqualTo(new uint[] { 1 }));
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1, 1 }));
            });
        }

        [Test]
        public void Session_EarlyOffset_TagAtZeroStillFiresOnThePlayStartJump()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 0f));
            run.Session.OffsetSeconds = 0.03;

            run.Advance(1.3, playing: false).Advance(0.016, elapsed: 5);

            Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }));
        }

        [Test]
        public void Session_LateOffset_FiresATagAtTheEndAfterTheWrap_NotBefore()
        {
            var run = new SessionRun(Sounding(("Tag", 1))).Rebuild(Trigger("Tag", 1.99f));
            run.Session.OffsetSeconds = -0.03;

            run.Advance(1.98).Advance(2.0);
            var atTheEnd = run.Played.ToList();
            run.Advance(0.01);
            var wrapped = run.Played.ToList();
            run.Advance(0.03);

            Assert.Multiple(() =>
            {
                Assert.That(atTheEnd, Is.Empty);
                Assert.That(wrapped, Is.Empty);
                Assert.That(run.Played, Is.EqualTo(new uint[] { 1 }));
            });
        }

        [TestCase(0.03)]
        [TestCase(-0.03)]
        public void Session_TagOutsideTheClip_NeverFires_WhateverTheOffset(double offset)
        {
            var run = new SessionRun(Sounding(("Before", 1), ("After", 2))).Rebuild(Trigger("Before", -0.1f), Trigger("After", 2.1f));
            run.Session.OffsetSeconds = offset;

            run.Frames(0, 200);

            Assert.That(run.Played, Is.Empty);
        }

        static byte[] SineWem(params double[] amplitudes)
        {
            if (amplitudes.Length == 0)
                amplitudes = [0.5];

            const int rate = 44100;
            var channels = amplitudes.Length;
            var frames = rate / 5;
            var samples = new short[frames * channels];
            for (var i = 0; i < frames; i++)
            {
                for (var c = 0; c < channels; c++)
                    samples[i * channels + c] = (short)(amplitudes[c] * short.MaxValue * Math.Sin(2 * Math.PI * 1000 * i / rate));
            }

            using var wav = new MemoryStream();
            using (var writer = new NAudio.Wave.WaveFileWriter(wav, new NAudio.Wave.WaveFormat(rate, 16, channels)))
                writer.WriteSamples(samples, 0, samples.Length);
            return WemFile.CreateFromWavBytes(wav.ToArray(), new WemEncodingSettings()).WriteData();
        }

        const int FiftyMs = 4800;

        static float[] Read(SoundTriggerMixer mixer, int count)
        {
            var buffer = new float[count];
            Assert.That(mixer.Output.Read(buffer, 0, count), Is.EqualTo(count), "the output always fills the request");
            return buffer;
        }

        static double Rms(float[] samples) => Math.Sqrt(samples.Sum(x => (double)x * x) / samples.Length);

        static float[] Channel(float[] interleaved, int channel) => interleaved.Where((_, i) => i % 2 == channel).ToArray();

        static SoundTriggerMixer MixerWith(uint id, byte[]? wem = null)
        {
            var mixer = new SoundTriggerMixer(openDevice: false);
            Assert.That(mixer.Prepare(id, () => wem ?? SineWem()), Is.True);
            return mixer;
        }

        [Test]
        public void Mixer_PlaysAPreparedSound()
        {
            using var mixer = MixerWith(7);

            mixer.Play(7);

            Assert.That(Rms(Read(mixer, FiftyMs)), Is.GreaterThan(0.05));
        }

        [Test]
        public void Mixer_MonoSource_PlaysOnBothChannels_At1000Hz()
        {
            using var mixer = MixerWith(7);

            mixer.Play(7);
            var read = Read(mixer, FiftyMs);
            var left = Channel(read, 0);
            var right = Channel(read, 1);
            var crossings = left.Zip(left.Skip(1)).Count(pair => pair.First < 0 != pair.Second < 0);

            Assert.Multiple(() =>
            {
                Assert.That(right, Is.EqualTo(left), "a mono source plays the same on both channels");
                Assert.That(crossings, Is.InRange(97, 103));
            });
        }

        [Test]
        public void Mixer_StereoSource_KeepsItsChannels()
        {
            using var mixer = MixerWith(7, SineWem(0.5, 0));

            mixer.Play(7);
            var read = Read(mixer, FiftyMs);

            Assert.Multiple(() =>
            {
                Assert.That(Rms(Channel(read, 0)), Is.GreaterThan(0.05));
                Assert.That(Rms(Channel(read, 1)), Is.LessThan(0.02));
            });
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(6)]
        public void Mixer_MoreThanTwoChannels_KeepsTheFirstTwo(int channels)
        {
            var wem = SineWem(Enumerable.Range(0, channels).Select(c => 0.1 + 0.15 * c).ToArray());
            var decoded = DecodedChannelRms(wem, frames: 4410);
            using var mixer = MixerWith(7, wem);

            mixer.Play(7);
            var read = Read(mixer, 2 * FiftyMs);

            Assert.Multiple(() =>
            {
                Assert.That(Math.Abs(decoded[0] - decoded[2]), Is.GreaterThan(0.04), "the third channel is told apart from the first");
                Assert.That(Math.Abs(decoded[1] - decoded[2]), Is.GreaterThan(0.04), "and from the second");
                Assert.That(Rms(Channel(read, 0)), Is.EqualTo(decoded[0]).Within(0.02));
                Assert.That(Rms(Channel(read, 1)), Is.EqualTo(decoded[1]).Within(0.02));
            });
        }

        static double[] DecodedChannelRms(byte[] wem, int frames)
        {
            using var reader = new NAudio.Vorbis.VorbisWaveReader(new MemoryStream(Shared.GameFormats.Audio.Containers.Ogg.OggFile.CreateFromWemBytes(wem).WriteData(), writable: false));
            var channels = reader.WaveFormat.Channels;
            var buffer = new float[frames * channels];
            Assert.That(reader.Read(buffer, 0, buffer.Length), Is.EqualTo(buffer.Length));
            return Enumerable.Range(0, channels).Select(c => Rms(buffer.Where((_, i) => i % channels == c).ToArray())).ToArray();
        }

        [Test]
        public void Mixer_AVoice_PlaysItsSoundOnceAndEnds()
        {
            using var mixer = MixerWith(7);

            mixer.Play(7);
            var chunks = Enumerable.Range(0, 6).Select(_ => Rms(Read(mixer, FiftyMs))).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(chunks.Take(4), Is.All.GreaterThan(0.05), "the sound plays through its 0.2 s");
                Assert.That(chunks[5], Is.EqualTo(0), "and is silent from 0.25 s on");
            });
        }

        [Test]
        public void Mixer_SameSoundTwice_IsTwoVoices()
        {
            using var mixer = MixerWith(7);

            mixer.Play(7);
            var alone = Rms(Read(mixer, FiftyMs));
            mixer.Play(7);
            var overlap = Rms(Read(mixer, FiftyMs));
            Read(mixer, 2 * FiftyMs);
            var pastTheFirst = Rms(Read(mixer, FiftyMs));

            Assert.Multiple(() =>
            {
                Assert.That(alone, Is.GreaterThan(0.05));
                Assert.That(overlap, Is.GreaterThan(alone * 1.5));
                Assert.That(pastTheFirst, Is.GreaterThan(0.05), "the second voice is still sounding after the first has ended");
            });
        }

        [Test]
        public void Mixer_StopAll_Silences()
        {
            using var mixer = MixerWith(7);
            mixer.Play(7);
            Assert.That(Rms(Read(mixer, FiftyMs)), Is.GreaterThan(0.05), "the sound was playing");

            mixer.StopAll();

            Assert.That(Read(mixer, FiftyMs), Is.All.EqualTo(0f));
        }

        [Test]
        public void Mixer_UnpreparedOrBadBytes()
        {
            using var mixer = new SoundTriggerMixer(openDevice: false);

            var results = new bool[2];
            Assert.DoesNotThrow(() => results[0] = mixer.Prepare(8, () => null));
            Assert.DoesNotThrow(() => results[1] = mixer.Prepare(9, () => [1, 2, 3]));
            mixer.Play(8);
            mixer.Play(9);
            mixer.Play(10);

            Assert.Multiple(() =>
            {
                Assert.That(results, Is.All.False);
                Assert.That(Read(mixer, FiftyMs), Is.All.EqualTo(0f));
            });
        }

        [Test]
        public void Mixer_Keep_DropsOthers()
        {
            using var mixer = MixerWith(7);
            Assert.That(mixer.Prepare(10, () => SineWem()), Is.True);

            mixer.Keep([7]);
            mixer.Play(10);
            var dropped = Read(mixer, FiftyMs);
            mixer.Play(7);
            var kept = Read(mixer, FiftyMs);

            Assert.Multiple(() =>
            {
                Assert.That(dropped, Is.All.EqualTo(0f));
                Assert.That(Rms(kept), Is.GreaterThan(0.05));
            });
        }

        [Test]
        public void Mixer_Keep_LetsAPlayingVoiceFinish()
        {
            using var mixer = MixerWith(7);
            mixer.Play(7);

            mixer.Keep([]);

            Assert.That(Rms(Read(mixer, FiftyMs)), Is.GreaterThan(0.05));
        }

        [Test]
        public void Mixer_PreparedAgain_DoesNotDecodeAgain()
        {
            using var mixer = MixerWith(7);

            var asked = false;
            var again = mixer.Prepare(7, () => { asked = true; return SineWem(); });

            Assert.Multiple(() =>
            {
                Assert.That(again, Is.True);
                Assert.That(asked, Is.False);
            });
        }

        [Test]
        public void Mixer_AfterDispose_PlaysNothing()
        {
            var mixer = MixerWith(7);
            mixer.Dispose();

            mixer.Play(7);
            var (preparedAfter, asked) = (true, false);
            Assert.DoesNotThrow(() => preparedAfter = mixer.Prepare(8, () => { asked = true; return SineWem(); }));
            mixer.Play(8);

            Assert.Multiple(() =>
            {
                Assert.That(preparedAfter, Is.False);
                Assert.That(asked, Is.False, "the bytes were not asked for");
                Assert.That(Read(mixer, FiftyMs), Is.All.EqualTo(0f));
            });
        }

        [Test]
        public void Mixer_MoreVoicesThanTheMixerTakes_PlaysTheFirst1024()
        {
            using var mixer = MixerWith(7);
            mixer.Play(7);
            var one = Rms(Read(mixer, FiftyMs));
            mixer.StopAll();

            Assert.DoesNotThrow(() => { for (var i = 0; i < 1100; i++) mixer.Play(7); });
            var many = Rms(Read(mixer, FiftyMs));

            Assert.That(many / one, Is.EqualTo(1024).Within(1).Percent);
        }

        static Mock<IWavePlayer> Device(string? failAt = null)
        {
            var device = new Mock<IWavePlayer>();
            var fail = new InvalidOperationException("the device refused: " + failAt);
            if (failAt == "Play")
                device.Setup(x => x.Play()).Throws(fail);
            if (failAt == "Init")
                device.Setup(x => x.Init(It.IsAny<IWaveProvider>())).Throws(fail);
            if (failAt == "Stop")
                device.Setup(x => x.Stop()).Throws(fail);
            if (failAt == "Dispose")
                device.Setup(x => x.Dispose()).Throws(fail);
            return device;
        }

        [TestCase("Init")]
        [TestCase("Play")]
        public void Mixer_ADeviceThatCannotStart_IsDisposed_AndTheNextOneIsUsed(string failAt)
        {
            var (first, second) = (Device(failAt), Device());

            using (new SoundTriggerMixer([() => first.Object, () => second.Object]))
            {
                first.Verify(x => x.Dispose(), Times.Once);
                second.Verify(x => x.Play(), Times.Once);
                second.Verify(x => x.Dispose(), Times.Never);
            }
            second.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void Mixer_ADeviceThatStopsWithAFailure_SaysSo()
        {
            var device = Device();
            using var mixer = new SoundTriggerMixer([() => device.Object]);
            var stopped = new List<Exception>();
            mixer.DeviceStopped += stopped.Add;
            var failure = new InvalidOperationException("the endpoint went away");

            device.Raise(x => x.PlaybackStopped += null, new StoppedEventArgs());
            device.Raise(x => x.PlaybackStopped += null, new StoppedEventArgs(failure));

            Assert.That(stopped, Is.EqualTo(new[] { failure }));
        }

        [Test]
        public void Mixer_WhenNoDeviceStarts_EachIsDisposed_AndTheLastFailureIsThrown()
        {
            var (first, second) = (Device("Play"), Device("Init"));

            var thrown = Assert.Throws<InvalidOperationException>(() => new SoundTriggerMixer([() => first.Object, () => second.Object]));

            Assert.That(thrown!.Message, Does.EndWith("Init"));
            first.Verify(x => x.Dispose(), Times.Once);
            second.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void Mixer_Dispose_StopsAndDisposesTheDeviceOnce_AndNeverThrows()
        {
            var (quiet, failingStop, failingDispose) = (Device(), Device("Stop"), Device("Dispose"));
            var mixer = new SoundTriggerMixer([() => quiet.Object]);

            mixer.Dispose();
            Assert.DoesNotThrow(mixer.Dispose);
            Assert.DoesNotThrow(new SoundTriggerMixer([() => failingStop.Object]).Dispose);
            Assert.DoesNotThrow(new SoundTriggerMixer([() => failingDispose.Object]).Dispose);

            quiet.Verify(x => x.Stop(), Times.Once);
            quiet.Verify(x => x.Dispose(), Times.Once);
            failingStop.Verify(x => x.Dispose(), Times.Once);
            failingDispose.Verify(x => x.Stop(), Times.Once);
        }

        [Test]
        public void Mixer_UsedFromSeveralThreads_KeepsWorking()
        {
            using var mixer = new SoundTriggerMixer(openDevice: false);
            var wem = SineWem();
            using var stop = new CancellationTokenSource();
            var reader = Task.Run(() =>
            {
                var buffer = new float[FiftyMs];
                while (!stop.IsCancellationRequested)
                    Assert.That(mixer.Output.Read(buffer, 0, buffer.Length), Is.EqualTo(buffer.Length));
            });
            var users = Enumerable.Range(0, 2).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var id = (uint)(t * 1000 + i % 5);
                    mixer.Prepare(id, () => wem);
                    mixer.Play(id);
                    mixer.Keep([id, (uint)((1 - t) * 1000 + i % 5)]);
                }
            })).ToArray();

            Assert.That(Task.WaitAll(users, TimeSpan.FromSeconds(15)), Is.True, "the two threads finished");
            stop.Cancel();
            Assert.That(reader.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(mixer.Prepare(7, () => wem), Is.True);
            mixer.StopAll();
            mixer.Play(7);
            Assert.That(Rms(Read(mixer, FiftyMs)), Is.GreaterThan(0.05));
        }
    }
}
