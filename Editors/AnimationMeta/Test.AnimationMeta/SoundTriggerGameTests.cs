using System.Diagnostics;
using System.IO;
using Editors.AnimationMeta.SuperView;
using Editors.Audio.SoundTriggers;
using Editors.Shared.Core.Common;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Commands;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Test.TestingUtility.Shared;

namespace Test.AnimationMeta
{
    public class SoundTriggerGameTests
    {
        static readonly string[] Packs = ["db.pack", "audio_base_bnk.pack", "audio_base.pack", "audio_en_bnk.pack", "audio_en.pack", "anim2.pack"];

        const string AttackSndMeta = "hu1_sws_attack_01.";
        const string AttackFolder = "humanoid01/sword_and_shield/attacks/";

        AssetEditorTestRunner _runner = null!;
        SoundTriggerResolver _resolver = null!;
        AudioMetadataTags _tags = null!;
        TimeSpan _loadTime;
        static readonly IReadOnlyDictionary<uint, uint> NoChoices = new Dictionary<uint, uint>();

        [OneTimeSetUp]
        public void LoadTheGamesAudio()
        {
            var data = Environment.GetEnvironmentVariable("WH3_DATA");
            Assume.That(data != null && Directory.Exists(data), "set WH3_DATA to the game's data folder");

            _runner = new AssetEditorTestRunner();
            var loader = _runner.ServiceProvider.GetRequiredService<IPackFileContainerLoader>();
            foreach (var pack in Packs)
            {
                var container = loader.CreateFromPackFile(PackFileContainerType.Normal, Path.Combine(data!, pack), true);
                container.IsCaPackFile = true;
                _runner.PackFileService.AddContainer(container);
            }

            var clock = Stopwatch.StartNew();
            var superView = (SuperViewViewModel)_runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.SuperView_Editor);
            _resolver = superView.LoadSounds(CancellationToken.None).GetAwaiter().GetResult();
            _loadTime = clock.Elapsed;
            _tags = AudioMetadataTags.Read(_runner.PackFileService);
        }

        static IReadOnlyList<uint> Leaves(SoundTriggerResult result) => [.. SoundNodes.Leaves(result.Sound!)];

        [Test]
        public void VanillaPainCry_Resolves()
        {
            Assert.That(_tags.Find("Vocalisation_Humanoid_Male_Pain")?.BattleStart, Is.EqualTo("Battle_Individual_Vocalisation_Humanoid_Male_Pain"));

            var result = _resolver.Resolve("Vocalisation_Humanoid_Male_Pain", false, NoChoices);

            Assert.That(result.Silent, Is.Null);
            Assert.That(Leaves(result).Any(x => FindWem(x) != null), Is.True);
        }

        [Test]
        public void DropWeaponRest_UsesTheTable()
        {
            Assert.That(_tags.Find("Drop_Weapon_Rest")?.BattleStart, Is.EqualTo("Battle_Individual_Melee_Weapon_Drop_Weapon_Rest"));

            var result = _resolver.Resolve("Drop_Weapon_Rest", false, NoChoices);

            Assert.That(result.Silent, Is.Null);
            Assert.That(Leaves(result).Any(x => FindWem(x) != null), Is.True);
        }

        [Test]
        public void MadeUpTag_IsSilent()
        {
            Assert.That(_resolver.Resolve("Derpy_Made_Up_Tag", false, NoChoices).Silent, Is.EqualTo("no audio_metadata_tags_tables row"));
        }

        [Test]
        public void MeleeWeaponHitPrimary_IsNotSupported_AndSaysWhatItReaches()
        {
            var result = _resolver.Resolve("Melee_Weapon_Hit_Primary", false, NoChoices);
            TestContext.Out.WriteLine(result.Silent);

            Assert.That(result.Silent, Does.StartWith("not supported: 0x2303 action -> ").And.EndWith("Dialogue_Event"));
        }

        PackFile? FindWem(uint sourceId) => _runner.PackFileService.FindFile($"audio\\wwise\\{sourceId}.wem")
            ?? _runner.PackFileService.FindFile($"audio\\wwise\\english(uk)\\{sourceId}.wem");

