using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Editors.Audio.AudioEditor.AudioProjectCompiler;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using static Editors.Audio.AudioEditor.IntegrityChecker;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using static Editors.Audio.GameSettings.Warhammer3.StateGroups;

namespace Editors.Audio.AudioEditor.Data.AudioProjectService
{
    public class AudioProjectService : IAudioProjectService
    {
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public AudioProjectDataModel AudioProject { get; set; } = new AudioProjectDataModel();
        public string AudioProjectFileName { get; set; }
        public string AudioProjectDirectory { get; set; }
        public Dictionary<string, List<string>> StateGroupsWithModdedStatesRepository { get; set; } = [];
        public Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError { get; set; } = [];
        public Dictionary<string, DialogueEventPreset?> DialogueEventSoundBankFiltering { get; set; } = [];

        public void SaveAudioProject(IPackFileService packFileService)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            var audioProject = GetAudioProjectWithoutUnusedObjects();
            var fileJson = JsonSerializer.Serialize(audioProject, options);
            var pack = packFileService.GetEditablePack();
            var fileName = $"{AudioProjectFileName}.aproj";

            var fileEntry = new NewPackFileEntry(AudioProjectDirectory, PackFile.CreateFromASCII(fileName, fileJson));
            packFileService.AddFilesToPack(pack, [fileEntry]);

            _logger.Here().Information($"Saved Audio Project file: {AudioProjectDirectory}\\{AudioProjectFileName}.aproj");
        }

