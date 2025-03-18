using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioProjectCompiler;
using Editors.Audio.GameSettings.Warhammer3;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.StateGroups;

namespace Editors.Audio.AudioEditor
{
    public class AudioEditorService : IAudioEditorService
    {
        readonly ILogger _logger = Logging.Create<AudioEditorService>();

        private readonly IPackFileService _packFileService;
        private readonly IFileSaveService _fileSaveService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly IntegrityChecker _integrityChecker;
        private readonly CompilerDataProcessor _compilerDataProcessor;
        private readonly SoundBankGenerator _soundBankGenerator;
        private readonly WemGenerator _wemGenerator;
        private readonly DatGenerator _datGenerator;

        public AudioEditorService(
            IPackFileService packFileService,
            IFileSaveService fileSaveService,
            IStandardDialogs standardDialogs,
            IntegrityChecker integrityChecker,
            CompilerDataProcessor compilerDataProcessor,
            SoundBankGenerator soundBankGenerator,
            WemGenerator wemGenerator,
            DatGenerator datGenerator)
        {
            _packFileService = packFileService;
            _fileSaveService = fileSaveService;
            _standardDialogs = standardDialogs;
            _integrityChecker = integrityChecker;
            _compilerDataProcessor = compilerDataProcessor;
            _soundBankGenerator = soundBankGenerator;
            _wemGenerator = wemGenerator;
            _datGenerator = datGenerator;
        }

        public AudioProject AudioProject { get; set; }
        public string AudioProjectFileName { get; set; }
        public string AudioProjectDirectory { get; set; }
        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        public TreeNode SelectedAudioProjectTreeNode { get; set; }
        public ObservableCollection<Dictionary<string, string>> AudioProjectEditorDataGrid { get; set; }
        public ObservableCollection<Dictionary<string, string>> AudioProjectViewerDataGrid { get; set; }
        public Dictionary<string, List<string>> ModdedStatesByStateGroupLookup { get; set; } = [];
        public Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError { get; set; } = [];
        public Dictionary<string, DialogueEventPreset?> DialogueEventSoundBankFiltering { get; set; } = [];

        public void SaveAudioProject()
        {
            var audioProject = GetAudioProjectWithoutUnusedObjects();

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var audioProjectJson = JsonSerializer.Serialize(audioProject, options);
            var audioProjectFileName = $"{AudioProjectFileName}.aproj";
            var audioProjectFilePath = $"{AudioProjectDirectory}\\{audioProjectFileName}";
            var packFile = PackFile.CreateFromASCII(audioProjectFileName, audioProjectJson);
            _fileSaveService.Save(audioProjectFilePath, packFile.DataSource.ReadData(), true);

            _logger.Here().Information($"Saved Audio Project file: {AudioProjectDirectory}\\{AudioProjectFileName}.aproj");
        }

        public void LoadAudioProject(AudioEditorViewModel audioEditorViewModel)
        {
            var result = _standardDialogs.DisplayBrowseDialog([".aproj"]);
            if (result.Result)
            {
                var filePath = _packFileService.GetFullPath(result.File);
                var fileName = Path.GetFileName(filePath);
                var fileType = Path.GetExtension(filePath);
                var file = _packFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);

                audioEditorViewModel.AudioProjectExplorerViewModel.AudioProjectExplorerLabel = $"Audio Project Explorer - {DataGridHelpers.AddExtraUnderscoresToString(fileName)}";

                // Reset data
                audioEditorViewModel.ResetAudioEditorViewModelData();
                ResetAudioProject();

                // Set the AudioProject
                var savedProject = JsonSerializer.Deserialize<AudioProject>(audioProjectJson);
                AudioProjectFileName = fileName.Replace(fileType, string.Empty);
                AudioProjectDirectory = filePath.Replace($"\\{fileName}", string.Empty);

                // Initialise a full Audio Project and merge the saved Audio Project with it
                InitialiseAudioProject(AudioProjectFileName, AudioProjectDirectory, savedProject.Language);
                MergeSavedAudioProjectIntoAudioProjectWithUnusedItems(savedProject);

                // Initialise data after AudioProject is set so it uses the correct instance
                audioEditorViewModel.Initialise();

                // Get the Modded States and prepare them for being added to the DataGrid ComboBoxes
                BuildModdedStatesByStateGroupLookup(AudioProject.StateGroups, ModdedStatesByStateGroupLookup);

                _integrityChecker.CheckAudioProjectDialogueEventIntegrity(this);

                _logger.Here().Information($"Loaded Audio Project: {fileName}");
            }
        }

