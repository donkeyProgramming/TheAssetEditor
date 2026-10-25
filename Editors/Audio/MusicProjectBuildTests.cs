using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.MusicDat;

namespace Test.Audio
{
    // The music project is the one file a mixer mod's maintainer keeps: every culture is added to
    // it, and the scripts are rebuilt from vanilla with all of them. These run on the .dat fixtures
    // in Data/Music standing in for the game's copies, so they need no install.
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
            _packFileService.AddFilesToPack(game,
            [
                new NewPackFileEntry(@"audio\scripts", PackFile.CreateFromBytes("battle_music.dat", Fixture("battle_music.dat"))),
                new NewPackFileEntry(@"audio\scripts", PackFile.CreateFromBytes("campaign_music.dat", Fixture("campaign_music.dat")))
            ]);
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
                Assert.That(reloaded.MusicCultures.Select(culture => culture.AudioState), Is.EqualTo(new[] { "First_Culture", "Second_Culture" }));
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
                Assert.That(audioProject.MusicCultures.Select(culture => culture.AudioState), Is.EqualTo(new[] { "First_Culture", "Second_Culture" }));
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
            Assert.That(soundBank.MusicRandomSequences, Has.Count.GreaterThan(1), "expected a branch per battle container");

            service.RemoveCulture(audioProject, culture);

            Assert.Multiple(() =>
            {
                Assert.That(audioProject.MusicCultures, Is.Empty);
                Assert.That(soundBank.ActionEvents, Is.Empty);
                Assert.That(soundBank.MusicRandomSequences, Is.Empty);
                Assert.That(soundBank.MusicSegments, Is.Empty);
                Assert.That(audioProject.AudioFiles, Is.Empty);
                Assert.That(audioProject.StateGroups.SelectMany(stateGroup => stateGroup.States), Is.Empty);
            });
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

        const string FirstWav = @"audio\wwiseirst.wav";
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

        static MusicDatFile Load(string name) => MusicDatParser.Parse(Fixture(name));

        static byte[] Fixture(string name)
        {
            for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, "Data", "Music", name);
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            throw new FileNotFoundException($"Data\\Music\\{name} not found above {TestContext.CurrentContext.TestDirectory}");
        }
    }
}
