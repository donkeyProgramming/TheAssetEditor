using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Utilities;
using Editors.Audio.Shared.Wwise;
using Editors.Audio.Shared.Wwise.HircExploration;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioProjectConverter
{
    public partial class AudioProjectConverterViewModel : ObservableObject
    {
        private readonly IStandardDialogs _standardDialogs;
        private readonly IFileSaveService _fileSaveService;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioEditorFileService _audioEditorFileService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly VgStreamWrapper _vgStreamWrapper;

        private readonly ILogger _logger = Logging.Create<AudioProjectConverterViewModel>();
        private System.Action _closeAction;

        [ObservableProperty] private string _audioProjectName;
        [ObservableProperty] private string _outputDirectoryPath;
        [ObservableProperty] private string _wemsDirectoryPath;
        [ObservableProperty] private string _bnksDirectoryPath;
        [ObservableProperty] private string _vOActorSubstring;
        [ObservableProperty] private string _soundbanksInfoXmlPath;
        [ObservableProperty] private bool _isUsingWwiseProject;

        [ObservableProperty] private bool _isAudioProjectNameSet;
        [ObservableProperty] private bool _isOutputDirectoryPathSet;
        [ObservableProperty] private bool _isWemsDirectoryPathSet; 
        [ObservableProperty] private bool _isBnksDirectoryPathSet;
        [ObservableProperty] private bool _isVOActorSubstringSet;
        [ObservableProperty] private bool _isSoundbanksInfoXmlSet;
        [ObservableProperty] private bool _isOkButtonEnabled;

        public AudioProjectConverterViewModel(
            IStandardDialogs standardDialogs,
            IFileSaveService fileSaveService,
            IAudioRepository audioRepository,
            IAudioEditorFileService audioEditorFileService,
            ApplicationSettingsService applicationSettingsService,
            VgStreamWrapper vgStreamWrapper)
        {
            _standardDialogs = standardDialogs;
            _fileSaveService = fileSaveService;
            _audioRepository = audioRepository;
            _audioEditorFileService = audioEditorFileService;
            _applicationSettingsService = applicationSettingsService;
            _vgStreamWrapper = vgStreamWrapper;

            _audioRepository.Load([Wh3LanguageInformation.GetLanguageAsString(Wh3Language.EnglishUK)]);

            OutputDirectoryPath = "audio\\audio_projects";
        }

        [RelayCommand] public void ProcessAudioProjectConversion()
        {
            var currentGame = _applicationSettingsService.CurrentSettings.CurrentGame;
            var fileName = $"{AudioProjectName}.aproj";
            var filePath = $"{OutputDirectoryPath}\\{fileName}";
            var language = "english(uk)";
            var audioProject = AudioProjectFile.Create(currentGame, language, AudioProjectName);

            var soundBankPaths = new List<string>();
            if (Directory.Exists(BnksDirectoryPath))
                soundBankPaths = Directory.GetFiles(BnksDirectoryPath, "*.bnk", SearchOption.TopDirectoryOnly).ToList();

            var statesLookupByStateGroupByStateId = new Dictionary<string, Dictionary<uint, string>>();
            var dialogueEventsLookupByWemId = new Dictionary<uint, List<string>>();
            var statePathsLookupByDialogueEvent = new Dictionary<string, List<StatePathInfo>>();
            var dialogueEventsToProcess = new List<ICAkDialogueEvent>();
            var moddedStateGroups = new Dictionary<string, List<string>>();
            var globalBaseNameUsage = new Dictionary<string, int>();

            var hircItems = GetHircItems(soundBankPaths);
            var hircLookupById = BuildHircLookupById(hircItems);

            if (!string.IsNullOrEmpty(SoundbanksInfoXmlPath))
                BuildStateGroupStateLookup(statesLookupByStateGroupByStateId);

            var dialogueEvents = hircItems.OfType<ICAkDialogueEvent>().ToList();
            foreach (var dialogueEvent in dialogueEvents)
                SetDialogueEventData(
                    dialogueEvent,
                    dialogueEventsLookupByWemId,
                    statePathsLookupByDialogueEvent,
                    statesLookupByStateGroupByStateId,
                    hircLookupById,
                    dialogueEventsToProcess,
                    moddedStateGroups);

            var usedHircIds = new HashSet<uint>();
            var usedSourceIds = new HashSet<uint>();

            var audioProjectGeneratableItemIds = audioProject.GetGeneratableItemIds();
            var audioProjectSourceIds = audioProject.GetAudioFileIds();

            var languageId = WwiseHash.Compute(audioProject.Language);
            var languageHircIds = _audioRepository.GetUsedVanillaHircIdsByLanguageId(languageId);
            var languageSourceIds = _audioRepository.GetUsedVanillaSourceIdsByLanguageId(languageId);

            usedHircIds.UnionWith(audioProjectGeneratableItemIds);
            usedHircIds.UnionWith(languageHircIds);
            usedSourceIds.UnionWith(audioProjectSourceIds);
            usedSourceIds.UnionWith(languageSourceIds);

            foreach (var dialogueEvent in dialogueEventsToProcess)
                ProcessDialogueEvent(
                    audioProject,
                    dialogueEvent,
                    dialogueEventsLookupByWemId,
                    statePathsLookupByDialogueEvent,
                    globalBaseNameUsage,
                    usedHircIds,
                    usedSourceIds);

            ProcessModdedStateGroups(audioProject, moddedStateGroups);

            _audioEditorFileService.Save(audioProject, fileName, filePath);

            CloseWindowAction();
        }

        private static Dictionary<uint, List<HircItem>> BuildHircLookupById(List<HircItem> hircItems)
        {
            var hircLookupById = new Dictionary<uint, List<HircItem>>();
            foreach (var hircItem in hircItems)
            {
                if (!hircLookupById.TryGetValue(hircItem.Id, out var hircItemList))
                {
                    hircItemList = [];
                    hircLookupById.TryAdd(hircItem.Id, hircItemList);
                }
                hircItemList.Add(hircItem);
            }

            return hircLookupById;
        }

        private static List<HircItem> GetHircItems(List<string> soundBankPaths)
        {
            var parsedSoundBanks = new List<ParsedBnkFile>();

            foreach (var soundBankPath in soundBankPaths)
            {
                var soundBankDataBytes = File.ReadAllBytes(soundBankPath);
                var soundBankPackFile = PackFile.CreateFromBytes(soundBankPath, soundBankDataBytes);
                var parsedSoundBank = BnkParser.Parse(soundBankPackFile, soundBankPath, false);
                parsedSoundBanks.Add(parsedSoundBank);
            }

            var hircItems = parsedSoundBanks
                .SelectMany(soundBank => soundBank.HircChunk.HircItems)
                .ToList();

            return hircItems;
        }

        private Dictionary<string, Dictionary<uint, string>> BuildStateGroupStateLookup(Dictionary<string, Dictionary<uint, string>> stateLookupByStateGroup)
        {
            var soundBanksInfo = XDocument.Load(SoundbanksInfoXmlPath);

            foreach (var stateGroup in soundBanksInfo.Descendants("StateGroup"))
            {
                var stateGroupName = stateGroup.Attribute("Name")?.Value;
                if (string.IsNullOrEmpty(stateGroupName))
                    continue;

                if (!stateLookupByStateGroup.TryGetValue(stateGroupName, out var stateLookupByStateId))
                {
                    stateLookupByStateId = [];
                    stateLookupByStateGroup.TryAdd(stateGroupName, stateLookupByStateId);
                }

                foreach (var state in stateGroup.Element("States")?.Elements("State") ?? [])
                {
                    var stateName = state.Attribute("Name")?.Value;
                    if (!string.IsNullOrEmpty(stateName))
                    {
                        var stateHash = WwiseHash.Compute(stateName);
                        stateLookupByStateId.TryAdd(stateHash, stateName);
                    }
                }
            }

            return stateLookupByStateGroup;
        }

        private void SetDialogueEventData(
            ICAkDialogueEvent dialogueEvent,
            Dictionary<uint, List<string>> dialogueEventsLookupByWemId,
            Dictionary<string, List<StatePathInfo>> statePathsLookupByDialogueEvent,
            Dictionary<string, Dictionary<uint, string>> statesLookupByStateGroupByStateId,
            Dictionary<uint, List<HircItem>> hircLookupById,
            List<ICAkDialogueEvent> dialogueEventsToProcess,
            Dictionary<string, List<string>> moddedStateGroups)
        {
            var stateGroup = "VO_Actor";
            var statesLookupByStateId = GetStatesLookupByStateId(statesLookupByStateGroupByStateId, stateGroup);

            var dialogueEventHirc = dialogueEvent as HircItem;
            var dialogueEventName = _audioRepository.GetNameFromId(dialogueEventHirc.Id);

            var statePathParser = new StatePathParser(_audioRepository);
            var result = statePathParser.GetStatePaths(dialogueEvent);

            var voActorSubstrings = VOActorSubstring
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(substring => substring.Trim())
                .ToArray();

            var anyStatePathsContainRequiredPattern = result.StatePaths
                .Any(statePath => statePath.Items.Any(state =>
                    statesLookupByStateId.TryGetValue(state.Value, out var stateName) &&
                    voActorSubstrings.Any(substring => stateName.Contains(substring, StringComparison.CurrentCultureIgnoreCase))));

            if (!anyStatePathsContainRequiredPattern)
                return;

            // Store the dialogue event for use later
            dialogueEventsToProcess.Add(dialogueEvent);

            foreach (var statePath in result.StatePaths)
            {
                var statePathContainsRequiredPattern = statePath.Items.Any(state => statesLookupByStateId.TryGetValue(state.Value, out var stateName) 
                    && voActorSubstrings.Any(substring => stateName.Contains(substring, StringComparison.CurrentCultureIgnoreCase)));

                if (!statePathContainsRequiredPattern)
                    continue;

                var wavFiles = new List<WavFile>();
                var statePathNodes = new List<StatePath.Node>();

                StoreStateGroupAndStateInfo(dialogueEvent, statesLookupByStateId, statesLookupByStateGroupByStateId, statePath, statePathNodes, moddedStateGroups);

                StoreDialogueEventsLookupByWemId(dialogueEventsLookupByWemId, hircLookupById, wavFiles, dialogueEventName, statePath);

                StoreStatePathInfo(statePathsLookupByDialogueEvent, wavFiles, dialogueEventName, statePathNodes);
            }
        }

        private Dictionary<uint, string> GetStatesLookupByStateId(Dictionary<string, Dictionary<uint, string>> statesLookupByStateGroupByStateId, string stateGroupLookup)
        {
            statesLookupByStateGroupByStateId.TryGetValue(stateGroupLookup, out var statesInfoFromWwiseProject);

            var statesLookupByStateId = new Dictionary<uint, string>();

            if (statesInfoFromWwiseProject != null)
            {
                foreach (var stateInfo in statesInfoFromWwiseProject)
                    statesLookupByStateId.TryAdd(stateInfo.Key, stateInfo.Value);
            }

            // Not all names are States but that doesn't matter
            foreach (var stateInfo in _audioRepository.NameById)
                statesLookupByStateId.TryAdd(stateInfo.Key, stateInfo.Value);

            return statesLookupByStateId;
        }

        private void StoreStateGroupAndStateInfo(
            ICAkDialogueEvent dialogueEvent,
            Dictionary<uint, string> wwiseStatesIdLookup,
            Dictionary<string, Dictionary<uint, string>> statesLookupByStateGroupByStateId,
            StatePathParser.StatePath statePath,
            List<StatePath.Node> statePathNodes,
            Dictionary<string, List<string>> moddedStateGroups)
        {
            var stateGroupIndex = 0;

            foreach (var statePathItem in statePath.Items)
            {
                var stateGroup = _audioRepository.GetNameFromId(dialogueEvent.Arguments[stateGroupIndex].GroupId);
                var statesLookupByStateId = GetStatesLookupByStateId(statesLookupByStateGroupByStateId, stateGroup);
                var state = string.Empty;

                if (statePathItem.Value == 0)
                    state = "Any";
                else
                {
                    if (statesLookupByStateId.TryGetValue(statePathItem.Value, out var unhashedState))
                        state = unhashedState;
                }

                var audioProjectstateGroup = StateGroup.Create(stateGroup);
                var audioProjectstate = State.Create(state);
                var statePathNode = StatePath.Node.Create(audioProjectstateGroup, audioProjectstate);
                statePathNodes.Add(statePathNode);

                // Store modded states info
                if (state != "Any" && !_audioRepository.NameById.ContainsValue(state))
                {
                    if (moddedStateGroups.TryGetValue(stateGroup, out var stateList))
                    {
                        if (!stateList.Contains(state))
                            stateList.Add(state);
                    }
                    else
                        moddedStateGroups.TryAdd(stateGroup, [state]);
                }

                stateGroupIndex++;
            }
        }

        private static void StoreDialogueEventsLookupByWemId(
            Dictionary<uint, List<string>> dialogueEventsLookupByWemId,
            Dictionary<uint, List<HircItem>> hircLookupById,
            List<WavFile> wavFiles,
            string dialogueEventName,
            StatePathParser.StatePath statePath)
        {
            ProcessHircItem(statePath.ChildNodeId, dialogueEventsLookupByWemId, hircLookupById, wavFiles, dialogueEventName);
        }

        private static void ProcessHircItem(
            uint childNodeId,
            Dictionary<uint, List<string>> dialogueEventsLookupByWemId,
            Dictionary<uint, List<HircItem>> hircLookupById,
            List<WavFile> wavFiles,
            string dialogueEventName)
        {
            if (!hircLookupById.TryGetValue(childNodeId, out var hircItems) || hircItems.Count == 0)
                return;

            var hircItem = hircItems.First();

            if (hircItem is ICAkSound soundHirc)
            {
                var wavFile = new WavFile()
                {
                    WemId = soundHirc.GetSourceId(),
                    DialogueEvent = dialogueEventName
                };
                wavFiles.Add(wavFile);

                if (dialogueEventsLookupByWemId.TryGetValue(wavFile.WemId, out var dialogueEventList))
                {
                    if (!dialogueEventList.Contains(dialogueEventName))
                        dialogueEventList.Add(dialogueEventName);
                }
                else
                    dialogueEventsLookupByWemId.TryAdd(wavFile.WemId, [dialogueEventName]);
            }
            else if (hircItem is ICAkRanSeqCntr ranSeqCntr)
            {
                foreach (var childId in ranSeqCntr.GetChildren())
                    ProcessHircItem(childId, dialogueEventsLookupByWemId, hircLookupById, wavFiles, dialogueEventName);
            }
        }

        private static void StoreStatePathInfo(
            Dictionary<string, List<StatePathInfo>> statePathsLookupByDialogueEvent,
            List<WavFile> wavFiles,
            string dialogueEventName,
            List<StatePath.Node> statePathNodes)
        {
            var joinedStatePath = string.Join(".", statePathNodes.Select(statePathNode => statePathNode.State.Name));

            var statePathInfo = new StatePathInfo
            {
                JoinedStatePath = joinedStatePath,
                StatePathNodes = statePathNodes,
                WavFiles = wavFiles
            };

            if (statePathsLookupByDialogueEvent.TryGetValue(dialogueEventName, out var statePath))
            {
                var containsJoinedStatePath = statePath.Any(statePathInfo => statePathInfo.JoinedStatePath == joinedStatePath);
                if (!containsJoinedStatePath)
                    statePath.Add(statePathInfo);
            }
            else
                statePathsLookupByDialogueEvent.TryAdd(dialogueEventName, [statePathInfo]);
        }

        private void ProcessDialogueEvent(
            AudioProjectFile audioProject,
            ICAkDialogueEvent dialogueEvent,
            Dictionary<uint, List<string>> dialogueEventsLookupByWemId,
            Dictionary<string, List<StatePathInfo>> statePathsLookupByDialogueEvent,
            Dictionary<string, int> globalBaseNameUsage,
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds)
        {
            var dialogueEventHirc = dialogueEvent as HircItem;
            var dialogueEventName = _audioRepository.GetNameFromId(dialogueEventHirc.Id);
            var actorMixerId = Wh3DialogueEventInformation.GetActorMixerId(dialogueEventName);

            _logger.Here().Information($"Processing Dialogue Event: {dialogueEventName}");

            var dialogueEventAndSoundBank = audioProject.SoundBanks
                .Where(soundBank => soundBank.DialogueEvents != null)
                .SelectMany(
                    soundBank => soundBank.DialogueEvents,
                    (soundBank, dialogueEventItem) => new { SoundBank = soundBank, DialogueEvent = dialogueEventItem })
                .FirstOrDefault(pair => pair.DialogueEvent.Name == dialogueEventName);

            var audioProjectDialogueEvent = dialogueEventAndSoundBank.DialogueEvent;
            var audioProjectDialogueEventSoundBank = dialogueEventAndSoundBank.SoundBank;

            var voActorSubstrings = VOActorSubstring
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(substring => substring.Trim().ToLower())
                .ToArray();

            foreach (var statePath in statePathsLookupByDialogueEvent[dialogueEventName].ToList())
            {
                _logger.Here().Information($"Processing State Path: {statePath.JoinedStatePath}");

                var audioFiles = new List<AudioFile>();
                var statePathWavs = statePath.WavFiles;

                // Get the first VO_Actor as that should? always be the one we want
                var voActor = statePath.StatePathNodes
                    .Where(statePathNode => statePathNode.StateGroup.Name == "VO_Actor")
                    .FirstOrDefault()?.State.Name;

                if (voActor == null)
                    continue;

                // Stop if not containing the thing we're after
                if (!voActorSubstrings.Any(substring => voActor.Contains(substring, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                ProcessWavFiles(audioProject, statePathWavs, audioFiles, dialogueEventsLookupByWemId, globalBaseNameUsage, dialogueEventName, voActor, usedSourceIds);

                if (audioFiles.Count == 0)
                    continue;

                StatePath audioProjectStatePath;
                if (audioFiles.Count > 1)
                {
                    var children = new List<uint>();

                    var randomSequenceContainerIds = IdGenerator.GenerateIds(usedHircIds);
                    usedHircIds.Add(randomSequenceContainerIds.Id);

                    var playlistOrder = 0;
                    foreach (var audioFile in audioFiles)
                    {
                        playlistOrder++;
                        var soundIds = IdGenerator.GenerateIds(usedHircIds);
                        var sound = Sound.Create(soundIds.Guid, soundIds.Id, randomSequenceContainerIds.Id, playlistOrder, audioFile.Id, audioProject.Language);

                        usedHircIds.Add(soundIds.Id);
                        audioFile.Sounds.Add(sound.Id);
                        children.Add(sound.Id);
                        audioProjectDialogueEventSoundBank.Sounds.TryAdd(sound);
                    }

                    var randomSequenceContainerSettings = Shared.AudioProject.Models.HircSettings.CreateRecommendedRandomSequenceContainerSettings(audioFiles.Count);
                    var randomSequenceContainer = RandomSequenceContainer.Create(
                        randomSequenceContainerIds.Guid,
                        randomSequenceContainerIds.Id,
                        randomSequenceContainerSettings,
                        children,
                        directParentId: actorMixerId);
                    audioProjectStatePath = StatePath.Create(statePath.StatePathNodes, randomSequenceContainer.Id, AkBkHircType.RandomSequenceContainer);

                    audioProjectDialogueEventSoundBank.RandomSequenceContainers.TryAdd(randomSequenceContainer);
                }
                else
                {
                    var audioFile = audioFiles[0];
                    var soundIds = IdGenerator.GenerateIds(usedHircIds);
                    usedHircIds.Add(soundIds.Id);

                    var soundSettings = Shared.AudioProject.Models.HircSettings.CreateSoundSettings();
                    var sound = Sound.Create(soundIds.Guid, soundIds.Id, 0, actorMixerId, audioFile.Id, audioProject.Language, soundSettings);
                    audioFile.Sounds.Add(sound.Id);

                    audioProjectStatePath = StatePath.Create(statePath.StatePathNodes, sound.Id, AkBkHircType.Sound);

                    audioProjectDialogueEventSoundBank.Sounds.TryAdd(sound);
                }

                audioProjectDialogueEvent.StatePaths.InsertAlphabetically(audioProjectStatePath);

                // Remove the processed StatePath from the original list as, if we're processing multiple bnks,
                // Dialogue Events can occur several times so it would add duplicates of the same StatePath
                statePathsLookupByDialogueEvent[dialogueEventName].Remove(statePath);
            }
        }

        private void ProcessWavFiles(
            AudioProjectFile audioProject,
            List<WavFile> statePathWavs,
            List<AudioFile> audioFiles,
            Dictionary<uint, List<string>> dialogueEventsLookupByWemId,
            Dictionary<string, int> globalBaseNameUsage,
            string dialogueEventName,
            string voActor,
            HashSet<uint> usedSourceIds)
        {
            var voActorSegment = voActor.Substring(voActor.IndexOf("vo_actor_") + "vo_actor_".Length).ToLower();

            foreach (var wavFile in statePathWavs)
            {
                var chosenDialogueEventName = GetPreferredDialogueEventName(wavFile.WemId, dialogueEventName, dialogueEventsLookupByWemId).ToLower();
                var baseFileName = $"{voActorSegment}_{chosenDialogueEventName}".ToLower();

                // Ensure unique numbering across events globally
                if (!globalBaseNameUsage.TryGetValue(baseFileName, out var count))
                    count = 0;
                count++;
                globalBaseNameUsage[baseFileName] = count;

                wavFile.FileName = $"{baseFileName}_{count}.wav".ToLower();
                wavFile.FilePath = $"{OutputDirectoryPath}\\vo\\{voActorSegment}\\{wavFile.FileName}".ToLower();

                var audioFile = audioProject.GetAudioFile(wavFile.FilePath);
                if (audioFile == null)
                {
                    var audioFileIds = IdGenerator.GenerateIds(usedSourceIds);
                    audioFile = AudioFile.Create(audioFileIds.Guid, audioFileIds.Id, wavFile.FileName, wavFile.FilePath);

                    usedSourceIds.Add(audioFile.Id);
                    audioProject.AudioFiles.TryAdd(audioFile);
                }
                audioFiles.Add(audioFile);

                var wemFilePath = $"{WemsDirectoryPath}\\{wavFile.WemId}.wem";
                var wavTempFilePath = $"{DirectoryHelper.Temp}\\Audio\\{wavFile.FileName}";
                var wavPackOutputPath = $"{OutputDirectoryPath}\\vo\\{voActorSegment}\\{wavFile.FileName}";

                var conversionResult = _vgStreamWrapper.ConvertFileUsingVgStream(wemFilePath, wavTempFilePath);
                if (conversionResult.Failed)
                    throw new Exception($"Failed to convert {wemFilePath} to {wavTempFilePath}");

                var wavFileBytes = File.ReadAllBytes(wavTempFilePath);
                var packFile = PackFile.CreateFromBytes(AudioProjectName, wavFileBytes);
                _fileSaveService.Save(wavPackOutputPath, packFile.DataSource.ReadData(), false);
            }
        }

        private static void ProcessModdedStateGroups(AudioProjectFile audioProject, Dictionary<string, List<string>> moddedStateGroups)
        {
            foreach (var moddedStateGroup in moddedStateGroups)
            {
                foreach (var moddedState in moddedStateGroup.Value)
                {
                    var audioProjectModdedState = State.Create(moddedState);
                    var audioProjectStateGroup = audioProject.StateGroups.FirstOrDefault(stateGroup => stateGroup.Name == moddedStateGroup.Key);
                    audioProjectStateGroup.States.InsertAlphabetically(audioProjectModdedState);
                }
            }
        }

        private static string GetPreferredDialogueEventName(uint wemId, string fallbackDialogueEventName, Dictionary<uint, List<string>> dialogueEventsLookupByWemId)
        {
            if (dialogueEventsLookupByWemId.TryGetValue(wemId, out var dialogueEvents) && dialogueEvents != null && dialogueEvents.Count > 0)
            {
                // A list of Dialogue Events whose names I want to prioritise as the name of wems if they appear in them
                var priorityList = new List<string>
                {
                    "battle_vo_order_attack",
                    "battle_vo_order_move",
                    "battle_vo_order_select",
                    "battle_vo_order_halt",
                    "battle_vo_order_withdraw",
                    "battle_vo_order_withdraw_tactical",
                    "battle_vo_order_generic_response",
                    "campaign_vo_attack",
                    "campaign_vo_move",
                    "campaign_vo_selected",
                    "campaign_vo_yes",
                    "campaign_vo_no",
                };

                foreach (var priority in priorityList)
                {
                    if (dialogueEvents.Any(dialogueEvent => dialogueEvent.Equals(priority, StringComparison.OrdinalIgnoreCase)))
                        return dialogueEvents.First(dialogueEvent => dialogueEvent.Equals(priority, StringComparison.OrdinalIgnoreCase));
                }

                return dialogueEvents.OrderBy(dialogueEvent => dialogueEvent, StringComparer.OrdinalIgnoreCase).First();
            }
            return fallbackDialogueEventName;
        }

        partial void OnAudioProjectNameChanged(string value)
        {
            IsAudioProjectNameSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnOutputDirectoryPathChanged(string value)
        {
            IsOutputDirectoryPathSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnVOActorSubstringChanged(string value)
        {
            IsVOActorSubstringSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnWemsDirectoryPathChanged(string value)
        {
            IsWemsDirectoryPathSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnBnksDirectoryPathChanged(string value)
        {
            IsBnksDirectoryPathSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnSoundbanksInfoXmlPathChanged(string value)
        {
            IsSoundbanksInfoXmlSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnIsUsingWwiseProjectChanged(bool value)
        {
            IsUsingWwiseProject = value;
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOkButtonEnabled = IsAudioProjectNameSet
                && IsOutputDirectoryPathSet
                && IsWemsDirectoryPathSet
                && IsBnksDirectoryPathSet
                && IsVOActorSubstringSet
                && ((IsUsingWwiseProject && IsSoundbanksInfoXmlSet) || !IsUsingWwiseProject);
        }

        [RelayCommand] public void SetOutputDirectoryPath()
        {
            var result = _standardDialogs.DisplayBrowseFolderDialog();
            if (result.Result)
            {
                var filePath = result.Folder;
                OutputDirectoryPath = filePath;
                _logger.Here().Information($"Audio Project directory set to: {filePath}");
            }
        }

        [RelayCommand] public void SetWemsDirectoryPath()
        {
            using var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                WemsDirectoryPath = dialog.SelectedPath;
        }

        [RelayCommand] public void SetBnksDirectoryPath()
        {
            using var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                BnksDirectoryPath = dialog.SelectedPath;
        }

        [RelayCommand] public void SetSoundbanksInfoXmlPath()
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select SoundbanksInfo.xml file";
            openFileDialog.Filter = "SoundbanksInfo.xml|SoundbanksInfo.xml";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                SoundbanksInfoXmlPath = openFileDialog.FileName;
        }

        [RelayCommand] public void CloseWindowAction() => _closeAction?.Invoke();

        public void SetCloseAction(System.Action closeAction) =>_closeAction = closeAction;
    }
}
