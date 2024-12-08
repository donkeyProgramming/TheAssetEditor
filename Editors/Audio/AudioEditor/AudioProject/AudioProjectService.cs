using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManagerHelpers;
using static Editors.Audio.AudioEditor.DialogueEventFilter;
using static Editors.Audio.AudioEditor.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.AudioEditor.GameSettings.Warhammer3.SoundBanks;
using static Editors.Audio.AudioEditor.GameSettings.Warhammer3.StateGroups;
using static Editors.Audio.AudioEditor.IntegrityChecker;
using static Editors.Audio.AudioEditor.TreeViewWrapper;

namespace Editors.Audio.AudioEditor.AudioProject
{
    public interface IAudioProjectService
    {
        AudioProjectData AudioProject { get; set; }
        Dictionary<string, List<string>> StateGroupsWithModdedStatesRepository { get; set; }
        Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository { get; set; }
        Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithIntegrityError {  get; set; }
        void SaveAudioProject(IPackFileService packFileService);
        void LoadAudioProject(IPackFileService packFileService, IAudioRepository audioRepository, AudioEditorViewModel audioEditorViewModel, IStandardDialogs packFileUiProvider);
        void InitialiseAudioProject(AudioEditorViewModel audioEditorViewModel, string fileName, string directory, string language);
        void BuildStateGroupsWithModdedStatesRepository(ObservableCollection<StateGroup> moddedStateGroups, Dictionary<string, List<string>> stateGroupsWithModdedStatesRepository);
        void BuildDialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository(Dictionary<string, List<string>> dialogueEventsWithStateGroups, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository);
        void ResetAudioProject();
        string AudioProjectFileName { get; set; }
        string AudioProjectDirectory { get; set; }
    }

    public class AudioProjectService : IAudioProjectService
    {
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public AudioProjectData AudioProject { get; set; } = new AudioProjectData();
        public string AudioProjectFileName { get; set; }
        public string AudioProjectDirectory { get; set; }
        public Dictionary<string, List<string>> StateGroupsWithModdedStatesRepository { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository { get; set; } = [];
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

            // Add file to pack
            var fileEntry = new NewPackFileEntry(AudioProjectDirectory, PackFile.CreateFromASCII(fileName, fileJson));
            packFileService.AddFilesToPack(pack, [fileEntry]);

            _logger.Here().Information($"Saved Audio Project file: {AudioProjectDirectory}\\{AudioProjectFileName}.aproj");
        }

        public void LoadAudioProject(IPackFileService packFileService, IAudioRepository audioRepository, AudioEditorViewModel audioEditorViewModel, IStandardDialogs packFileUiProvider)
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

                audioEditorViewModel.AudioProjectExplorerLabel = $"Audio Project Explorer - {AddExtraUnderscoresToString(fileName)}";

                // Reset data.
                audioEditorViewModel.ResetAudioEditorViewModelData();
                ResetAudioProject();

                // Set the AudioProject.
                AudioProject = JsonSerializer.Deserialize<AudioProjectData>(audioProjectJson);
                AudioProjectFileName = fileName.Replace(fileType, string.Empty);
                AudioProjectDirectory = filePath.Replace($"\\{fileName}", string.Empty);

                // Initialise data. This must be done after AudioProject is set so it uses the correct instance.
                audioEditorViewModel.InitialiseCollections();

                // Get the Modded States and prepare them for being added to the DataGrid ComboBoxes.
                BuildStateGroupsWithModdedStatesRepository(AudioProject.ModdedStates, StateGroupsWithModdedStatesRepository);

                CheckAudioProjectDialogueEventIntegrity(audioRepository, this);

                AddAllDialogueEventsToSoundBankTreeViewItems(AudioProject, audioEditorViewModel.ShowEditedDialogueEventsOnly);

                // Update AudioProjectTreeViewItems.
                AddAllSoundBanksToTreeViewItemsWrappers(this);

                _logger.Here().Information($"Loaded Audio Project: {fileName}");
            }
        }

        public void InitialiseAudioProject(AudioEditorViewModel audioEditorViewModel, string fileName, string directory, string language)
        {
            audioEditorViewModel.AudioProjectExplorerLabel = $"Audio Project Explorer - {AddExtraUnderscoresToString(fileName)}";

            AudioProjectFileName = fileName;
            AudioProjectDirectory = directory;
            AudioProject.Language = language;

            InitialiseSoundBanks(AudioProject);

            InitialiseModdedStatesGroups(AudioProject.ModdedStates);

            AddAllDialogueEventsToSoundBankTreeViewItems(AudioProject, audioEditorViewModel.ShowEditedDialogueEventsOnly);

            SortSoundBanksAlphabetically(AudioProject.SoundBanks);

            AddAllSoundBanksToTreeViewItemsWrappers(this);
        }

        private static void InitialiseSoundBanks(AudioProjectData audioProject)
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
                audioProject.SoundBanks.Add(soundBank);

                var dialogueEvents = DialogueEventData.Where(dialogueEvent => dialogueEvent.SoundBank == GetSoundBank(soundBank.Name))
                    .Select(dialogueEvent => new DialogueEvent
                    {
                        Name = dialogueEvent.Name
                    });

                foreach (var dialogueEvent in dialogueEvents)
                    soundBank.DialogueEvents.Add(dialogueEvent);
            }
        }

        private static void InitialiseModdedStatesGroups(ObservableCollection<StateGroup> moddedStates)
        {
            foreach (var moddedStateGroup in ModdedStateGroups)
            {
                var stateGroup = new StateGroup { Name = moddedStateGroup };
                moddedStates.Add(stateGroup);
            }
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

        // Add qualifiers to State Groups so that dictionary keys are unique as some events have the same State Group twice e.g. VO_Actor.
        public void BuildDialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository(Dictionary<string, List<string>> dialogueEventsWithStateGroups, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository)
        {
            foreach (var dialogueEvent in dialogueEventsWithStateGroups)
            {
                var stateGroupsWithQualifiers = new Dictionary<string, string>();
                var stateGroups = dialogueEvent.Value;

                var voActorCount = 0;
                var voCultureCount = 0;

                foreach (var stateGroup in stateGroups)
                {
                    if (stateGroup == "VO_Actor")
                    {
                        voActorCount++;

                        var qualifier = voActorCount > 1 ? "VO_Actor (Reference)" : "VO_Actor (Source)";
                        stateGroupsWithQualifiers[qualifier] = "VO_Actor";
                    }
                    else if (stateGroup == "VO_Culture")
                    {
                        voCultureCount++;

                        var qualifier = voCultureCount > 1 ? "VO_Culture (Reference)" : "VO_Culture (Source)";
                        stateGroupsWithQualifiers[qualifier] = "VO_Culture";
                    }
                    else
                    {
                        // No qualifier needed, add the same state group as both original and qualified
                        stateGroupsWithQualifiers[stateGroup] = stateGroup;
                    }
                }

                dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository[dialogueEvent.Key] = stateGroupsWithQualifiers;
            }
        }

        public void ResetAudioProject()
        {
            AudioProject = new AudioProjectData();
        }
    }
}
