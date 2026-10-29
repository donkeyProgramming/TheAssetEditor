using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.MusicDat;

namespace Test.Audio
{
    // The music project is the one file a mixer mod's maintainer keeps: every culture is added to
    // it, and the scripts are rebuilt from vanilla with all of them. The installed game's scripts
    // stand in a pack of their own; tests that build them are skipped without an install.
    internal class MusicProjectBuildTests
    {
        ServiceProvider _provider;
        IPackFileService _packFileService;

        [SetUp]
        public void SetUp()
        {
            _provider = VanillaBankReader.CreateProvider(Path.GetTempPath());
            _packFileService = _provider.GetRequiredService<IPackFileService>();

            var game = _packFileService.CreateNewPackFileContainer("game", PackFileVersion.PFH5, PackFileCAType.RELEASE);
            if (VanillaBankReader.HasInstall)
            {
                _packFileService.AddFilesToPack(game,
                [
                    new NewPackFileEntry(MusicDatFixtures.ScriptFolder, PackFile.CreateFromBytes("battle_music.dat", MusicDatFixtures.ReadBytes("battle_music.dat"))),
                    new NewPackFileEntry(MusicDatFixtures.ScriptFolder, PackFile.CreateFromBytes("campaign_music.dat", MusicDatFixtures.ReadBytes("campaign_music.dat")))
                ]);
            }
            game.IsCaPackFile = true; // read-only from here on

            _packFileService.CreateNewPackFileContainer("mod", PackFileVersion.PFH5, PackFileCAType.MOD, setEditablePack: true);
        }

        [TearDown]
        public void TearDown() => _provider.Dispose();

        [Test]
        public void TheScriptsAreVanillaWithEveryCultureWiredIn()
        {
            MusicCulture[] cultures = [Culture("First_Culture", "first_sc"), Culture("Second_Culture", "second_sc")];

            var build = Build().Build(cultures);

            Assert.That(build.Problems, Is.Empty);
            Assert.That(Write(build.Battle), Is.EqualTo(Write(WiredByHand("battle_music.dat", cultures, culture => culture.MusicalCulture))));
            Assert.That(Write(build.Campaign), Is.EqualTo(Write(WiredByHand("campaign_music.dat", cultures, culture => culture.SubcultureKey))));
        }

        // The mod pack holds the last build's scripts. Building on those would wire every culture
        // in a second time on each rebuild.
        [Test]
        public void RebuildingStartsFromVanillaNotFromTheLastBuild()
        {
            MusicCulture[] cultures = [Culture("First_Culture", "first_sc")];

            Assert.That(Build().BuildAndSave(cultures), Is.Empty);
            var firstBuild = _packFileService.FindFile(MusicScriptBuildService.BattleScriptPath, _packFileService.GetEditablePack()).DataSource.ReadData();

            Assert.That(Build().BuildAndSave(cultures), Is.Empty);
            var secondBuild = _packFileService.FindFile(MusicScriptBuildService.BattleScriptPath, _packFileService.GetEditablePack()).DataSource.ReadData();

            Assert.That(secondBuild, Is.EqualTo(firstBuild));
        }

        // A game update that renames a function is harmless, since chains are found by shape. One
        // that removes or restructures a chain the project uses must not silently drop the culture
        // from it.
        [Test]
        public void ASlotNeitherScriptHasAnyMoreIsReported()
        {
            MusicDatFixtures.RequireInstall();
            var culture = BattleCulture("First_Culture", "first_sc");
            culture.Slots.Add(new MusicCultureSlot { EventPrefix = "music_c_removed_in_an_update_" });

            var build = Build().Build([culture]);

            Assert.That(build.Problems, Has.Some.Contains("music_c_removed_in_an_update_").And.Contains("First_Culture"));
        }

        [Test]
        public void AGameScriptThatCannotBeReadIsReportedRatherThanThrown()
        {
            var corrupted = MusicDatFixtures.ReadBytes("battle_music.dat");
            BitConverter.GetBytes(int.MaxValue).CopyTo(corrupted, 8); // the first string's length

            // A later game pack, so its copy is the one the game would load.
            var patch = _packFileService.CreateNewPackFileContainer("patch", PackFileVersion.PFH5, PackFileCAType.RELEASE);
            _packFileService.AddFilesToPack(patch, [new NewPackFileEntry(@"audio\scripts", PackFile.CreateFromBytes("battle_music.dat", corrupted))]);
            patch.IsCaPackFile = true;

            var build = Build().Build([Culture("First_Culture", "first_sc")]);

            Assert.That(build.Problems, Has.Some.Contains(MusicScriptBuildService.BattleScriptPath).And.Contains("could not be read"));
        }