        [Test]
        public void RealClip_ResolvesAndDecodes_OffTheUiThread()
        {
            var file = _runner.PackFileService.GetAllPackfileContainers().SelectMany(x => x.SearchFiles(AttackSndMeta, [".snd.meta"]))
                .Where(x => x.Path.Replace('\\', '/').Contains(AttackFolder)).OrderBy(x => x.Path).FirstOrDefault();
            Assert.That(file.File, Is.Not.Null, $"the game has a .snd.meta named {AttackSndMeta}* under {AttackFolder}");
            var (ui, before) = (new UiThread(), SynchronizationContext.Current);
            SynchronizationContext.SetSynchronizationContext(ui);
            try
            {
                var superView = (SuperViewViewModel)_runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.SuperView_Editor);
                var mixer = new SoundTriggerMixer(openDevice: false);
                superView.OpenMixer = () => mixer;
                superView.LoadSounds = _ => Task.FromResult(_resolver);
                var (prepare, queued) = (superView.PrepareSound, new List<uint>());
                superView.PrepareSound = id =>
                {
                    queued.Add(id);
                    return prepare(id);
                };
                superView.SoundOn = true;
                ui.Settle(superView);
                var memory = GC.GetTotalMemory(true);

                var onThisThread = Stopwatch.StartNew();
                var main = superView.SceneObjects[0].Data;
                main.SoundMetaData = file.File;
                _runner.ScopeRepository.GetRequiredService<SceneObjectEditor>(superView).SetMetaFile(main, main.MetaData, main.PersistMetaData);
                onThisThread.Stop();
                var firstResolves = Stopwatch.StartNew();
                Assert.That(superView.SoundDecoding.Wait(TimeSpan.FromMinutes(2)), Is.True, "resolved within two minutes");
                firstResolves.Stop();
                Pump();
                var whileDecoding = superView.SoundStatus.LastOrDefault();
                var decoding = Stopwatch.StartNew();
                do
                    Assert.That(superView.SoundDecoding.Wait(TimeSpan.FromMinutes(2)), Is.True, "decoded within two minutes");
                while (Pump());
                decoding.Stop();

                var sources = queued.Distinct().ToList();
                var decoded = sources.Count(id => mixer.Prepare(id, () => null));
                var triggers = _runner.ScopeRepository.GetRequiredService<MetaDataFileParser>(superView).ParseFile(file.File.DataSource.ReadData()).Attributes.Count(x => x is SoundTrigger_v10 or SoundTrigger_v4);
                TestContext.Out.WriteLine($"The fixture's LoadSounds took {_loadTime.TotalSeconds:0.0} s. {file.Path}: {triggers} triggers, {sources.Count} sources. " +
                    $"On the UI thread: {onThisThread.Elapsed.TotalMilliseconds:0} ms. Off it: the first resolves {firstResolves.Elapsed.TotalMilliseconds:0} ms, " +
                    $"{decoded} decoded in {decoding.Elapsed.TotalMilliseconds:0} ms, {(GC.GetTotalMemory(true) - memory) / 1_048_576.0:0} MB. " +
                    string.Join(" | ", superView.SoundStatus));
                Assert.That(triggers, Is.GreaterThan(0));
                Assert.That(decoded, Is.GreaterThan(0), "real WEMs decode");
                Assert.That(onThisThread.Elapsed.TotalMilliseconds, Is.LessThan(1000), "the first resolves and the decoding are not on the UI thread");
                Assert.That((firstResolves.Elapsed + onThisThread.Elapsed).TotalMilliseconds, Is.GreaterThan(1000), "the clip was cold, or this measures nothing");
                Assert.That(whileDecoding, Is.EqualTo("Decoding sounds..."), "the status says so while the decoding runs");
                Assert.That(superView.SoundStatus, Has.None.EqualTo("Decoding sounds..."), "and not after");
                GC.KeepAlive(mixer);

                bool Pump()
                {
                    onThisThread.Start();
                    var ran = ui.RunPosted();
                    onThisThread.Stop();
                    return ran;
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(before);
            }
        }
    }
}