        public void LoadAudioProject(AudioEditorViewModel audioEditorViewModel, IPackFileService packFileService, IAudioRepository audioRepository, IStandardDialogs packFileUiProvider)
        {
            var result = packFileUiProvider.DisplayBrowseDialog([".aproj"]);
            if (result.Result)
            {
                var filePath = packFileService.GetFullPath(result.File);
                var fileName = Path.GetFileName(filePath);
                var fileType = Path.GetExtension(filePath);
                var file = packFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);

                audioEditorViewModel.AudioProjectExplorerViewModel.AudioProjectExplorerLabel = $"Audio Project Explorer - {AudioProjectHelpers.AddExtraUnderscoresToString(fileName)}";

                // Reset data
                audioEditorViewModel.ResetAudioEditorViewModelData();
                ResetAudioProject();

                // Set the AudioProject
                var savedProject = JsonSerializer.Deserialize<AudioProjectDataModel>(audioProjectJson);
                AudioProjectFileName = fileName.Replace(fileType, string.Empty);
                AudioProjectDirectory = filePath.Replace($"\\{fileName}", string.Empty);

                // Initialise a full Audio Project and merge the saved Audio Project with it
                InitialiseAudioProject(audioEditorViewModel, AudioProjectFileName, AudioProjectDirectory, savedProject.Language);
                MergeSavedAudioProjectIntoAudioProjectWithUnusedItems(savedProject);

                // Initialise data after AudioProject is set so it uses the correct instance
                audioEditorViewModel.Initialise();

                // Get the Modded States and prepare them for being added to the DataGrid ComboBoxes
                BuildStateGroupsWithModdedStatesRepository(AudioProject.StateGroups, StateGroupsWithModdedStatesRepository);

                CheckAudioProjectDialogueEventIntegrity(audioRepository, this);

                audioEditorViewModel.AudioProjectExplorerViewModel.CreateAudioProjectTree();

                _logger.Here().Information($"Loaded Audio Project: {fileName}");
            }
        }

        public void InitialiseAudioProject(AudioEditorViewModel audioEditorViewModel, string fileName, string directory, string language)
        {
            audioEditorViewModel.AudioProjectExplorerViewModel.AudioProjectExplorerLabel = $"Audio Project Explorer - {AudioProjectHelpers.AddExtraUnderscoresToString(fileName)}";

            AudioProjectFileName = fileName;
            AudioProjectDirectory = directory;
            AudioProject.Language = language;

            InitialiseSoundBanks();

            InitialiseModdedStatesGroups();

            SortSoundBanksAlphabetically();

            audioEditorViewModel.AudioProjectExplorerViewModel.CreateAudioProjectTree();
        }

        public void CompileAudioProject(IPackFileService packFileService, IAudioRepository audioRepository, ApplicationSettingsService applicationSettingsService)
        {
            var soundBankGenerator = new SoundBankGenerator(packFileService, audioRepository, this, applicationSettingsService);
            var audioProject = GetAudioProjectWithoutUnusedObjects();
            soundBankGenerator.CompileSoundBanksFromAudioProject(audioProject);
        }

        private void InitialiseSoundBanks()
        {
            var soundBanks = Enum.GetValues<Wh3SoundBankSubType>()
                .Select(soundBank => new SoundBank
                {
                    Name = GetSoundBankSubTypeDisplayString(soundBank),
                    Type = GetSoundBankSubType(soundBank)
                })
                .ToList();

            AudioProject.SoundBanks = [];

            foreach (var soundBankEnum in Enum.GetValues<Wh3SoundBankSubType>())
            {
                var soundBank = new SoundBank
                {
                    Name = GetSoundBankSubTypeDisplayString(soundBankEnum),
                    Type = GetSoundBankSubType(soundBankEnum)
                };

                if (soundBank.Type == Wh3SoundBankType.ActionEventSoundBank)
                    soundBank.ActionEvents = [];
                else
                {
                    soundBank.DialogueEvents = [];

                    var filteredDialogueEvents = DialogueEventData
                        .Where(dialogueEvent => dialogueEvent.SoundBank == GetSoundBankEnum(soundBank.Name));

                    foreach (var dialogueData in filteredDialogueEvents)
                    {
                        var dialogueEvent = new DialogueEvent
                        {
                            Name = dialogueData.Name,
                            DecisionTree = []
                        };
                        soundBank.DialogueEvents.Add(dialogueEvent);
                    }
                }

                AudioProject.SoundBanks.Add(soundBank);
            }
        }

        private void InitialiseModdedStatesGroups()
        {
            AudioProject.StateGroups = [];

            foreach (var moddedStateGroup in ModdedStateGroups)
            {
                var stateGroup = new StateGroup { Name = moddedStateGroup, States = [] };
                AudioProject.StateGroups.Add(stateGroup);
            }
        }

        public void SortSoundBanksAlphabetically()
        {
            var sortedSoundBanks = AudioProject.SoundBanks.OrderBy(soundBank => soundBank.Name).ToList();

            AudioProject.SoundBanks.Clear();

            foreach (var soundBank in sortedSoundBanks)
                AudioProject.SoundBanks.Add(soundBank);
        }

        public void BuildStateGroupsWithModdedStatesRepository(ObservableCollection<StateGroup> moddedStateGroups, Dictionary<string, List<string>> stateGroupsWithModdedStatesRepository)
        {
            if (stateGroupsWithModdedStatesRepository == null)
                stateGroupsWithModdedStatesRepository = new Dictionary<string, List<string>>();
            else
                stateGroupsWithModdedStatesRepository.Clear();

            foreach (var stateGroup in moddedStateGroups)
            {
                if (stateGroup.States != null && stateGroup.States.Count > 0)
                {
                    foreach (var state in stateGroup.States)
                    {
                        if (!stateGroupsWithModdedStatesRepository.ContainsKey(stateGroup.Name))
                            stateGroupsWithModdedStatesRepository[stateGroup.Name] = new List<string>();

                        stateGroupsWithModdedStatesRepository[stateGroup.Name].Add(state.Name);
                    }
                }
            }
        }

        private AudioProjectDataModel GetAudioProjectWithoutUnusedObjects()
        {
            var usedSoundBanksList = AudioProject.SoundBanks
                .Where(soundBank => soundBank != null)
                .Select(soundBank =>
                {
                    var dialogueEvents = (soundBank.DialogueEvents ?? Enumerable.Empty<DialogueEvent>())
                        .Where(dialogueEvent => dialogueEvent.DecisionTree != null && dialogueEvent.DecisionTree.Count != 0)
                        .ToList();

                    var actionEvents = (soundBank.ActionEvents ?? Enumerable.Empty<ActionEvent>())
                        .Where(actionEvent => actionEvent.Sound != null || actionEvent.SoundContainer != null)
                        .ToList();

                    return new SoundBank
                    {
                        Name = soundBank.Name,
                        Type = soundBank.Type,
                        DialogueEvents = dialogueEvents.Count != 0
                            ? new ObservableCollection<DialogueEvent>(dialogueEvents)
                            : null,
                        ActionEvents = actionEvents.Count != 0
                            ? new ObservableCollection<ActionEvent>(actionEvents)
                            : null
                    };
                })
                .Where(soundBank => (soundBank.DialogueEvents != null && soundBank.DialogueEvents.Any()) || (soundBank.ActionEvents != null && soundBank.ActionEvents.Any()))
                .ToList();

            var soundBanksResult = usedSoundBanksList.Count != 0
                ? new ObservableCollection<SoundBank>(usedSoundBanksList)
                : null;

            var usedStateGroupsList = (AudioProject.StateGroups ?? new ObservableCollection<StateGroup>())
                .Where(stateGroup => stateGroup.States != null && stateGroup.States.Count != 0)
                .ToList();

            var stateGroupsResult = usedStateGroupsList.Count != 0
                ? new ObservableCollection<StateGroup>(usedStateGroupsList)
                : null;

            return new AudioProjectDataModel
            {
                Language = AudioProject.Language,
                SoundBanks = soundBanksResult,
                StateGroups = stateGroupsResult
            };
        }

        private void MergeSavedAudioProjectIntoAudioProjectWithUnusedItems(AudioProjectDataModel savedProject)
        {
            if (savedProject == null)
                return;

            if (!string.IsNullOrEmpty(savedProject.Language))
                AudioProject.Language = savedProject.Language;

            if (savedProject.SoundBanks != null)
            {
                foreach (var savedSoundBank in savedProject.SoundBanks)
                {
                    var soundBank = AudioProject.SoundBanks.FirstOrDefault(soundBank => soundBank.Name == savedSoundBank.Name);
                    if (soundBank != null)
                    {
                        if (savedSoundBank.DialogueEvents != null)
                        {
                            foreach (var savedDialogueEvent in savedSoundBank.DialogueEvents)
                            {
                                var dialogueEvent = soundBank.DialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Name == savedDialogueEvent.Name);
                                if (dialogueEvent != null)
                                    dialogueEvent.DecisionTree = savedDialogueEvent.DecisionTree;
                                else
                                    soundBank.DialogueEvents.Add(savedDialogueEvent);
                            }
                        }

                        if (savedSoundBank.ActionEvents != null)
                        {
                            foreach (var savedActionEvent in savedSoundBank.ActionEvents)
                            {
                                var actionEvent = soundBank.ActionEvents.FirstOrDefault(actionEvent => actionEvent.Name == savedActionEvent.Name);
                                if (actionEvent != null)
                                {
                                    if (actionEvent.Sound != null)
                                        savedActionEvent.Sound = savedActionEvent.Sound;
                                    else
                                        actionEvent.SoundContainer = savedActionEvent.SoundContainer;
                                }
                                else
                                    soundBank.ActionEvents.Add(savedActionEvent);
                            }
                        }
                    }
                    else
                        AudioProject.SoundBanks.Add(savedSoundBank);
                }
            }

            if (savedProject.StateGroups != null)
            {
                foreach (var savedStateGroup in savedProject.StateGroups)
                {
                    var stateGroup = AudioProject.StateGroups.FirstOrDefault(stateGroup => stateGroup.Name == savedStateGroup.Name);
                    if (stateGroup != null)
                    {
                        if (savedStateGroup.States != null && savedStateGroup.States.Count != 0)
                            stateGroup.States = savedStateGroup.States;
                    }
                    else
                        AudioProject.StateGroups.Add(savedStateGroup);
                }
            }
        }

        public void ResetAudioProject()
        {
            AudioProject = new AudioProjectDataModel();
        }
    }
}
