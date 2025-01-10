using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.Views;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManager;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.CopyPasteHandler;
using static Editors.Audio.AudioEditor.DataGrids.SingleRowDataGridConfiguration;
using static Editors.Audio.AudioEditor.DialogueEventFilter;
using static Editors.Audio.AudioEditor.IntegrityChecker;
using static Editors.Audio.AudioEditor.TreeViewItemLoader;
using static Editors.Audio.AudioEditor.TreeViewWrapper;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using static Editors.Audio.Utility.SoundPlayer;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class AudioEditorViewModel : ObservableObject, IEditorInterface
    {
        private readonly IAudioRepository _audioRepository;
        private readonly IPackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;

        public AudioSettingsViewModel AudioSettingsViewModel { get; set; } = new();

        public string DisplayName { get; set; } = "Audio Editor";

        // Audio Editor data
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorSingleRowDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _audioProjectEditorFullDataGrid;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _selectedDataGridRows;
        [ObservableProperty] private ObservableCollection<Dictionary<string, object>> _copiedDataGridRows;
        [ObservableProperty] public ObservableCollection<SoundBank> _soundBanks;
        [ObservableProperty] public ObservableCollection<object> _audioProjectTreeViewItems;

        // Audio Project Explorer
        [ObservableProperty] private string _audioProjectExplorerLabel = "Audio Project Explorer";
        public object _selectedAudioProjectTreeItem;
        public object _previousSelectedAudioProjectTreeItem;
        [ObservableProperty] private string _selectedDialogueEventPreset;
        [ObservableProperty] private bool _showEditedSoundBanksOnly;
        [ObservableProperty] private bool _showEditedDialogueEventsOnly;
        [ObservableProperty] private bool _isDialogueEventPresetFilterEnabled = false;
        [ObservableProperty] private ObservableCollection<GameSoundBank> _dialogueEventSoundBanks = new(Enum.GetValues<GameSoundBank>().Where(soundBank => GetSoundBankType(soundBank) == GameSoundBankType.DialogueEventSoundBank));
        [ObservableProperty] private ObservableCollection<string> _dialogueEventPresets;
        public Dictionary<string, string> DialogueEventSoundBankFiltering { get; set; } = [];

        // Audio Project Editor
        [ObservableProperty] private string _audioProjectEditorLabel = "Audio Project Editor";
        [ObservableProperty] private string _audioProjectViewerLabel = "Audio Project Viewer";
        [ObservableProperty] private string _audioProjectEditorSingleRowDataGridTag = "AudioProjectEditorSingleRowDataGrid";
        [ObservableProperty] private string _audioProjectEditorFullDataGridTag = "AudioProjectEditorFullDataGrid";
        [ObservableProperty] private bool _showModdedStatesOnly;
        [ObservableProperty] private bool _isAddRowButtonEnabled = false;
        [ObservableProperty] private bool _isUpdateRowButtonEnabled = false;
        [ObservableProperty] private bool _isRemoveRowButtonEnabled = false;
        [ObservableProperty] private bool _isPlayAudioButtonEnabled = false;
        [ObservableProperty] private bool _isShowModdedStatesCheckBoxEnabled = false;
        [ObservableProperty] private bool _isPasteEnabled = true;

        public AudioEditorViewModel(IAudioRepository audioRepository, IPackFileService packFileService, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;

            Initialise();

            CheckAudioEditorDialogueEventIntegrity(_audioRepository, DialogueEventData);
        }

        public void OnDataGridSelectionChanged(IList selectedItems)
        {
            SetButtonEnablement(this, _audioProjectService, selectedItems);
        }

        partial void OnAudioProjectEditorFullDataGridChanged(ObservableCollection<Dictionary<string, object>> value)
        {
            if (AudioProjectEditorFullDataGrid != null)
            {
                AudioProjectEditorFullDataGrid.CollectionChanged += AudioProjectEditorFullDataGrid_CollectionChanged;
                OnAudioProjectEditorFullDataGridChanged();
            }
        }

        private void AudioProjectEditorFullDataGrid_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnAudioProjectEditorFullDataGridChanged();
        }

        private void OnAudioProjectEditorFullDataGridChanged()
        {
            if (AudioProjectEditorFullDataGrid != null && AudioProjectEditorFullDataGrid.Count > 0)
                SetIsPasteEnabled(this, _audioProjectService, _audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
        }

        partial void OnSelectedDialogueEventPresetChanged(string value)
        {
            ApplyDialogueEventPresetFiltering(this, _audioProjectService);
        }

        partial void OnShowEditedSoundBanksOnlyChanged(bool value)
        {
            if (value == true)
                AddEditedSoundBanksToAudioProjectTreeViewItemsWrappers(_audioProjectService);
            else if (value == false)
                AddAllSoundBanksToTreeViewItemsWrappers(_audioProjectService);
        }

        partial void OnShowEditedDialogueEventsOnlyChanged(bool value)
        {
            AddEditedDialogueEventsToSoundBankTreeViewItems(_audioProjectService.AudioProject, DialogueEventSoundBankFiltering, ShowEditedDialogueEventsOnly);
        }

        partial void OnShowModdedStatesOnlyChanged(bool value)
        {
            if (_selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                // Clear the previous DataGrid Data
                ClearDataGrid(AudioProjectEditorSingleRowDataGrid);

                ConfigureAudioProjectEditorSingleRowDataGridForDialogueEvent(this, _audioRepository, selectedDialogueEvent, _audioProjectService);
                SetAudioProjectEditorSingleRowDataGridToDialogueEvent(AudioProjectEditorSingleRowDataGrid, _audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups, selectedDialogueEvent);
            }
        }

        public void OnSelectedAudioProjectTreeViewItemChanged(object value)
        {
            // Store the previous selected item
            if (_selectedAudioProjectTreeItem != null)
                _previousSelectedAudioProjectTreeItem = _selectedAudioProjectTreeItem;
            _selectedAudioProjectTreeItem = value;

            HandleSelectedTreeViewItem(this, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void NewAudioProject()
        {
            NewAudioProjectWindow.Show(_packFileService, this, _audioProjectService, _packFileUiProvider);
        }

        [RelayCommand] public void SaveAudioProject()
        {
            _audioProjectService.SaveAudioProject(_packFileService);
        }

        [RelayCommand] public void LoadAudioProject()
        {
            _audioProjectService.LoadAudioProject(_packFileService, _audioRepository, this, _packFileUiProvider);
        }

        [RelayCommand] public void ResetFiltering()
        {
            // Workaround for using ref with the MVVM toolkit as you can't pass a property by ref, so instead pass a field that is set to the property by ref then assign the ref field to the property
            var selectedDialogueEventPreset = SelectedDialogueEventPreset;
            ResetDialogueEventFiltering(DialogueEventSoundBankFiltering, ref selectedDialogueEventPreset, _audioProjectService);
            SelectedDialogueEventPreset = selectedDialogueEventPreset;

            AddAllDialogueEventsToSoundBankTreeViewItems(_audioProjectService.AudioProject, ShowEditedDialogueEventsOnly);
        }

        [RelayCommand] public void UpdateAudioProjectEditorFullDataGridRow()
        {
            HandleUpdatingRowData(this, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void RemoveAudioProjectEditorFullDataGridRow()
        {
            HandleRemovingRowData(this, _audioProjectService, _audioRepository);
        }

        [RelayCommand] public void PlayRandomAudioFile()
        {
            if (SelectedDataGridRows[0].TryGetValue("AudioFiles", out var audioFilesObj) && audioFilesObj is List<string> audioFiles && audioFiles.Any())
            {
                var random = new Random();
                var randomIndex = random.Next(audioFiles.Count);
                var randomAudioFile = audioFiles[randomIndex];
                PlayWavFile(randomAudioFile);
            }
        }

        [RelayCommand] public void CopyRows()
        {
            if (_selectedAudioProjectTreeItem is DialogueEvent)
                CopyDialogueEventRows(this, _audioProjectService, _audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
        }

        [RelayCommand] public void PasteRows()
        {
            if (IsPasteEnabled && _selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
                PasteDialogueEventRows(this, _audioProjectService, selectedDialogueEvent, _audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups);
        }

        [RelayCommand] public void AddRowFromAudioProjectEditorSingleRowDataGridToFullDataGrid()
        {
            if (AudioProjectEditorSingleRowDataGrid.Count == 0)
                return;

            HandleAddingRowData(this, _audioProjectService, _audioRepository);
        }

        public void ResetAudioEditorViewModelData()
        {
            AudioProjectEditorSingleRowDataGrid = null;
            AudioProjectEditorFullDataGrid = null;
            SelectedDataGridRows = null;
            CopiedDataGridRows = null;
            _selectedAudioProjectTreeItem = null;
            _previousSelectedAudioProjectTreeItem = null;
            AudioProjectTreeViewItems.Clear();
            DialogueEventSoundBankFiltering.Clear();
        }

        public void Initialise()
        {
            AudioProjectEditorSingleRowDataGrid = [];
            AudioProjectEditorFullDataGrid = [];
            SelectedDataGridRows = [];
            CopiedDataGridRows = [];
            DialogueEventPresets = [];
            AudioProjectTreeViewItems = _audioProjectService.AudioProject.AudioProjectTreeViewItems;
        }

        public void Close()
        {
            ResetAudioEditorViewModelData();
            _audioProjectService.ResetAudioProject();
        }
    }
}
