using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectEditor;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.AudioProjectViewer;
using Editors.Audio.AudioEditor.AudioSettings;
using Editors.Audio.AudioEditor.DataGrids;
using Editors.Audio.AudioProjectCompiler;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using TreeNode = Editors.Audio.AudioEditor.AudioProjectExplorer.TreeNode;

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
        public TreeNode SelectedExplorerNode { get; set; }
        public ObservableCollection<AudioFile> AudioFiles { get; set; } = [];
        public IAudioSettings AudioSettings { get; set; }





        public AudioEditorViewModel AudioEditorViewModel { get; set; }
        public AudioProjectExplorerViewModel AudioProjectExplorerViewModel { get; set; }
        public AudioFilesExplorerViewModel AudioFilesExplorerViewModel { get; set; }
        public AudioProjectEditorViewModel AudioProjectEditorViewModel { get; set; }
        public AudioProjectViewerViewModel AudioProjectViewerViewModel { get; set; }
        public AudioSettingsViewModel AudioSettingsViewModel { get; set; }
        public Dictionary<string, List<string>> ModdedStatesByStateGroupLookup { get; set; } = []; // TODO Replace this as it can just be extracted from the audio project directly when needed.

        public void SaveAudioProject(AudioProject audioProject, string audioProjectFileName, string audioProjectDirectoryPath)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var audioProjectJson = JsonSerializer.Serialize(audioProject, options);

            audioProjectFileName = $"{audioProjectFileName}.aproj";
            var audioProjectFilePath = $"{audioProjectDirectoryPath}\\{audioProjectFileName}";
            var packFile = PackFile.CreateFromASCII(audioProjectFileName, audioProjectJson);
            _fileSaveService.Save(audioProjectFilePath, packFile.DataSource.ReadData(), true);

            _logger.Here().Information($"Saved Audio Project file: {audioProjectFilePath}");
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
                audioEditorViewModel.ResetAudioEditorData();
                ResetAudioProject();

                // Set the AudioProject
                var loadedAudioProject = JsonSerializer.Deserialize<AudioProject>(audioProjectJson);
                loadedAudioProject.FileName = fileName.Replace(fileType, string.Empty);
                loadedAudioProject.DirectoryPath = filePath.Replace($"\\{fileName}", string.Empty);

                if (loadedAudioProject.Language == null)
                    loadedAudioProject.Language = "english(uk)"; // TODO: maybe replace this with the selected language in the app settings

                // Initialise a 'full' Audio Project to include unused stuff
                InitialiseAudioProject(loadedAudioProject.FileName, loadedAudioProject.DirectoryPath, loadedAudioProject.Language);
                MergeSavedAudioProjectIntoAudioProjectWithUnusedItems(loadedAudioProject);

                // Initialise data after AudioProject is set so it uses the correct instance
                audioEditorViewModel.InitialiseAudioEditorData();

                // Get the Modded States and prepare them for being added to the DataGrid ComboBoxes
                BuildModdedStatesByStateGroupLookup(AudioProject.StateGroups, ModdedStatesByStateGroupLookup);

                _integrityChecker.CheckAudioProjectDialogueEventIntegrity(this);

                _logger.Here().Information($"Loaded Audio Project: {fileName}");
            }
        }

        public void InitialiseAudioProject(string fileName, string directory, string language)
        {
            AudioEditorViewModel.AudioProjectExplorerViewModel.AudioProjectExplorerLabel = $"Audio Project Explorer - {DataGridHelpers.AddExtraUnderscoresToString(fileName)}";

            AudioProject = AudioProject.CreateAudioProject();
            AudioProject.FileName = fileName;
            AudioProject.DirectoryPath = directory;
            AudioProject.Language = language;

            AudioEditorViewModel.AudioProjectExplorerViewModel.CreateAudioProjectTree();
        }

        public void CompileAudioProject()
        {
            var audioProject = AudioProject.GetAudioProject(AudioProject);

            SaveAudioProject(audioProject, audioProject.FileName, audioProject.DirectoryPath);

            if (audioProject.SoundBanks == null)
                return;

            var audioProjectFileName = audioProject.FileName.Replace(" ", "_");
            _compilerDataProcessor.SetSoundBankData(audioProject);

            // We set the data from the bottom up, so Sounds, then Actions, then Events to ensure that Ids are generated before they're referenced.
            // For example Ids set in Sounds / Sound Containers are used in Actions, and Ids set in Actions are used in Events.
            _compilerDataProcessor.SetInitialSourceData(audioProject);

            if (audioProject.SoundBanks.Any(soundBank => soundBank.ActionEvents != null))
            {
                _compilerDataProcessor.CreateStopActionEvents(audioProject);
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

        public void BuildModdedStatesByStateGroupLookup(List<StateGroup> moddedStateGroups, Dictionary<string, List<string>> moddedStatesByStateGroupLookup)
        {
            if (moddedStatesByStateGroupLookup == null)
                moddedStatesByStateGroupLookup = [];
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

        // The Audio Project Explorer displays the entire Audio Project including unused items, but we only save the used items to the Audio Project file
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
            var audioProjectFileName = $"{audioProject.FileName}_compiled.json";
            var audioProjectFilePath = $"{audioProject.DirectoryPath}\\{audioProjectFileName}";
            var packFile = PackFile.CreateFromASCII(audioProjectFileName, audioProjectJson);
            _fileSaveService.Save(audioProjectFilePath, packFile.DataSource.ReadData(), true);
        }

        public void ResetAudioProject()
        {
            AudioProject = new AudioProject();
        }

        public DataTable GetEditorDataGrid()
        {
            return AudioProjectEditorViewModel.AudioProjectEditorDataGrid;
        }

        public DataTable GetViewerDataGrid()
        {
            return AudioProjectViewerViewModel.AudioProjectViewerDataGrid;
        }

        public List<DataRow> GetSelectedViewerRows()
        {
            return AudioProjectViewerViewModel._selectedDataGridRows;
        }
    }
}
