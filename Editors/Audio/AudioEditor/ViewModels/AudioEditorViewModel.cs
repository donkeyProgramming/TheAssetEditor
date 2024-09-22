using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Views;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioEditorSettings;

// TODO:
// Make some way of turning dialogue events into audio projects
// Update Audio compiler 
// Can't add row until all states and audio files are selected
// Modify the watermark combo box controls so that it doesn't highlight the selected item

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");
        private readonly string _dataGridBuilderName = "AudioProjectEditorDataGrid";
        private readonly string _dataGridNameName = "AudioProjectViewerDataGrid";

        // Audio Editor data.
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectViewerDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _selectedDataGridRows;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _copiedDataGridRows;
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;
        [ObservableProperty] public ObservableCollection<object> _audioProjectTreeViewItems;

        public object _selectedAudioProjectTreeItem;
        public object _previousSelectedAudioProjectTreeItem;

        // Audio Project configuration properties.
        [ObservableProperty] private string _selectedAudioType;
        [ObservableProperty] private string _selectedAudioSubtype;

        // Audio Project Configuration collections.
        [ObservableProperty] private ObservableCollection<Language> _languages = new(Enum.GetValues(typeof(Language)).Cast<Language>());
        [ObservableProperty] private ObservableCollection<AudioType> _audioProjectEventTypes = new(Enum.GetValues(typeof(AudioType)).Cast<AudioType>());
        [ObservableProperty] private ObservableCollection<AudioSubtype> _audioProjectSubtypes; // Determined according to what Event Type is selected
        [ObservableProperty] private ObservableCollection<DialogueEventCheckBox> _dialogueEventCheckBoxes; // The Dialogue Event CheckBoxes that are displayed in the Dialogue Events ListBox.
        
        // Audio Project Configuration button enablement.
        [ObservableProperty] private bool _isAnyAudioProjectItemChecked;
        [ObservableProperty] private bool _isAddToAudioProjectButtonEnabled;

        // Audio Project Builder properties.
        [ObservableProperty] private bool _showModdedStatesOnly;

        // UI visibility controls.
        [ObservableProperty] private bool _audioEditorVisibility = false;

        // UI enablement controls.
        [ObservableProperty] private bool _isAudioSubtypeEnabled = false;
        [ObservableProperty] private bool _isPlayAudioButtonEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = true;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;

        public AudioEditorViewModel(IAudioRepository audioRepository, PackFileService packFileService, IAudioProjectService audioProjectService)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;

            InitialiseCollections();

            AddQualifiersToStateGroups(_audioRepository.DialogueEventsWithStateGroups);

            AudioProjectViewerDataGrid.CollectionChanged += OnAudioProjectViewerDataGridChanged;
        }

        [RelayCommand] public void NewAudioProject()
        {
            NewAudioProjectWindow.Show(_packFileService, this, _audioProjectService);
        }

        [RelayCommand] public void SaveAudioProject()
        {
            _audioProjectService.SaveAudioProject(_packFileService);
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioProjectService.LoadAudioProject(_packFileService, _audioRepository, this);
        }

        public void SetAudioEditorVisibility(bool isVisible)
        {
            AudioEditorVisibility = isVisible;
        }

        public void ResetAudioEditorViewModelData()
        {
            AudioProjectEditorDataGrid = null;
            AudioProjectViewerDataGrid = null;
            SelectedDataGridRows = null;
            CopiedDataGridRows = null;
            DialogueEventCheckBoxes = null;
            _selectedAudioProjectTreeItem = null;
            _previousSelectedAudioProjectTreeItem = null;
        }

        public void InitialiseCollections()
        {
            AudioProjectEditorDataGrid = [];
            AudioProjectViewerDataGrid = [];
            SelectedDataGridRows = [];
            CopiedDataGridRows = [];
            AudioProjectSubtypes = [];
            DialogueEventCheckBoxes = [];

            AudioProjectTreeViewItems = _audioProjectService.AudioProject.AudioProjectTreeViewItems;
        }

        public void Close()
        {
            // Reset and initialise data.
            ResetAudioProjectConfiguration();
            ResetAudioEditorViewModelData();
            _audioProjectService.ResetAudioProject();
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }
}
