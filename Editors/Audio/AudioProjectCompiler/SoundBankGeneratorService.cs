using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Bkhd;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.AudioProjectCompiler
{
    public interface ISoundBankGeneratorService
    {
        void GenerateSoundBankWithoutDialogueEvents(SoundBank soundBank);
        void GenerateDialogueEventsForTestingSoundBank(SoundBank soundBank);
        void GenerateMergingSoundBank(SoundBank soundBank);
        void GenerateMergedDialogueEventSoundBanks(List<string> moddedSoundBanks);
    }

    public class SoundBankGeneratorService : ISoundBankGeneratorService
    {
        private readonly IFileSaveService _fileSaveService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IAudioRepository _audioRepository;
        private readonly WwiseHircGeneratorServiceFactory _wwiseHircGeneratorServiceFactory;
        private readonly IAudioEditorIntegrityService _audioEditorIntegrityService;

        public SoundBankGeneratorService(
            IFileSaveService fileSaveService,
            ApplicationSettingsService applicationSettingsService,
            IAudioRepository audioRepository,
            IAudioEditorIntegrityService audioEditorIntegrityService)
        {
            _fileSaveService = fileSaveService;
            _applicationSettingsService = applicationSettingsService;
            _audioRepository = audioRepository;
            _audioEditorIntegrityService = audioEditorIntegrityService;

            var bankGeneratorVersion = (uint)GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame).BankGeneratorVersion;
            _wwiseHircGeneratorServiceFactory = WwiseHircGeneratorServiceFactory.CreateFactory(bankGeneratorVersion);
        }

        public void GenerateSoundBankWithoutDialogueEvents(SoundBank soundBank)
        {
            var actionEventToHircLookup = new Dictionary<ActionEvent, HircItem>();
            var hircItems = new List<HircItem>();

            if (soundBank.ActionEvents != null)
            {
                var actionEventHircs = GenerateActionEventHircs(soundBank, actionEventToHircLookup);
                hircItems.AddRange(actionEventHircs);

                var actionHircs = GenerateActionHircs(soundBank, actionEventToHircLookup);
                hircItems.AddRange(actionHircs);

                var sourceHircsFromPlay = GenerateSourceHircsFromPlayActions(soundBank);
                hircItems.AddRange(sourceHircsFromPlay);
            }

            if (soundBank.DialogueEvents != null)
            {
                // We don't generate the Dialogue Events in this .bnk, we keep them in the merging SoundBank instead
                var sourceHircsFromDialogue = GenerateSourceHircsFromDialogueEvents(soundBank);
                hircItems.AddRange(sourceHircsFromDialogue);
            }

            SortHircs(hircItems);

            WriteSoundBank(soundBank.Id, soundBank.LanguageId, soundBank.FileName, soundBank.FilePath, hircItems);
        }

        public void GenerateDialogueEventsForTestingSoundBank(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();

            var dialogueEventHircs = GenerateDialogueEventHircs(soundBank);
            hircItems.AddRange(dialogueEventHircs);

            var vanillaDialogueEvents = _audioRepository.GetHircsByType<ICAkDialogueEvent>()
                .Select(hircItem => hircItem as HircItem)
                .Where(hircItem => hircItem.IsCAHircItem == true);

            foreach (var hircItem in hircItems)
            {
                var vanillaDialogueEvent = vanillaDialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Id == hircItem.Id) as CAkDialogueEvent_V136;
                var vanillaDecisionTree = vanillaDialogueEvent.AkDecisionTree as AkDecisionTree_V136;

                var compilerDialogueEvent = hircItem as CAkDialogueEvent_V136;
                var compilerDecisionTree = compilerDialogueEvent.AkDecisionTree as AkDecisionTree_V136;

                // Merge the vanilla decision tree into the compiled decision tree so the compiled decision tree takes priority
                var decisionTree = DecisionTreeMerger.MergeDecisionTrees(compilerDecisionTree.DecisionTree, vanillaDecisionTree.DecisionTree);
                var nodes = DecisionTreeMerger.FlattenDecisionTree(decisionTree);
                var mergedDecisionTree = new AkDecisionTree_V136
                {
                    DecisionTree = decisionTree,
                    Nodes = nodes
                };
                compilerDialogueEvent.AkDecisionTree = mergedDecisionTree;
                compilerDialogueEvent.TreeDataSize = mergedDecisionTree.GetSize();
                compilerDialogueEvent.UpdateSectionSize();
            }

            SortHircs(hircItems);

            WriteSoundBank(soundBank.TestingId, soundBank.LanguageId, soundBank.TestingFileName, soundBank.TestingFilePath, hircItems);
        }

        public void GenerateMergingSoundBank(SoundBank soundBank)
        {
            // We generate all hircs in the merging SoundBank as when merging we check for ID clashes so we can report them to modders
            var actionEventToHircLookup = new Dictionary<ActionEvent, HircItem>();
            var hircItems = new List<HircItem>();

            if (soundBank.ActionEvents != null)
            {
                var actionEventHircs = GenerateActionEventHircs(soundBank, actionEventToHircLookup);
                hircItems.AddRange(actionEventHircs);

                var actionHircs = GenerateActionHircs(soundBank, actionEventToHircLookup);
                hircItems.AddRange(actionHircs);

                var sourceHircsFromPlay = GenerateSourceHircsFromPlayActions(soundBank);
                hircItems.AddRange(sourceHircsFromPlay);
            }

            if (soundBank.DialogueEvents != null)
            {
                var dialogueEventHircs = GenerateDialogueEventHircs(soundBank);
                hircItems.AddRange(dialogueEventHircs);

                var sourceHircsFromDialogueEvents = GenerateSourceHircsFromDialogueEvents(soundBank);
                hircItems.AddRange(sourceHircsFromDialogueEvents);
            }

            SortHircs(hircItems);

            WriteSoundBank(soundBank.MergingId, soundBank.LanguageId, soundBank.MergingFileName, soundBank.MergingFilePath, hircItems);
        }

        public void GenerateMergedDialogueEventSoundBanks(List<string> moddedSoundBanks)
        {
            _audioEditorIntegrityService.CheckMergingSoundBanksIdIntegrity();

            var vanillaDialogueEventsByBnkByLanguage = _audioRepository.GetVanillaDialogueEventsByBnkByLanguage();
            var moddedDialogueEventsByLanguage = _audioRepository.GetModdedDialogueEventsByLanguage(moddedSoundBanks);

            var soundBanksToGenerateByLanguage = moddedDialogueEventsByLanguage
                .ToDictionary(
                    languageEntry => languageEntry.Key,
                    languageEntry => languageEntry.Value
                        .Select(hircItem => _audioRepository.GetNameFromId(hircItem.Id))
                        .Select(dialogueEventName => Wh3DialogueEventInformation.GetSoundBank(dialogueEventName))
                        .Distinct()
                        .ToList());

            foreach (var bnkByLanguage in vanillaDialogueEventsByBnkByLanguage)
            {
                var language = bnkByLanguage.Key;
                if (!moddedDialogueEventsByLanguage.ContainsKey(language))
                    continue;

                var soundBanksToGenerate = soundBanksToGenerateByLanguage[language];
                foreach (var hircsByBnk in bnkByLanguage.Value)
                {
                    var firstDialogueEventName = hircsByBnk.Value.Select(hircItem => _audioRepository.GetNameFromId(hircItem.Id)).FirstOrDefault();
                    var currentSoundBank = Wh3DialogueEventInformation.GetSoundBank(firstDialogueEventName);
                    if (!soundBanksToGenerate.Contains(currentSoundBank))
                        continue;

                    GenerateMergedDialogueEventSoundBank(hircsByBnk.Value, moddedDialogueEventsByLanguage, language, currentSoundBank);
                }
            }
        }

        private List<HircItem> GenerateActionEventHircs(SoundBank soundBank, Dictionary<ActionEvent, HircItem> actionEventToHircLookup)
        {
            var hircItems = new List<HircItem>();
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var actionEventHirc = _wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent);
                hircItems.Add(actionEventHirc);
                actionEventToHircLookup[actionEvent] = actionEventHirc;
            }
            return hircItems;
        }

        private List<HircItem> GenerateActionHircs(SoundBank soundBank, Dictionary<ActionEvent, HircItem> actionEventToHircLookup)
        {
            var hircItems = new List<HircItem>();

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                if (actionEvent.Actions.Count > 1)
                    throw new NotSupportedException("Multiple Actions are not supported");

                var actionEventHirc = actionEventToHircLookup[actionEvent];

                foreach (var action in actionEvent.Actions)
                {
                    var actionHirc = _wwiseHircGeneratorServiceFactory.GenerateHirc(action);
                    hircItems.Add(actionHirc);

                    if (actionEventHirc.HircChildren == null)
                        actionEventHirc.HircChildren = [];
                    actionEventHirc.HircChildren.Add(actionHirc);
                }
            }

            return hircItems;
        }

        private List<HircItem> GenerateSourceHircsFromPlayActions(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                foreach (var action in actionEvent.Actions)
                {
                    if (action.ActionType == AkActionType.Play)
                    {
                        var sound = action.Sound;
                        var randomSequenceContainer = action.RandomSequenceContainer;

                        var sourceHircs = GenerateSourceHircs(soundBank, sound, randomSequenceContainer);
                        hircItems.AddRange(sourceHircs);
                    }
                }
            }

            return hircItems;
        }

        private List<HircItem> GenerateDialogueEventHircs(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();
            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                var dialogueEventHirc = _wwiseHircGeneratorServiceFactory.GenerateHirc(dialogueEvent);
                hircItems.Add(dialogueEventHirc);
            }
            return hircItems;
        }

        private List<HircItem> GenerateSourceHircsFromDialogueEvents(SoundBank soundBank)
        {
            var hircItems = new List<HircItem>();

            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                foreach (var statePath in dialogueEvent.StatePaths)
                {
                    var sound = statePath.Sound;
                    var randomSequenceContainer = statePath.RandomSequenceContainer;

                    var sourceHircs = GenerateSourceHircs(soundBank, sound, randomSequenceContainer);
                    hircItems.AddRange(sourceHircs);
                }
            }

            return hircItems;
        }

        private List<HircItem> GenerateSourceHircs(SoundBank soundBank, Sound sound = null, RandomSequenceContainer randomSequenceContainer = null)
        {
            var hircItems = new List<HircItem>();

            if (sound != null)
            {
                var soundHirc = _wwiseHircGeneratorServiceFactory.GenerateHirc(sound);
                soundHirc.IsTarget = true;
                hircItems.Add(soundHirc);
                return hircItems;
            }

            var randomSequenceContainerHirc = _wwiseHircGeneratorServiceFactory.GenerateHirc(randomSequenceContainer);
            hircItems.Add(randomSequenceContainerHirc);

            randomSequenceContainerHirc.IsTarget = true;
            if (randomSequenceContainerHirc.HircChildren == null)
                randomSequenceContainerHirc.HircChildren = [];

            foreach (var randomSequenceContainerSound in randomSequenceContainer.Sounds)
            {
                var soundHirc = _wwiseHircGeneratorServiceFactory.GenerateHirc(randomSequenceContainerSound);
                hircItems.Add(soundHirc);
                randomSequenceContainerHirc.HircChildren.Add(soundHirc);
            }

            return hircItems;
        }

        private void GenerateMergedDialogueEventSoundBank(
            List<HircItem> vanillaHircs,
            Dictionary<string, List<HircItem>> moddedDialogueEventsByLanguage,
            string language,
            Wh3SoundBank currentSoundBank)
        {
            var dialogueEvents = new List<HircItem>();

            foreach (var vanillaHirc in vanillaHircs)
            {
                var vanillaDialogueEvent = vanillaHirc as CAkDialogueEvent_V136;
                var mergedDialogueEvent = vanillaDialogueEvent.Clone();

                var matchingModdedDialogueEvents = moddedDialogueEventsByLanguage[language]
                    .Where(moddedHircItem => moddedHircItem.Id == vanillaDialogueEvent.Id)
                    .ToList();

                if (matchingModdedDialogueEvents.Count == 0)
                    continue;

                foreach (var moddedDialogueEventHirc in matchingModdedDialogueEvents)
                {
                    var currentDecisionTree = mergedDialogueEvent.AkDecisionTree as AkDecisionTree_V136;

                    var moddedDialogueEvent = moddedDialogueEventHirc as CAkDialogueEvent_V136;
                    var moddedDecisionTree = moddedDialogueEvent.AkDecisionTree as AkDecisionTree_V136;

                    var mergedDecisionTree = new AkDecisionTree_V136();
                    mergedDecisionTree.DecisionTree = DecisionTreeMerger.MergeDecisionTrees(currentDecisionTree.DecisionTree, moddedDecisionTree.DecisionTree);
                    mergedDecisionTree.Nodes = DecisionTreeMerger.FlattenDecisionTree(mergedDecisionTree.DecisionTree);

                    mergedDialogueEvent.AkDecisionTree = mergedDecisionTree;
                    mergedDialogueEvent.TreeDataSize = mergedDecisionTree.GetSize();
                    mergedDialogueEvent.UpdateSectionSize();
                }

                dialogueEvents.Add(mergedDialogueEvent);
            }

            SortHircs(dialogueEvents);

            var soundBankNameBase = Wh3SoundBankInformation.GetName(currentSoundBank);
            var soundBankNameWithoutExtension = $"{soundBankNameBase}_0_audio_mixer";
            var soundBankFileName = $"{soundBankNameWithoutExtension}.bnk";

            var soundBankFilePath = $"audio\\wwise\\{language}\\{soundBankFileName}";
            if (language == Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx))
                soundBankFilePath = $"audio\\wwise\\{soundBankFileName}";

            var soundBankId = WwiseHash.Compute(soundBankNameWithoutExtension);
            var languageId = WwiseHash.Compute(language);

            WriteSoundBank(soundBankId, languageId, soundBankFileName, soundBankFilePath, dialogueEvents);
        }

        private static void SortHircs(List<HircItem> hircItems)
        {
            var targetHircs = hircItems
                .Where(hircItem => hircItem.IsTarget)
                .ToList();

            var eventHircs = hircItems
                .Where(hircItem => hircItem.HircType == AkBkHircType.Event || hircItem.HircType == AkBkHircType.Dialogue_Event)
                .ToList();

            var sortedHircItems = new List<HircItem>();

            foreach (var targetHirc in targetHircs.OrderBy(sourceHirc => sourceHirc.Id))
            {
                if (targetHirc.HircType == AkBkHircType.RandomSequenceContainer)
                {
                    var soundHircs = targetHirc.HircChildren.OrderBy(soundHirc => soundHirc.Id);
                    sortedHircItems.AddRange(soundHircs);
                    sortedHircItems.Add(targetHirc);
                }
                else
                    sortedHircItems.Add(targetHirc);
            }

            foreach (var eventHirc in eventHircs.OrderBy(eventHirc => eventHirc.Id))
            {
                if (eventHirc.HircType == AkBkHircType.Event)
                {
                    var actionHircs = eventHirc.HircChildren.OrderBy(eventHirc => eventHirc.Id);
                    sortedHircItems.AddRange(actionHircs);
                    sortedHircItems.Add(eventHirc);
                }
                else
                    sortedHircItems.Add(eventHirc);
            }

            hircItems.Clear();
            hircItems.AddRange(sortedHircItems);
        }

        private void WriteSoundBank(uint id, uint languageId, string fileName, string filePath, List<HircItem> hircItems)
        {
            var gameInformation = GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame);
            var bankGeneratorVersion = (uint)gameInformation.BankGeneratorVersion;
            var wwiseProjectId = (uint)gameInformation.WwiseProjectId;

            var bkhdChunk = BkhdChunkGenerator.GenerateBkhdChunk(bankGeneratorVersion, id, languageId, wwiseProjectId);
            var bkhdChunkBytes = BkhdChunk.WriteData(bkhdChunk);

            var hircChunk = HircChunkGenerator.GenerateHircChunk(hircItems);
            var hircChunkBytes = HircChunk.WriteData(hircChunk, bankGeneratorVersion);

            using var memStream = new MemoryStream();
            memStream.Write(bkhdChunkBytes);
            memStream.Write(hircChunkBytes);
            var bytes = memStream.ToArray();

            var bnkPackFile = new PackFile(fileName, new MemorySource(bytes));
            var reparsedSanityFile = BnkParser.Parse(bnkPackFile, "test\\fakefilename.bnk", true);

            _fileSaveService.Save(filePath, bnkPackFile.DataSource.ReadData(), false);
        }
    }
}
