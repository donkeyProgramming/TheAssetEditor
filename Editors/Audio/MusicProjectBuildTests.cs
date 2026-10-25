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
        const string ProjectFolder = @"audio\audio_projects";
        const string ProjectName = "music";

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
        public void ACultureIsAddedToTheOneProjectRatherThanAProjectOfItsOwn()
        {
            var service = _provider.GetRequiredService<IMusicAudioProjectService>();

            var first = service.AddCulture(ProjectName, ProjectFolder, Culture("First_Culture", "first_sc"), ["music_b_faction_first_culture"]);
            var second = service.AddCulture(ProjectName, ProjectFolder, Culture("Second_Culture", "second_sc"), ["music_b_faction_second_culture"]);

            var reloaded = Reload(second.FilePath);
            Assert.Multiple(() =>
            {
                Assert.That(second.FilePath, Is.EqualTo(first.FilePath));
                Assert.That(reloaded.MusicCultures.Select(culture => culture.AudioState), Is.EqualTo(new[] { "First_Culture", "Second_Culture" }));
                Assert.That(reloaded.GetActionEvents().Select(actionEvent => actionEvent.Name),
                    Is.EquivalentTo(new[] { "music_b_faction_first_culture", "music_b_faction_second_culture" }));
            });
        }

        // Running the wizard again for a culture updates it: one entry, and its events are not
        // added a second time.
        [Test]
        public void AddingTheSameCultureAgainReplacesIt()
        {
            var service = _provider.GetRequiredService<IMusicAudioProjectService>();
            service.AddCulture(ProjectName, ProjectFolder, Culture("First_Culture", "first_sc"), ["music_b_faction_first_culture"]);

            var changed = Culture("First_Culture", "first_sc");
            changed.Slots.RemoveAt(0);
            var result = service.AddCulture(ProjectName, ProjectFolder, changed, ["music_b_faction_first_culture"]);

            var reloaded = Reload(result.FilePath);
            Assert.Multiple(() =>
            {
                Assert.That(result.CreatedEvents, Is.Empty);
                Assert.That(reloaded.MusicCultures, Has.Count.EqualTo(1));
                Assert.That(reloaded.MusicCultures[0].Slots, Has.Count.EqualTo(changed.Slots.Count));
                Assert.That(reloaded.GetActionEvents(), Has.Count.EqualTo(1));
            });
        }

        IMusicScriptBuildService Build() => _provider.GetRequiredService<IMusicScriptBuildService>();

        AudioProjectFile Reload(string filePath) =>
            _provider.GetRequiredService<IAudioProjectFileService>().DeserialiseAudioProject(_packFileService.FindFile(filePath));

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
