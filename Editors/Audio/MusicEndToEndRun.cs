using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Core.AudioProjectMutation;
using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.MusicDatEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.MusicDat;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Test.Audio
{
    /// <summary>
    /// One run of the whole thing, in the order a modder does it: the MusicDat wizard wires a new
    /// culture into both music scripts and writes the audio project holding the events they now
    /// post, then the Audio Editor gives those events audio and compiles them.
    ///
    /// The unit tests all work on hircs in memory, and a bank that is perfectly well formed is still
    /// silent if no script posts its event. What this run adds is the join: the .dat and the .bnk
    /// have to agree on a name, and they end up in one pack together or the mod is half a mod.
    /// It cannot show the game plays it; that still needs a person and a copy of WH3.
    /// </summary>
    [Explicit("End to end run, needs a WH3 install and a wav on disk, run manually")]
    internal class MusicEndToEndRun
    {
        const string GameDirectory = @"D:\SteamLibrary\steamapps\common\Total War WARHAMMER III";
        const string WavDiskFilePath = @"C:\Users\rob\Desktop\tw modding\projects\araby audio projects\music\241 Adrian von Ziegler - Hual Hadi.wav";

        // What a modder types into the wizard. The Audio State is also what event names are built
        // from, lower-cased; the subculture key is what campaign_music.dat matches against.
        const string AudioState = "Araby";
        const string SubcultureKey = "ovn_sc_arb_araby";

        /// <summary>The wizard lower-cases the Audio State before building event names and States off
        /// it, since that is what the shipped scripts do.</summary>
        const string MusicalCulture = "araby";

        const string AudioProjectFolder = @"audio\audio_projects";
        const uint CampaignSubcultureContainerId = 698158058;
        const uint BattleCultureContainerId = 26264058;
        const uint InBattleContainerId = 67383790;

        static string OutputDirectory => Path.Combine(Path.GetTempPath(), "ArabyMusicRun");

        [Test]
        public void AddArabyThroughTheWizardThenGiveItAudioAndCompile()
        {
            using var provider = VanillaBankReader.CreateProvider(GameDirectory);
            var packFileService = provider.GetRequiredService<IPackFileService>();
            var containerLoader = provider.GetRequiredService<IPackFileContainerLoader>();

            foreach (var packName in new[] { "audio_base_bnk.pack", "audio_base.pack" })
            {
                var container = containerLoader.CreateFromPackFile(
                    PackFileContainerType.Normal, Path.Combine(GameDirectory, "data", packName), true);
                container.IsCaPackFile = true;
                packFileService.AddContainer(container);
            }

            // Everything the run produces goes here, the same as the pack a modder has selected for
            // edit while working.
            var modPack = packFileService.CreateNewPackFileContainer(
                "araby_music", PackFileVersion.PFH5, PackFileCAType.MOD, setEditablePack: true);

            var wizard = RunTheWizard(packFileService);
            var audioProjectPath = WriteTheAudioProject(provider, wizard);

            var audioFile = ImportTheWav(packFileService, provider.GetRequiredService<IAudioRepository>());
            var compiledProject = GiveEveryEventTheWavAndCompile(provider, audioProjectPath, audioFile);

            ReportPackContents(modPack);
            AssertTheScriptsAndTheBankAgree(packFileService, compiledProject);
            AssertTheMergedContainersKeepVanilla(packFileService, provider.GetRequiredService<IAudioRepository>(), compiledProject);
            AssertEachSegmentCarriesTheCuesItsScriptReads(packFileService, compiledProject);
            AssertThePulseTracksCarryTheNewCulture(packFileService, provider.GetRequiredService<IAudioRepository>(), compiledProject);
            AssertTheFragmentsReachTheNewCulturesAudio(packFileService, provider.GetRequiredService<IAudioRepository>(), compiledProject);

            Directory.CreateDirectory(OutputDirectory);
            var packDiskPath = Path.Combine(OutputDirectory, "araby_music.pack");
            packFileService.SavePackContainer(modPack, packDiskPath, false,
                GameInformationDatabase.GetGameById(GameTypeEnum.Warhammer3));

            Console.WriteLine($"\nPack written to {packDiskPath}");
        }

        /// <summary>
        /// The Add Culture wizard, driven the way the window drives it. It only validates here:
        /// the scripts are built from the music project when it is compiled, the same as a rebuild.
        /// </summary>
        static AddCultureWizardViewModel RunTheWizard(IPackFileService packFileService)
        {
            var battlePackFile = FindMusicDat(packFileService, "battle_music.dat");
            var campaignPackFile = FindMusicDat(packFileService, "campaign_music.dat");

            var wizard = new AddCultureWizardViewModel(
                MusicDatParser.Parse(battlePackFile), MusicDatParser.Parse(campaignPackFile))
            {
                AudioState = AudioState,
                SubcultureKey = SubcultureKey
            };

            // Every row left at its default, which is included and authoring its own audio - the
            // most demanding plan the wizard can be given.
            Assert.That(wizard.CanApply, Is.True);
            Assert.That(wizard.TryApply(), Is.True, wizard.Error);

            Console.WriteLine($"Wizard wants audio for: {string.Join(", ", wizard.EventsNeedingAudio)}");
            return wizard;
        }

        static string WriteTheAudioProject(ServiceProvider provider, AddCultureWizardViewModel wizard)
        {
            var result = provider.GetRequiredService<IMusicAudioProjectService>()
                .AddCulture(AddCultureWizardViewModel.AudioProjectName, AudioProjectFolder, wizard.ToMusicCulture(), wizard.EventsNeedingAudio);

            Assert.That(result.SkippedEvents, Is.Empty,
                string.Join("; ", result.SkippedEvents.Select(skipped => $"{skipped.EventName}: {skipped.Reason}")));
            Assert.That(result.CreatedEvents, Is.EquivalentTo(wizard.EventsNeedingAudio));
            Assert.That(result.MusicCultures.Select(culture => culture.SubcultureKey), Is.EqualTo(new[] { SubcultureKey }));

            return result.FilePath;
        }

        static AudioFile ImportTheWav(IPackFileService packFileService, IAudioRepository audioRepository)
        {
            // A wav has to be in the pack before it can be picked, the same as importing one in the
            // editor - the wem generator reads it from there, not from disk.
            const string wavPackFileName = "araby_music.wav";
            PackFileUtil.LoadFileFromDisk(packFileService,
                new PackFileUtil.FileRef(WavDiskFilePath, "audio\\wwise", wavPackFileName));

            var wavPackFilePath = $"audio\\wwise\\{wavPackFileName}";
            Assert.That(packFileService.FindFile(wavPackFilePath), Is.Not.Null, "the wav was not added to the mod pack");

            var language = Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx);
            audioRepository.Load([language]);

            var ids = IdGenerator.GenerateIds(audioRepository.GetUsedVanillaSourceIdsByLanguageId(WwiseHash.Compute(language)));
            return new AudioFile(ids.Guid, ids.Id, wavPackFileName, wavPackFilePath);
        }

        /// <summary>
        /// Opens the wizard's audio project and gives every event the same wav, which at the service
        /// level is what editing a row in the viewer does: the row is removed and re-added, this
        /// time with audio picked in the Audio Files Explorer.
        /// </summary>
        static AudioProjectFile GiveEveryEventTheWavAndCompile(ServiceProvider provider, string audioProjectPath, AudioFile audioFile)
        {
            var fileName = Path.GetFileName(audioProjectPath);
            var loaded = provider.GetRequiredService<IAudioProjectFileService>().Load(fileName, audioProjectPath);

            var stateService = provider.GetRequiredService<IAudioEditorStateService>();
            stateService.StoreAudioProject(loaded.AudioProject);
            stateService.StoreAudioProjectFileName(fileName);

            var actionEventService = provider.GetRequiredService<IActionEventService>();
            var musicSoundBank = loaded.AudioProject.SoundBanks.Single(soundBank => soundBank.ActionEvents.Count != 0);
            var actionEventTypeName = Wh3ActionEventInformation.GetName(Wh3ActionEventType.Music);

            foreach (var actionEvent in musicSoundBank.ActionEvents.ToList())
            {
                var setStateAction = actionEvent.Actions.Single();
                actionEventService.RemoveActionEvent(actionEventTypeName, actionEvent.Name);
                actionEventService.AddSetStateActionEvent(actionEventTypeName, actionEvent.Name,
                    setStateAction.StateGroupName, setStateAction.StateName, [audioFile]);
            }

            // The editor compiles the cleaned project, not the live one.
            var compiledProject = loaded.AudioProject.Clean();
            provider.GetRequiredService<IAudioProjectCompilerService>()
                .Compile(compiledProject, fileName, audioProjectPath);

            return compiledProject;
        }

        /// <summary>
        /// The join between the two halves. Every event the scripts now post has to exist as an
        /// Event hirc in a compiled .bnk, or the script posts a name into nothing - and in the .bnk
        /// its vanilla siblings live in, or the context that posts it never has it loaded.
        /// </summary>
        static void AssertTheScriptsAndTheBankAgree(IPackFileService packFileService, AudioProjectFile compiledProject)
        {
            var hircsByBank = ShippedBanks(packFileService).ToDictionary(path => path, path => ReadHircs(packFileService, path));
            var hircs = hircsByBank.Values.SelectMany(bankHircs => bankHircs).ToList();

            var battle = MusicDatParser.Parse(packFileService.FindFile(@"audio\scripts\battle_music.dat"));
            var campaign = MusicDatParser.Parse(packFileService.FindFile(@"audio\scripts\campaign_music.dat"));

            var postedEvents = PostedEventsFor(battle, AudioState.ToLowerInvariant())
                .Concat(PostedEventsFor(campaign, SubcultureKey))
                .ToList();

            Assert.That(postedEvents, Is.Not.Empty, "the patched scripts post nothing for the new culture");

            Assert.Multiple(() =>
            {
                foreach (var eventName in postedEvents)
                {
                    var eventHirc = hircs.FirstOrDefault(hirc => hirc.Id == WwiseHash.Compute(eventName));
                    Assert.That(eventHirc, Is.Not.Null, $"the scripts post '{eventName}' but no .bnk defines it");
                    Assert.That(eventHirc.HircType, Is.EqualTo(AkBkHircType.Event));

                    var expectedBank = eventName.StartsWith("music_b_", StringComparison.Ordinal)
                        ? "battle_music__core.bnk"
                        : "campaign_music__core.bnk";
                    Assert.That(hircsByBank.Single(bank => bank.Value.Contains(eventHirc)).Key, Does.EndWith(expectedBank),
                        $"'{eventName}' is not in the .bnk its vanilla siblings live in");
                }
            });

            Console.WriteLine($"\nScripts post {postedEvents.Count} event(s):");
            foreach (var eventName in postedEvents)
                Console.WriteLine($"  {eventName} -> {WwiseHash.Compute(eventName)} in " +
                    hircsByBank.Single(bank => bank.Value.Any(hirc => hirc.Id == WwiseHash.Compute(eventName))).Key);
        }

        /// <summary>
        /// The shipped .bnks replace the vanilla ones outright, so a branch lost in the merge is not
        /// merely missing - it is a culture that had music and now has none.
        /// </summary>
        static void AssertTheMergedContainersKeepVanilla(
            IPackFileService packFileService, IAudioRepository audioRepository, AudioProjectFile compiledProject)
        {
            var musicSoundBank = compiledProject.SoundBanks.Single(soundBank => soundBank.MusicRandomSequences.Count != 0);
            var containerIds = musicSoundBank.MusicRandomSequences
                .Select(randomSequence => randomSequence.DirectParentId)
                .Distinct()
                .ToList();

            Assert.That(containerIds, Is.EquivalentTo(new[] { CampaignSubcultureContainerId, BattleCultureContainerId, InBattleContainerId }),
                "expected a branch in the campaign subculture container and both battle culture containers");

            // Only the ids the mod invented are its own to place. Vanilla's children stay wherever
            // vanilla put them, so they are not something a testing .bnk has to carry.
            var newHircIds = musicSoundBank.MusicRandomSequences
                .Select(randomSequence => randomSequence.Id)
                .ToHashSet();

            var testingBankPaths = ShippedBanks(packFileService);

            var hircsByTestingBank = testingBankPaths
                .ToDictionary(path => path, path => ReadHircs(packFileService, path));

            // Looked up in the .bnk vanilla's copy came from: some containers are defined in more
            // than one vanilla .bnk, and only that one is merged.
            CAkMusicSwitchCntr_V136 MergedContainer(CAkMusicSwitchCntr_V136 vanilla) => hircsByTestingBank
                .Where(bank => string.Equals(bank.Key.Replace('/', '\\'), vanilla.BnkFilePath.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase))
                .SelectMany(bank => bank.Value)
                .OfType<CAkMusicSwitchCntr_V136>()
                .SingleOrDefault(container => container.Id == vanilla.Id);

            Assert.Multiple(() =>
            {
                foreach (var containerId in containerIds)
                {
                    var vanilla = (CAkMusicSwitchCntr_V136)audioRepository.GetHircs(AkBkHircType.Music_Switch)
                        .First(hirc => hirc.Id == containerId && hirc.IsCA);

                    var merged = MergedContainer(vanilla);
                    Assert.That(merged, Is.Not.Null, $"no shipped .bnk carries container {containerId}");
                    if (merged == null)
                        continue;

                    // The tree is only readable against the arguments and depth declared alongside
                    // it, so the copy the merge builds has to carry them over unchanged.
                    Assert.That(merged.TreeDepth, Is.EqualTo(vanilla.TreeDepth));
                    Assert.That(merged.Arguments.Select(argument => argument.GroupId),
                        Is.EqualTo(vanilla.Arguments.Select(argument => argument.GroupId)));

                    AssertVanillaBranchesSurvive(vanilla.AkDecisionTree.DecisionTree, merged.AkDecisionTree.DecisionTree, containerId);

                    // The culture has to be in every block where vanilla tells cultures apart: three
                    // of the in-battle container's phases list every culture with no default, so a
                    // block it misses is a battle phase where it gets silence.
                    var cultureLevel = vanilla.Arguments.FindIndex(argument => argument.GroupId == WwiseHash.Compute("Battle_Music_WH3_Culture"));
                    if (cultureLevel != -1)
                    {
                        var missing = CultureBlocks(merged.AkDecisionTree.DecisionTree, cultureLevel)
                            .Where(block => block.Any(key => key != 0) && !block.Contains(WwiseHash.Compute(MusicalCulture)))
                            .Count();
                        Assert.That(missing, Is.Zero, $"container {containerId}: {missing} culture block(s) have no '{MusicalCulture}' branch");
                    }

                    // Nothing the tree names plays unless the container also claims it as a child,
                    // and a missing child is silent rather than an error - the container just takes
                    // its default, which is vanilla's music under a new culture's name.
                    var childIds = merged.MusicTransNodeParams.MusicNodeParams.Children.ChildIds;
                    var orphans = LeavesOf(merged.AkDecisionTree.DecisionTree)
                        .Where(audioNodeId => audioNodeId != 0 && !childIds.Contains(audioNodeId))
                        .Distinct()
                        .ToList();

                    Assert.That(orphans, Is.Empty, $"container {containerId} names nodes it does not claim as children");

                    // Vanilla keeps every child of these two containers in the same .bnk as the
                    // container, so a testing .bnk that carries the container without the hierarchy
                    // under it is not a shape the game is ever asked to load.
                    var bankHircIds = hircsByTestingBank
                        .Single(bank => bank.Value.Contains(merged))
                        .Value.Select(hirc => hirc.Id)
                        .ToHashSet();

                    var elsewhere = LeavesOf(merged.AkDecisionTree.DecisionTree)
                        .Where(audioNodeId => audioNodeId != 0 && newHircIds.Contains(audioNodeId) && !bankHircIds.Contains(audioNodeId))
                        .Distinct()
                        .ToList();

                    Assert.That(elsewhere, Is.Empty,
                        $"container {containerId} names new nodes that are not in its own .bnk");
                }
            });

            foreach (var path in testingBankPaths)
                Console.WriteLine($"\n{path}: {hircsByTestingBank[path].Count} hircs");
        }

        /// <summary>
        /// The other half of what the wizard's rows can now carry: the three pulse Groups, which are
        /// served by adding a sub-track to vanilla switch tracks rather than by merging a branch.
        /// Every vanilla track for the Group has to get one, since a culture that only appears in
        /// some of them falls back to Cathay for the rest of the loop.
        /// </summary>
        static void AssertThePulseTracksCarryTheNewCulture(
            IPackFileService packFileService, IAudioRepository audioRepository, AudioProjectFile compiledProject)
        {
            var musicSoundBank = compiledProject.SoundBanks.Single(soundBank => soundBank.AmsPulses.Count != 0);
            var stateId = WwiseHash.Compute(MusicalCulture);

            // Some tracks are defined in more than one vanilla .bnk, so each is looked up in the
            // shipped copy of the .bnk vanilla's came from - the only one the compile edits.
            var hircsByBank = new Dictionary<string, List<HircItem>>(StringComparer.OrdinalIgnoreCase);
            CAkMusicTrack_V136 MergedTrack(CAkMusicTrack_V136 vanillaTrack)
            {
                if (!hircsByBank.TryGetValue(vanillaTrack.BnkFilePath, out var hircs))
                    hircsByBank[vanillaTrack.BnkFilePath] = hircs = ShippedHircsReplacing(packFileService, vanillaTrack.BnkFilePath);
                return hircs.OfType<CAkMusicTrack_V136>().SingleOrDefault(track => track.Id == vanillaTrack.Id);
            }

            Assert.Multiple(() =>
            {
                foreach (var amsPulse in musicSoundBank.AmsPulses)
                {
                    var vanillaTracks = audioRepository.GetVanillaAmsPulseTracks(amsPulse.StateGroupName);
                    Assert.That(vanillaTracks, Is.Not.Empty, $"no vanilla switch track reads {amsPulse.StateGroupName}");

                    foreach (var vanillaTrack in vanillaTracks)
                    {
                        var mergedTrack = MergedTrack(vanillaTrack);
                        Assert.That(mergedTrack, Is.Not.Null, $"{amsPulse.StateGroupName}: track {vanillaTrack.Id} was not re-emitted");
                        if (mergedTrack == null)
                            continue;

                        var subTrack = mergedTrack.SwitchParams!.SwitchAssoc.IndexOf(stateId);

                        Assert.That(subTrack, Is.Not.EqualTo(-1),
                            $"{amsPulse.StateGroupName}: track {vanillaTrack.Id} has no sub-track for '{MusicalCulture}'");
                        Assert.That(mergedTrack.PlaylistList.Any(clip => clip.TrackId == subTrack), Is.True,
                            $"{amsPulse.StateGroupName}: the new sub-track on track {vanillaTrack.Id} has no clip");

                        // The vanilla cultures have to keep the index they had, or the merged track
                        // plays the wrong stem for every culture below the one that moved.
                        for (var index = 0; index < vanillaTrack.SwitchParams!.SwitchAssoc.Count; index++)
                            Assert.That(mergedTrack.SwitchParams.SwitchAssoc[index],
                                Is.EqualTo(vanillaTrack.SwitchParams.SwitchAssoc[index]),
                                $"track {vanillaTrack.Id}: sub-track {index} changed culture");
                    }

                    Console.WriteLine($"\n{amsPulse.StateGroupName}: {vanillaTracks.Count} track(s) given a '{MusicalCulture}' sub-track");
                }
            });
        }

        /// <summary>
        /// The last of the four AMS Groups, and the one that builds a hierarchy rather than adding to
        /// a vanilla one. Walked from the vanilla faction container all the way down to the wem,
        /// because every link in it is generated and a break anywhere is silence rather than an error.
        /// </summary>
        static void AssertTheFragmentsReachTheNewCulturesAudio(
            IPackFileService packFileService, IAudioRepository audioRepository, AudioProjectFile compiledProject)
        {
            var musicSoundBank = compiledProject.SoundBanks.Single(soundBank => soundBank.AmsFragments.Count != 0);
            var amsFragment = musicSoundBank.AmsFragments.Single();

            var vanillaContainer = audioRepository.GetHircs(amsFragment.FactionSwitchContainerId)
                .OfType<CAkSwitchCntr_V136>()
                .Single(container => container.IsCA);

            var mergedContainer = ShippedHircsReplacing(packFileService, vanillaContainer.BnkFilePath)
                .OfType<CAkSwitchCntr_V136>()
                .Single(container => container.Id == amsFragment.FactionSwitchContainerId);

            // The key switch and the sounds are new ids, spliced into the same .bnk as the container.
            var moddedHircs = ShippedBanks(packFileService).SelectMany(path => ReadHircs(packFileService, path)).ToList();
            var keySwitch = moddedHircs.OfType<CAkSwitchCntr_V136>().Single(c => c.Id == amsFragment.KeySwitchContainerId);

            Assert.Multiple(() =>
            {
                var cultureSwitch = mergedContainer.SwitchList
                    .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                    .SingleOrDefault(package => package.SwitchId == WwiseHash.Compute(MusicalCulture));

                Assert.That(cultureSwitch, Is.Not.Null, $"the faction container has no switch for '{MusicalCulture}'");
                Assert.That(cultureSwitch.NodeIdList, Is.EqualTo(new[] { amsFragment.KeySwitchContainerId }));
                Assert.That(mergedContainer.Children.ChildIds, Does.Contain(amsFragment.KeySwitchContainerId));

                // Vanilla's cultures have to survive; this .bnk replaces campaign_music__core's copy.
                foreach (var vanillaSwitch in vanillaContainer.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>())
                    Assert.That(mergedContainer.SwitchList
                        .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                        .Any(package => package.SwitchId == vanillaSwitch.SwitchId
                            && package.NodeIdList.SequenceEqual(vanillaSwitch.NodeIdList)),
                        Is.True, $"vanilla switch {vanillaSwitch.SwitchId} was lost or re-pointed");

                // The key switch has to branch on the same Group vanilla's siblings do, or the game
                // looks up a State Group nothing ever sets and always takes the default.
                var vanillaSibling = vanillaContainer.Children.ChildIds
                    .SelectMany(childId => audioRepository.GetHircs(childId))
                    .OfType<CAkSwitchCntr_V136>()
                    .First(child => child.SwitchList.Count != 0);

                Assert.That(keySwitch.GroupId, Is.EqualTo(vanillaSibling.GroupId));
                Assert.That(keySwitch.NodeBaseParams.DirectParentId, Is.EqualTo(amsFragment.FactionSwitchContainerId));
                Assert.That(keySwitch.SwitchList, Has.Count.EqualTo(vanillaSibling.SwitchList.Count));

                foreach (var keyPackage in keySwitch.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>())
                    Assert.That(keyPackage.NodeIdList, Is.EqualTo(new[] { amsFragment.TargetHircId }));

                // And the bottom of the chain: whatever the keys point at has to be in the bank.
                Assert.That(moddedHircs.Any(hirc => hirc.Id == amsFragment.TargetHircId), Is.True,
                    $"the key switch points at {amsFragment.TargetHircId}, which is in no .bnk");

                foreach (var soundId in amsFragment.SoundIds)
                    Assert.That(moddedHircs.Any(hirc => hirc.Id == soundId && hirc.HircType == AkBkHircType.Sound), Is.True,
                        $"Sound {soundId} was not generated");
            });

            Console.WriteLine(
                $"\nfragments: '{MusicalCulture}' -> key switch {amsFragment.KeySwitchContainerId} " +
                $"({keySwitch.SwitchList.Count} keys) -> {amsFragment.TargetHircId} -> {amsFragment.SoundIds.Count} sound(s)");
        }

        /// <summary>
        /// battle_music.dat runs its state machine off track start and track end cues and ignores
        /// the campaign's Update_PreBattle, so the copy of the music under the in-battle container
        /// has to carry those and every other copy keeps the campaign's cue.
        /// </summary>
        static void AssertEachSegmentCarriesTheCuesItsScriptReads(IPackFileService packFileService, AudioProjectFile compiledProject)
        {
            var musicSoundBank = compiledProject.SoundBanks.Single(soundBank => soundBank.MusicRandomSequences.Count != 0);
            // Only the mod's own: vanilla defines some of its segments in more than one .bnk.
            var modSegmentIds = musicSoundBank.MusicSegments.Select(musicSegment => musicSegment.Id).ToHashSet();
            var shippedSegments = ShippedBanks(packFileService)
                .SelectMany(path => ReadHircs(packFileService, path))
                .OfType<CAkMusicSegment_V136>()
                .Where(segment => modSegmentIds.Contains(segment.Id))
                .ToLookup(segment => segment.Id);

            Assert.Multiple(() =>
            {
                foreach (var musicRandomSequence in musicSoundBank.MusicRandomSequences)
                {
                    var expected = musicRandomSequence.DirectParentId == InBattleContainerId
                        ? new[] { Wh3MusicHierarchyInformation.TrackStartCueName, Wh3MusicHierarchyInformation.TrackEndCueName }
                        : [Wh3MusicHierarchyInformation.CustomCueName];

                    foreach (var musicSegment in musicSoundBank.GetMusicSegments(musicRandomSequence))
                    {
                        Assert.That(shippedSegments[musicSegment.Id].Count(), Is.EqualTo(1),
                            $"segment {musicSegment.Id} under container {musicRandomSequence.DirectParentId} should ship exactly once");
                        var shipped = shippedSegments[musicSegment.Id].FirstOrDefault();
                        if (shipped == null)
                            continue;

                        var named = shipped.ArrayMarkersList
                            .Where(marker => !string.IsNullOrEmpty(marker.MarkerName))
                            .Select(marker => marker.MarkerName)
                            .ToList();
                        Assert.That(named, Is.EqualTo(expected),
                            $"segment {musicSegment.Id} under container {musicRandomSequence.DirectParentId}");

                        Console.WriteLine($"segment {musicSegment.Id} under {musicRandomSequence.DirectParentId}: " +
                            string.Join(", ", shipped.ArrayMarkersList.Select(marker =>
                                $"{marker.MarkerName?.TrimEnd('\0') ?? marker.Id.ToString()} @ {marker.Position:0}")));
                    }
                }
            });
        }

        /// <summary>
        /// Every .bnk the compile put in the pack. Music ships only spliced copies of vanilla .bnks
        /// under vanilla's names - a .bnk of the mod's own beside them never overrides vanilla, and
        /// would define the same ids a second time - so anything else here is a regression.
        /// </summary>
        static List<string> ShippedBanks(IPackFileService packFileService)
        {
            var banks = packFileService.GetEditablePack().GetAllFiles()
                .Select(file => file.Key)
                .Where(path => path.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(banks.Where(path => !path.EndsWith("__core.bnk", StringComparison.OrdinalIgnoreCase)), Is.Empty,
                "the compile shipped a .bnk that is not a replacement for a vanilla one");
            return banks;
        }

        /// <summary>The hircs of the shipped .bnk that replaces the vanilla .bnk at this path.</summary>
        static List<HircItem> ShippedHircsReplacing(IPackFileService packFileService, string vanillaBnkFilePath)
        {
            var path = ShippedBanks(packFileService).SingleOrDefault(shipped =>
                string.Equals(shipped.Replace('/', '\\'), vanillaBnkFilePath.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase));

            Assert.That(path, Is.Not.Null, $"nothing replaces {vanillaBnkFilePath}");
            return ReadHircs(packFileService, path);
        }

        /// <summary>The keys of every block at one level of a tree.</summary>
        static IEnumerable<List<uint>> CultureBlocks(AkDecisionTree_V136.Node_V136 node, int level) =>
            level == 0
                ? [node.Nodes.Select(child => child.Key).ToList()]
                : node.Nodes.SelectMany(child => CultureBlocks(child, level - 1));

        static IEnumerable<uint> LeavesOf(AkDecisionTree_V136.Node_V136 node) =>
            node.Nodes.SelectMany(child => child.Nodes.Count == 0 ? [child.AudioNodeId] : LeavesOf(child));

        /// <summary>Walks both trees together, since a battle branch is a path rather than a single
        /// node and a loss further down is just as silent as one at the top.</summary>
        static void AssertVanillaBranchesSurvive(
            AkDecisionTree_V136.Node_V136 vanillaNode, AkDecisionTree_V136.Node_V136 mergedNode, uint containerId)
        {
            foreach (var vanillaChild in vanillaNode.Nodes)
            {
                var mergedChild = mergedNode.Nodes.SingleOrDefault(node => node.Key == vanillaChild.Key);
                Assert.That(mergedChild, Is.Not.Null, $"container {containerId}: vanilla branch {vanillaChild.Key} was dropped");

                if (vanillaChild.Nodes.Count == 0)
                    Assert.That(mergedChild.AudioNodeId, Is.EqualTo(vanillaChild.AudioNodeId),
                        $"container {containerId}: vanilla branch {vanillaChild.Key} now points somewhere else");
                else
                    AssertVanillaBranchesSurvive(vanillaChild, mergedChild, containerId);
            }
        }

        /// <summary>
        /// Every event name the spliced arms for this key post, read back out of the patched file.
        ///
        /// An arm names its event one of two ways and both have to be read, or this silently checks
        /// a subset: most spell the event out as a literal, while the subculture chain only stores
        /// the culture name and the script concatenates the prefix onto it at run time.
        /// </summary>
        static List<string> PostedEventsFor(MusicDatFile file, string matchKey)
        {
            return MusicDatCultureWiring.FindSlots(file)
                .SelectMany(slot => slot.Chains
                    .SelectMany(chain => chain.Cases)
                    .Where(matchCase => string.Equals(matchCase.MatchKey, matchKey, StringComparison.OrdinalIgnoreCase))
                    .Select(matchCase => matchCase.SoundEvent
                        ?? (matchCase.MusicalCulture == null ? null : slot.EventFor(matchCase.MusicalCulture))))
                .Where(eventName => eventName != null)
                .Distinct()
                .ToList()!;
        }

        static PackFile FindMusicDat(IPackFileService packFileService, string name)
        {
            var packFile = packFileService.FindAllWithExtention(".dat")
                .LastOrDefault(file => string.Equals(Path.GetFileName(file.FileName), name, StringComparison.OrdinalIgnoreCase))
                .Pack;

            Assert.That(packFile, Is.Not.Null, $"{name} was not found in the loaded packs");
            return packFile;
        }

        static List<HircItem> ReadHircs(IPackFileService packFileService, string packFilePath)
        {
            var packFile = packFileService.FindFile(packFilePath);
            Assert.That(packFile, Is.Not.Null, $"no .bnk at {packFilePath}");

            var bnkFile = BnkFile.CreateFromBytes(packFile.DataSource.ReadData(), packFilePath, false);
            return bnkFile.HircChunk.HircItems;
        }

        static void ReportPackContents(IPackFileContainer modPack)
        {
            Console.WriteLine("\nPack contents:");
            foreach (var file in modPack.GetAllFiles().OrderBy(file => file.Key))
                Console.WriteLine($"  {file.Key} ({file.Value.DataSource.Size} bytes)");
        }
    }
}
