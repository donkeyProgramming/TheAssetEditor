using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioProjectExplorer.DialogueEventFilter;
using static Editors.Audio.AudioEditor.AudioProjectExplorer.TreeViewBuilder;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public partial class AudioProjectExplorerViewModel : ObservableObject, IEditorInterface
    {
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;

        public string DisplayName { get; set; } = "Audio Project Explorer";

        [ObservableProperty] private string _audioProjectExplorerLabel = "Audio Project Explorer";
        public object _selectedAudioProjectTreeItem;
        public object _previousSelectedAudioProjectTreeItem;
        [ObservableProperty] private string _selectedDialogueEventPreset;
        [ObservableProperty] private bool _showEditedSoundBanksOnly;
        [ObservableProperty] private bool _showEditedDialogueEventsOnly;
        [ObservableProperty] private bool _isDialogueEventPresetFilterEnabled = false;
        [ObservableProperty] private ObservableCollection<GameSoundBank> _dialogueEventSoundBanks = new(Enum.GetValues<GameSoundBank>().Where(soundBank => GetSoundBankType(soundBank) == GameSoundBankType.DialogueEventSoundBank));
        [ObservableProperty] private ObservableCollection<string> _dialogueEventPresets;
        [ObservableProperty] public ObservableCollection<object> _audioProjectTreeViewItems;


        public Dictionary<string, string> DialogueEventSoundBankFiltering { get; set; } = [];

        public AudioProjectExplorerViewModel(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
        }

        public void OnSelectedAudioProjectTreeViewItemChanged(object value)
        {
            // Store the previous selected item
            if (_selectedAudioProjectTreeItem != null)
                _previousSelectedAudioProjectTreeItem = _selectedAudioProjectTreeItem;
            _selectedAudioProjectTreeItem = value;

            AudioProjectItemLoader.HandleSelectedTreeViewItem(_audioEditorViewModel, _audioProjectService, _audioRepository);
        }

        partial void OnSelectedDialogueEventPresetChanged(string value)
        {
            ApplyDialogueEventPresetFiltering(_audioEditorViewModel, _audioProjectService);
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

        [RelayCommand] public void ResetFiltering()
        {
            // Workaround for using ref with the MVVM toolkit as you can't pass a property by ref, so instead pass a field that is set to the property by ref then assign the ref field to the property
            var selectedDialogueEventPreset = SelectedDialogueEventPreset;
            ResetDialogueEventFiltering(DialogueEventSoundBankFiltering, ref selectedDialogueEventPreset, _audioProjectService);
            SelectedDialogueEventPreset = selectedDialogueEventPreset;

            AddAllDialogueEventsToSoundBankTreeViewItems(_audioProjectService.AudioProject, ShowEditedDialogueEventsOnly);
        }

        public void Close()
        {

        }
    }
}
