using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using CommonControls.BaseDialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Utilities;
using Editors.Audio.Shared.Wwise;
using Shared.Core.Misc;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V112;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.AudioExplorer
{
    public partial class AudioLanguage(Wh3Language language, bool isChecked = false) : ObservableObject
    {
        public Wh3Language Language { get; } = language;
        [ObservableProperty] private bool _isChecked = isChecked;
    }

    public partial class AudioExplorerViewModel : ObservableObject, IEditorInterface
    {
        private readonly IAudioRepository _audioRepository;
        private readonly SoundPlayer _soundPlayer;

        [ObservableProperty] private EventSelectionFilter _eventFilter;
        [ObservableProperty] private ObservableCollection<HircTreeItem> _treeList = [];
        [ObservableProperty] private HircTreeItem _selectedNode;
        [ObservableProperty] private string _selectedNodeText = string.Empty;
        [ObservableProperty] private ObservableCollection<AudioLanguage> _languages = [];
        [ObservableProperty] private ObservableCollection<Wh3Language> _selectedLanguages = [];
        [ObservableProperty] private bool _showEvents = true;
        [ObservableProperty] private bool _showDialogueEvents = true;
        [ObservableProperty] private bool _isPlaySoundButtonEnabled = false;

        public string DisplayName { get; set; } = "Audio Explorer";

        public AudioExplorerViewModel(IAudioRepository audioRepository, SoundPlayer soundPlayer)
        {
            _audioRepository = audioRepository;
            _soundPlayer = soundPlayer;

            // Remove SFX as we don't allow for filtering it out in the AudioRepository so we don't need to display it
            var languages = Enum.GetValues<Wh3Language>()
                .Where(language => language != Wh3Language.Sfx)
                .ToArray();
            Languages = new ObservableCollection<AudioLanguage>(
                languages.Select(language => new AudioLanguage(language, language == Wh3Language.EnglishUK))
            );

            Languages.CollectionChanged += OnLanguagesCollectionChanged;
            foreach (var language in Languages)
                language.PropertyChanged += OnAudioLanguageChanged;

            SetSelectedLanguages();
            LoadAudioRepositoryForSelectedLanguages();

            EventFilter = new EventSelectionFilter(_audioRepository, true, true);
            EventFilter.EventList.SelectedItemChanged += OnEventSelected;
        }

        partial void OnShowEventsChanged(bool value) => RefreshList();

        partial void OnShowDialogueEventsChanged(bool value) => RefreshList();

        partial void OnSelectedNodeChanged(HircTreeItem value) => OnNodeSelected(value);

        private void OnLanguagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (AudioLanguage item in e.NewItems)
                    item.PropertyChanged += OnAudioLanguageChanged;
            }

            if (e.OldItems != null)
            {
                foreach (AudioLanguage item in e.OldItems)
                    item.PropertyChanged -= OnAudioLanguageChanged;
            }

            SetSelectedLanguages();
        }

        private void OnAudioLanguageChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioLanguage.IsChecked))
                SetSelectedLanguages();
        }

        private void OnNodeSelected(HircTreeItem selectedNode)
        {
            IsPlaySoundButtonEnabled = selectedNode?.Item is ICAkSound or ICAkMusicTrack;

            SelectedNodeText = string.Empty;

            if (selectedNode == null || selectedNode.Item == null)
                return;

            var hircAsString = JsonSerializer.Serialize((object)selectedNode.Item, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, WriteIndented = true });
            SelectedNodeText = hircAsString;

            if (selectedNode.Item.HircType == AkBkHircType.Sound)
            {
                var findAudioParentStructureHelper = new FindAudioParentStructureHelper();
                var parentStructs = findAudioParentStructureHelper.Compute(selectedNode.Item, _audioRepository);

                SelectedNodeText += "\n\nParent structure:\n";
                foreach (var parentStruct in parentStructs)
                {
                    SelectedNodeText += "\t" + parentStruct.Description + "\n";
                    foreach (var graphItem in parentStruct.GraphItems)
                        SelectedNodeText += "\t\t" + graphItem.Description + "\n";

                    SelectedNodeText += "\n";
                }
            }
        }

        private void RefreshList() => EventFilter.Refresh(ShowEvents, ShowDialogueEvents);

        private void SetSelectedLanguages()
        {
            SelectedLanguages = new ObservableCollection<Wh3Language>(
                Languages.Where(audioLanguage => audioLanguage.IsChecked).Select(audioLanguage => audioLanguage.Language)
            );
        }

        [RelayCommand] public void LoadAudioRepositoryForSelectedLanguages()
        {
            var languages = SelectedLanguages
                .Select(Wh3LanguageInformation.GetLanguageAsString)
                .ToList();
            _audioRepository.Load(languages);
        }

        private void OnEventSelected(SelectedHircItem newValue)
        {
            if (newValue?.Id == SelectedNode?.Item?.Id)
                return;

            if (newValue != null)
            {
                SelectedNode = null;
                TreeList.Clear();

                var parser = new WwiseTreeParserChildren(_audioRepository);
                var rootNode = parser.BuildHierarchy(newValue.HircItem);
                TreeList.Add(rootNode);
            }
        }

        [RelayCommand] public void PlaySelectedSoundAction()
        {
            if (SelectedNode?.Item is ICAkSound sound)
            {
                if (sound.GetStreamType() == AKBKSourceType.Data_BNK)
                {
                    if (sound is CAkSound_V136)
                        _soundPlayer.PlayStreamedWem(sound.GetSourceId().ToString());
                    else if (sound is CAkSound_V112 sound_V112)
                    {
                        _soundPlayer.PlayDataWem(
                            sound_V112.AkBankSourceData.AkMediaInformation.SourceId,
                            sound_V112.AkBankSourceData.AkMediaInformation.FileId,
                            (int)sound_V112.AkBankSourceData.AkMediaInformation.FileOffset,
                            (int)sound_V112.AkBankSourceData.AkMediaInformation.InMemoryMediaSize
                        );
                    }
                }
                else
                    _soundPlayer.PlayStreamedWem(sound.GetSourceId().ToString());
            }
            else if (SelectedNode?.Item is ICAkMusicTrack musicTrack)
            {
                var musicTrackId = musicTrack.GetChildren().FirstOrDefault();
                _soundPlayer.PlayStreamedWem(musicTrackId.ToString());
            }
        }

        [RelayCommand] public void LoadHircFromIdAction()
        {
            var window = new TextInputWindow("Hirc Input", "Hirc ID", true);
            if (window.ShowDialog() == true)
            {
                var input = window.TextValue;
                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("No ID provided");
                    return;
                }

                if (uint.TryParse(input, out var hircId) == false)
                {
                    MessageBox.Show("Not a valid ID");
                    return;
                }

                var foundHircs = _audioRepository.GetHircs(hircId);
                if (foundHircs.Count == 0)
                {
                    MessageBox.Show($"No Hircs found with ID {hircId}");
                    return;
                }

                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, WriteIndented = true };
                var hircAsString = JsonSerializer.Serialize<object[]>(foundHircs.ToArray(), options);
                SelectedNodeText = hircAsString;
            }
        }

        // TODO: Should probably move this to Reports
        [RelayCommand] public void ExportStatePathsUsingState()
        {
            var window = new TextInputWindow("State Input", "State", true);
            if (window.ShowDialog() == true)
            {
                var input = window.TextValue;
                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("No id provided");
                    return;
                }

                if (!_audioRepository.NameById.ContainsKey(WwiseHash.Compute(input)))
                {
                    MessageBox.Show($"State {input} does not exist.");
                    return;
                }

                var filePath = $"{DirectoryHelper.ReportsDirectory}\\state_paths_using_{input}.txt";

                using var writer = new StreamWriter(filePath, false);

                var helper = new DecisionPathHelper(_audioRepository);
                var dialogueEvents = _audioRepository.GetHircsByType<ICAkDialogueEvent>();
                foreach (var dialogueEvent in dialogueEvents)
                {
                    var hircItem = dialogueEvent as HircItem;
                    var dialogueEventName = _audioRepository.GetNameFromId(hircItem.Id);

                    var decisionPathCollection = helper.GetDecisionPaths(dialogueEvent);
                    var stateGroups = decisionPathCollection.Header.GetAsString();
                    var processedDialogueEvents = new List<string>();

                    foreach (var statePath in decisionPathCollection.Paths)
                    {
                        var statePathString = statePath.GetAsString();
                        if (statePathString.Contains(input))
                        {
                            if (!processedDialogueEvents.Contains(dialogueEventName))
                            {
                                processedDialogueEvents.Add(dialogueEventName);
                                writer.WriteLine($"{dialogueEventName} [{stateGroups}]");
                                writer.WriteLine($"    {statePathString}");
                            }
                        }
                    }
                }
            }
        }

        public void Close()
        {
            EventFilter.EventList.SelectedItemChanged -= OnEventSelected;

            Languages.CollectionChanged -= OnLanguagesCollectionChanged;
            foreach (var item in Languages)
                item.PropertyChanged -= OnAudioLanguageChanged;
        }
    }
}
