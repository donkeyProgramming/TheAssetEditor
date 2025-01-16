using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using static Editors.Audio.AudioEditor.IntegrityChecker;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using static Editors.Audio.GameSettings.Warhammer3.StateGroups;

namespace Editors.Audio.AudioEditor.Data.AudioProjectService
{
    public class AudioProjectService : IAudioProjectService
    {
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public AudioProjectData AudioProject { get; set; } = new AudioProjectData();
        public string AudioProjectFileName { get; set; }
        public string AudioProjectDirectory { get; set; }
        public Dictionary<string, List<string>> StateGroupsWithModdedStatesRepository { get; set; } = [];
        public Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError { get; set; } = [];

        public void SaveAudioProject(IPackFileService packFileService)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            var fileJson = JsonSerializer.Serialize(AudioProject, options);
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
                AudioProject = JsonSerializer.Deserialize<AudioProjectData>(audioProjectJson);
                AudioProjectFileName = fileName.Replace(fileType, string.Empty);
                AudioProjectDirectory = filePath.Replace($"\\{fileName}", string.Empty);

                // Initialise data after AudioProject is set so it uses the correct instance
                audioEditorViewModel.Initialise();

                // Get the Modded States and prepare them for being added to the DataGrid ComboBoxes
                BuildStateGroupsWithModdedStatesRepository(AudioProject.States, StateGroupsWithModdedStatesRepository);

                CheckAudioProjectDialogueEventIntegrity(audioRepository, this);

                TreeViewBuilder.AddAllDialogueEventsToSoundBankTreeViewItems(AudioProject, audioEditorViewModel.AudioProjectExplorerViewModel.ShowEditedDialogueEventsOnly);

                // Update AudioProjectTreeViewItems
                TreeViewBuilder.AddAllSoundBanksToTreeViewItemsWrappers(this);

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

            TreeViewBuilder.AddAllDialogueEventsToSoundBankTreeViewItems(AudioProject, audioEditorViewModel.AudioProjectExplorerViewModel.ShowEditedDialogueEventsOnly);

            SortSoundBanksAlphabetically();

            TreeViewBuilder.AddAllSoundBanksToTreeViewItemsWrappers(this);
        }

        private void InitialiseSoundBanks()
        {
            var soundBanks = Enum.GetValues<GameSoundBank>()
                .Select(soundBank => new SoundBank
                {
                    Name = GetDisplayString(soundBank),
                    Type = GetSoundBankType(soundBank).ToString(),
                    DialogueEvents = new ObservableCollection<DialogueEvent>()
                })
                .ToList();

            foreach (var soundBank in soundBanks)
            {
                AudioProject.SoundBanks.Add(soundBank);

                var dialogueEvents = DialogueEventData.Where(dialogueEvent => dialogueEvent.SoundBank == GetSoundBank(soundBank.Name))
                    .Select(dialogueEvent => new DialogueEvent
                    {
                        Name = dialogueEvent.Name
                    });

                foreach (var dialogueEvent in dialogueEvents)
                    soundBank.DialogueEvents.Add(dialogueEvent);
            }
        }

        private void InitialiseModdedStatesGroups()
        {
            foreach (var moddedStateGroup in ModdedStateGroups)
            {
                var stateGroup = new StateGroup { Name = moddedStateGroup };
                AudioProject.States.Add(stateGroup);
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

        public void ResetAudioProject()
        {
            AudioProject = new AudioProjectData();
        }
    }
}