        public void InitialiseAudioProject(string fileName, string directory, string language)
        {
            AudioEditorViewModel.AudioProjectExplorerViewModel.AudioProjectExplorerLabel = $"Audio Project Explorer - {DataGridHelpers.AddExtraUnderscoresToString(fileName)}";

            AudioProjectFileName = fileName;
            AudioProjectDirectory = directory;
            AudioProject.Language = language;

            InitialiseSoundBanks();

            InitialiseModdedStatesGroups();

            SortSoundBanksAlphabetically();

            AudioEditorViewModel.AudioProjectExplorerViewModel.CreateAudioProjectTree();
        }

        public void CompileAudioProject()
        {
            var audioProject = GetAudioProjectWithoutUnusedObjects();

            SaveAudioProject();

            if (audioProject.SoundBanks == null)
                return;

            var audioProjectFileName = AudioProjectFileName.Replace(" ", "_");
            _compilerDataProcessor.SetSoundBankData(audioProject, audioProjectFileName);

            // We set the data from the bottom up, so Sounds, then Actions, then Events to ensure that IDs are generated before they're referenced.
            // For example IDs set in Sounds / Sound Containers are used in Actions, and IDs set in Actions are used in Events.
            _compilerDataProcessor.SetInitialSourceData(audioProject);

            if (audioProject.SoundBanks.Any(soundBank => soundBank.ActionEvents != null))
            {
                _compilerDataProcessor.SetActionData(audioProject);
                _compilerDataProcessor.SetActionEventData(audioProject);
            }

            if (audioProject.SoundBanks.Any(soundBank => soundBank.DialogueEvents != null))
            {
                _compilerDataProcessor.SetStatesData(audioProject);
                _compilerDataProcessor.SetDialogueEventData(audioProject);
            }

            _wemGenerator.GenerateWems(audioProject);

            _compilerDataProcessor.SetRemainingSourceData(audioProject);

            _wemGenerator.SaveWemsToPack(audioProject);

            _soundBankGenerator.GenerateSoundBanks(audioProject);

            _datGenerator.GenerateDatFiles(audioProject, audioProjectFileName);

            SaveCompiledAudioProjectToPack(audioProject);
        }