        // Music lives on the project's own Music object, so an Audio Editor project saves exactly
        // what it did before music existed.
        [Test]
        public void ADialogueProjectSavesNoMusicFields()
        {
            const string filePath = @"audio\audio_projects\dialogue.aproj";
            var audioProject = AudioProjectFile.CreateNew("english(uk)", "dialogue");
            var playAction = Editors.Audio.Shared.AudioProject.Models.Action.CreatePlay(1, global::Shared.GameFormats.Wwise.Enums.AkBkHircType.Sound, 2, 3);
            audioProject.SoundBanks[0].ActionEvents.Add(new ActionEvent(10, "Play_dialogue", [playAction], Wh3ActionEventType.BattleAbilities));

            _provider.GetRequiredService<IAudioProjectFileService>().Save(audioProject, Path.GetFileName(filePath), filePath);
            var json = System.Text.Encoding.UTF8.GetString(_packFileService.FindFile(filePath)!.DataSource.ReadData());

            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Contain("Play_dialogue"), "the project was not saved");
                Assert.That(json, Does.Not.Contain("Music"));
                Assert.That(json, Does.Not.Contain("Ams"));
                Assert.That(json, Does.Not.Contain("StateGroupId"));
            });
        }

        [Test]
        public void EveryCultureIsKeptInTheOneProject()
        {
            var service = Service();
            var audioProject = service.CreateNew();

            service.AddCulture(audioProject, BattleCulture("First_Culture", "first_sc"));
            service.AddCulture(audioProject, BattleCulture("Second_Culture", "second_sc"));

            var reloaded = SaveAndReload(audioProject);
            Assert.Multiple(() =>
            {
                Assert.That(reloaded.Music.Cultures.Select(culture => culture.AudioState), Is.EqualTo(new[] { "First_Culture", "Second_Culture" }));
                Assert.That(reloaded.GetActionEvents().Select(actionEvent => actionEvent.Name),
                    Is.EquivalentTo(new[] { "music_b_faction_first_culture", "music_b_faction_second_culture" }));
            });
        }

        // Running the wizard again for a culture updates it: one entry, in the same place, and no
        // events for slots it has dropped.
        [Test]
        public void AddingTheSameCultureAgainReplacesItInPlace()
        {
            var service = Service();
            var audioProject = service.CreateNew();
            service.AddCulture(audioProject, Culture("First_Culture", "first_sc"));
            service.AddCulture(audioProject, BattleCulture("Second_Culture", "second_sc"));

            var changed = BattleCulture("First_Culture", "first_sc");
            var result = service.AddCulture(audioProject, changed);

            Assert.Multiple(() =>
            {
                Assert.That(result.CreatedEvents, Is.Empty);
                Assert.That(audioProject.Music.Cultures.Select(culture => culture.AudioState), Is.EqualTo(new[] { "First_Culture", "Second_Culture" }));
                Assert.That(audioProject.GetActionEvents().Select(actionEvent => actionEvent.Name),
                    Is.EquivalentTo(new[] { "music_b_faction_first_culture", "music_b_faction_second_culture" }));
            });
        }

        [Test]
        public void SettingAnEventsAudioReplacesWhatItHad()
        {
            var service = Service();
            var audioProject = service.CreateNew();
            service.AddCulture(audioProject, BattleCulture("First_Culture", "first_sc"));

            service.SetAudio(audioProject, "music_b_faction_first_culture", [FirstWav, SecondWav]);
            service.SetAudio(audioProject, "music_b_faction_first_culture", [SecondWav]);

            Assert.Multiple(() =>
            {
                Assert.That(service.GetAudio(audioProject, "music_b_faction_first_culture").Select(audioFile => audioFile.WavPackFilePath),
                    Is.EqualTo(new[] { SecondWav }));
                Assert.That(audioProject.AudioFiles.Select(audioFile => audioFile.WavPackFilePath), Is.EqualTo(new[] { SecondWav }));
            });
        }

        // Editing a culture shouldn't cost it the audio of the events it keeps.
        [Test]
        public void ACultureEditedAgainKeepsItsAudio()
        {
            var service = Service();
            var audioProject = service.CreateNew();
            service.AddCulture(audioProject, Culture("First_Culture", "first_sc"));
            service.SetAudio(audioProject, "music_b_faction_first_culture", [FirstWav]);

            service.AddCulture(audioProject, BattleCulture("First_Culture", "first_sc"));

            Assert.That(service.GetAudio(audioProject, "music_b_faction_first_culture").Select(audioFile => audioFile.WavPackFilePath),
                Is.EqualTo(new[] { FirstWav }));
        }

        // Battle culture music has a branch under each container that branches on the culture, so
        // removing it has to take all of them, not just the first one found.
        [Test]
        public void RemovingACultureRemovesEveryBranchItsAudioBuilt()
        {
            var service = Service();
            var audioProject = service.CreateNew();
            var culture = BattleCulture("First_Culture", "first_sc");
            service.AddCulture(audioProject, culture);
            service.SetAudio(audioProject, "music_b_faction_first_culture", [FirstWav]);
            var soundBank = audioProject.SoundBanks.Single(bank => bank.ActionEvents.Count != 0);
            Assert.That(audioProject.Music.RandomSequences, Has.Count.GreaterThan(1), "expected a branch per battle container");

            service.RemoveCulture(audioProject, culture);

            Assert.Multiple(() =>
            {
                Assert.That(audioProject.Music.Cultures, Is.Empty);
                Assert.That(soundBank.ActionEvents, Is.Empty);
                Assert.That(audioProject.Music.RandomSequences, Is.Empty);
                Assert.That(audioProject.Music.Segments, Is.Empty);
                Assert.That(audioProject.AudioFiles, Is.Empty);
                Assert.That(audioProject.StateGroups.SelectMany(stateGroup => stateGroup.States), Is.Empty);
            });
        }

        // --- Music trees ---

        const string BattleEvent = "music_b_faction_first_culture";
        const uint PreBattleContainerId = 26264058;
        const uint InBattleContainerId = 67383790;

        // Audio for one part of the game is only generated in that part's container; the root's
        // goes in both, and the compile works out where each is reached.
        [Test]
        public void ABranchForOnePartOfTheGameIsOnlyGeneratedThere()
        {
            var (service, audioProject) = ProjectWithABattleCulture();
            var tree = service.GetTree(audioProject, BattleEvent);
            service.SetNodeAudio(audioProject, BattleEvent, tree.Root.Id, [FirstWav]);
            var branchId = service.AddBranch(audioProject, BattleEvent, tree.Root.Id, Wh3MusicTreeLevelInformation.ContainerLevel, PreBattleContainerId.ToString());
            service.SetNodeAudio(audioProject, BattleEvent, branchId, [SecondWav]);

            var sequences = audioProject.Music.RandomSequences;
            Assert.Multiple(() =>
            {
                Assert.That(sequences.Where(sequence => sequence.DirectParentId == PreBattleContainerId).Select(sequence => sequence.TreeNodeId),
                    Is.EquivalentTo(new[] { tree.Root.Id, branchId }));
                Assert.That(sequences.Where(sequence => sequence.DirectParentId == InBattleContainerId).Select(sequence => sequence.TreeNodeId),
                    Is.EquivalentTo(new[] { tree.Root.Id }));
            });
        }

        [Test]
        public void RemovingABranchRemovesItsMusicAndTheAudioOnlyItPlayed()
        {
            var (service, audioProject) = ProjectWithABattleCulture();
            var tree = service.GetTree(audioProject, BattleEvent);
            service.SetNodeAudio(audioProject, BattleEvent, tree.Root.Id, [FirstWav]);
            var branchId = service.AddBranch(audioProject, BattleEvent, tree.Root.Id, "Battle_Result_State", "win");
            service.SetNodeAudio(audioProject, BattleEvent, branchId, [SecondWav]);

            service.RemoveBranch(audioProject, BattleEvent, branchId);

            Assert.Multiple(() =>
            {
                Assert.That(tree.Root.SplitBy, Is.Null);
                Assert.That(audioProject.Music.RandomSequences.Select(sequence => sequence.TreeNodeId).Distinct(), Is.EqualTo(new[] { tree.Root.Id }));
                Assert.That(audioProject.AudioFiles.Select(audioFile => audioFile.WavPackFilePath), Is.EqualTo(new[] { FirstWav }));
            });
        }

        [Test]
        public void ANodeSplitsByOneLevelAndNeverByOneAboveIt()
        {
            var (service, audioProject) = ProjectWithABattleCulture();
            var root = service.GetTree(audioProject, BattleEvent).Root;
            var win = service.AddBranch(audioProject, BattleEvent, root.Id, "Battle_Result_State", "win");

            Assert.Multiple(() =>
            {
                Assert.Throws<InvalidOperationException>(() => service.AddBranch(audioProject, BattleEvent, root.Id, "Battle_Music_Key", "C"), "a second level at the same node");
                Assert.Throws<InvalidOperationException>(() => service.AddBranch(audioProject, BattleEvent, root.Id, "Battle_Result_State", "win"), "the same value twice");
                Assert.Throws<InvalidOperationException>(() => service.AddBranch(audioProject, BattleEvent, win, "Battle_Result_State", "lose"), "a level an ancestor splits by");
                Assert.DoesNotThrow(() => service.AddBranch(audioProject, BattleEvent, win, "Battle_Music_Key", "C"));
                Assert.Throws<InvalidOperationException>(() => service.RemoveBranch(audioProject, BattleEvent, root.Id), "the root");
            });
        }

        [Test]
        public void TheTreeIsKeptInTheProjectFile()
        {
            var (service, audioProject) = ProjectWithABattleCulture();
            var root = service.GetTree(audioProject, BattleEvent).Root;
            var win = service.AddBranch(audioProject, BattleEvent, root.Id, "Battle_Result_State", "win");
            service.SetNodeAudio(audioProject, BattleEvent, win, [FirstWav]);
            service.AddBranch(audioProject, BattleEvent, root.Id, "Battle_Result_State", null);

            var reloaded = SaveAndReload(audioProject);
            var reloadedRoot = service.GetTree(reloaded, BattleEvent).Root;

            Assert.Multiple(() =>
            {
                Assert.That(reloadedRoot.SplitBy, Is.EqualTo("Battle_Result_State"));
                Assert.That(reloadedRoot.Branches.Select(branch => branch.Value), Is.EqualTo(new[] { "win", null }));
                Assert.That(service.GetNodeAudio(reloaded, BattleEvent, win).Select(audioFile => audioFile.WavPackFilePath), Is.EqualTo(new[] { FirstWav }));
                Assert.That(reloaded.Music.RandomSequences.Select(sequence => sequence.TreeNodeId).Distinct(), Is.EqualTo(new[] { win }));
            });
        }

        (IMusicAudioProjectService Service, AudioProjectFile AudioProject) ProjectWithABattleCulture()
        {
            var service = Service();
            var audioProject = service.CreateNew();
            service.AddCulture(audioProject, BattleCulture("First_Culture", "first_sc"));
            return (service, audioProject);
        }

        IMusicScriptBuildService Build() => _provider.GetRequiredService<IMusicScriptBuildService>();

        IMusicAudioProjectService Service() => _provider.GetRequiredService<IMusicAudioProjectService>();

        AudioProjectFile SaveAndReload(AudioProjectFile audioProject)
        {
            const string filePath = MusicAudioProjectService.ProjectFilePath;
            var fileService = _provider.GetRequiredService<IAudioProjectFileService>();
            fileService.Save(audioProject, Path.GetFileName(filePath), filePath);
            return fileService.DeserialiseAudioProject(_packFileService.FindFile(filePath));
        }

        const string FirstWav = @"audio\wwise\first.wav";
        const string SecondWav = @"audio\wwise\second.wav";

        /// <summary>A culture in the battle culture slot only, bringing its own audio.</summary>
        static MusicCulture BattleCulture(string audioState, string subcultureKey) => new()
        {
            AudioState = audioState,
            SubcultureKey = subcultureKey,
            Slots = [new MusicCultureSlot { EventPrefix = "music_b_faction_" }]
        };

        /// <summary>A culture in every slot of both scripts, bringing its own audio.</summary>
        static MusicCulture Culture(string audioState, string subcultureKey) => new()
        {
            AudioState = audioState,
            SubcultureKey = subcultureKey,
            Slots = MusicDatCultureWiring.FindSlots(Load("battle_music.dat"))
                .Concat(MusicDatCultureWiring.FindSlots(Load("campaign_music.dat")))
                .Select(slot => new MusicCultureSlot { EventPrefix = slot.EventPrefix })
                .ToList()
        };

        static MusicDatFile WiredByHand(string name, IEnumerable<MusicCulture> cultures, Func<MusicCulture, string> matchKey)
        {
            var file = Load(name);
            foreach (var culture in cultures)
            {
                var plans = culture.Slots.Select(slot => new MusicDatCultureWiring.SlotPlan(slot.EventPrefix, true, slot.ReferenceMatchKey)).ToList();
                Assert.That(MusicDatCultureWiring.AddCulture(file, matchKey(culture), culture.MusicalCulture, plans), Is.Empty);
            }
            return file;
        }

        static byte[] Write(MusicDatFile file) => MusicDatParser.Write(file);

        static MusicDatFile Load(string name) => MusicDatFixtures.Load(name);
    }
}
