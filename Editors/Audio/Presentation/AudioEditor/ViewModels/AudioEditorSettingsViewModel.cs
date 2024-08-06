using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Storage;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using static Editors.Audio.Presentation.AudioEditor.AudioEditorViewModelHelpers;

namespace Editors.Audio.Presentation.AudioEditor.ViewModels
{
    public partial class AudioEditorSettingsViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;

        //readonly ILogger _logger = Logging.Create<AudioEditorSettingsViewModel>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");

        [ObservableProperty] private string _audioProjectFileName = "my_audio_project";
        [ObservableProperty] private string _customStatesFileName = "my_custom_states";
        [ObservableProperty] private string _selectedAudioProjectEventType;
        [ObservableProperty] private string _selectedAudioProjectEventSubtype;
        [ObservableProperty] private DialogueEventsPreset _selectedAudioProjectEventsPreset;
        [ObservableProperty] private List<string> _audioProjectEventType = AudioEditorSettings.EventType;
        [ObservableProperty] private ObservableCollection<string> _audioProjectSubtypes = []; // Determined according to what Event Type is selected
        [ObservableProperty] private ObservableCollection<string> _audioProjectDialogueEvents = []; // The list of events in the Audio Project.

        public AudioEditorSettingsViewModel(IAudioRepository audioRepository, PackFileService packFileService, AudioEditorViewModel audioEditorViewModel)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
        }

        partial void OnSelectedAudioProjectEventTypeChanged(string value)
        {
            // Update the ComboBox for EventSubType upon EventType selection.
            UpdateAudioProjectEventSubType(this);
        }

        [RelayCommand] public void CreateAudioProject()
        {
            // Remove any pre-existing data.
            AudioEditorData.Instance.EventsData.Clear();
            _audioEditorViewModel.AudioEditorDataGridItems.Clear();
            _audioEditorViewModel.SelectedAudioProjectEvent = "";

            // Create the object for State Groups with qualifiers so that their keys in the EventsData dictionary are unique.
            AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

            // Initialise EventsData according to the Audio Project settings selected.
            InitialiseEventsData(this);

            // Add the Audio Project with empty events to the PackFile.
            AudioProjectData.AddAudioProjectToPackFile(_packFileService, AudioEditorData.Instance.EventsData, AudioProjectFileName);

            // Load the custom States so that they can be referenced when the Event is loaded.
            //PrepareCustomStatesForComboBox(this);
        }

        public void Close()
        {
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }
}