        private void InitialiseSoundBanks()
        {
            var soundBanks = Enum.GetValues<SoundBanks.Wh3SoundBankSubtype>()
                .Select(soundBankSubtype => new SoundBank
                {
                    Name = SoundBanks.GetSoundBankSubTypeString(soundBankSubtype),
                    SoundBankType = SoundBanks.GetSoundBankSubType(soundBankSubtype)
                })
                .ToList();

            AudioProject.SoundBanks = [];

            foreach (var soundBankSubtype in Enum.GetValues<SoundBanks.Wh3SoundBankSubtype>())
            {
                var soundBank = new SoundBank
                {
                    Name = SoundBanks.GetSoundBankSubTypeString(soundBankSubtype),
                    SoundBankType = SoundBanks.GetSoundBankSubType(soundBankSubtype)
                };

                if (soundBank.SoundBankType == SoundBanks.Wh3SoundBankType.ActionEventSoundBank)
                    soundBank.ActionEvents = [];
                else
                {
                    soundBank.DialogueEvents = [];

                    var filteredDialogueEvents = DialogueEventData
                        .Where(dialogueEvent => dialogueEvent.SoundBank == SoundBanks.GetSoundBankSubtype(soundBank.Name));

                    foreach (var dialogueData in filteredDialogueEvents)
                    {
                        var dialogueEvent = new DialogueEvent
                        {
                            Name = dialogueData.Name,
                            StatePaths = []
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

        public void BuildModdedStatesByStateGroupLookup(List<StateGroup> moddedStateGroups, Dictionary<string, List<string>> moddedStatesByStateGroupLookup)
        {
            if (moddedStatesByStateGroupLookup == null)
                moddedStatesByStateGroupLookup = new Dictionary<string, List<string>>();
            else
                moddedStatesByStateGroupLookup.Clear();

            foreach (var stateGroup in moddedStateGroups)
            {
                if (stateGroup.States != null && stateGroup.States.Count > 0)
                {
                    foreach (var state in stateGroup.States)
                    {
                        if (!moddedStatesByStateGroupLookup.ContainsKey(stateGroup.Name))
                            moddedStatesByStateGroupLookup[stateGroup.Name] = new List<string>();

                        moddedStatesByStateGroupLookup[stateGroup.Name].Add(state.Name);
                    }
                }
            }
        }

        private AudioProject GetAudioProjectWithoutUnusedObjects()
        {
            var usedSoundBanksList = AudioProject.SoundBanks
                .Where(soundBank => soundBank != null)
                .Select(soundBank =>
                {
                    var dialogueEvents = (soundBank.DialogueEvents ?? Enumerable.Empty<DialogueEvent>())
                        .Where(dialogueEvent => dialogueEvent.StatePaths != null && dialogueEvent.StatePaths.Count != 0)
                        .ToList();

                    var actionEvents = (soundBank.ActionEvents ?? Enumerable.Empty<ActionEvent>())
                        .Where(actionEvent => actionEvent.Sound != null || actionEvent.RandomSequenceContainer != null)
                        .ToList();

                    return new SoundBank
                    {
                        Name = soundBank.Name,
                        SoundBankType = soundBank.SoundBankType,
                        DialogueEvents = dialogueEvents.Count != 0
                            ? new List<DialogueEvent>(dialogueEvents)
                            : null,
                        ActionEvents = actionEvents.Count != 0
                            ? new List<ActionEvent>(actionEvents)
                            : null
                    };
                })
                .Where(soundBank => soundBank.DialogueEvents != null && soundBank.DialogueEvents.Any() || soundBank.ActionEvents != null && soundBank.ActionEvents.Any())
                .ToList();

            var soundBanksResult = usedSoundBanksList.Count != 0
                ? new List<SoundBank>(usedSoundBanksList)
                : null;

            var usedStateGroupsList = (AudioProject.StateGroups ?? new List<StateGroup>())
                .Where(stateGroup => stateGroup.States != null && stateGroup.States.Count != 0)
                .ToList();

            var stateGroupsResult = usedStateGroupsList.Count != 0
                ? new List<StateGroup>(usedStateGroupsList)
                : null;

            return new AudioProject
            {
                Language = AudioProject.Language,
                SoundBanks = soundBanksResult,
                StateGroups = stateGroupsResult
            };
        }

        private void MergeSavedAudioProjectIntoAudioProjectWithUnusedItems(AudioProject savedProject)
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
                                    dialogueEvent.StatePaths = savedDialogueEvent.StatePaths;
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
                                        actionEvent.RandomSequenceContainer = savedActionEvent.RandomSequenceContainer;
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

        private void SaveCompiledAudioProjectToPack(AudioProject audioProject)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var audioProjectJson = JsonSerializer.Serialize(audioProject, options);
            var audioProjectFileName = $"{AudioProjectFileName}_compiled.aproj";
            var audioProjectFilePath = $"{AudioProjectDirectory}\\{audioProjectFileName}";
            var packFile = PackFile.CreateFromASCII(audioProjectFileName, audioProjectJson);
            _fileSaveService.Save(audioProjectFilePath, packFile.DataSource.ReadData(), true);
        }

        public void ResetAudioProject()
        {
            AudioProject = new AudioProject();
        }
    }
}
