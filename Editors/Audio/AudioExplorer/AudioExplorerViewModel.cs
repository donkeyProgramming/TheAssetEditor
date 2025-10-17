using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Utilities;
using Editors.Audio.Shared.Wwise.HircExploration;
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

        [ObservableProperty] private ExplorerListSelectionFilter _explorerFilter;
        [ObservableProperty] private ObservableCollection<HircTreeNode> _treeList = [];
        [ObservableProperty] private HircTreeNode _selectedNode;
        [ObservableProperty] private string _selectedNodeText = string.Empty;
        [ObservableProperty] private ObservableCollection<AudioLanguage> _languages = [];
        [ObservableProperty] private ObservableCollection<Wh3Language> _selectedLanguages = [];
        [ObservableProperty] private bool _searchByActionEvent = false;
        [ObservableProperty] private bool _searchByDialogueEvent = true;
        [ObservableProperty] private bool _searchByHircId = false;
        [ObservableProperty] private bool _searchByVOActor = false;
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

            ExplorerFilter = new ExplorerListSelectionFilter(_audioRepository, SearchByActionEvent, SearchByDialogueEvent, SearchByHircId, SearchByVOActor);
            ExplorerFilter.ExplorerList.SelectedItemChanged += OnEventSelected;
        }

        partial void OnSearchByActionEventChanged(bool value)
        {
            Reset();

            if (SearchByActionEvent)
            {
                SearchByDialogueEvent = false;
                SearchByHircId = false;
                SearchByVOActor = false;
            }

            RefreshList();
        }

        partial void OnSearchByDialogueEventChanged(bool value)
        {
            Reset();

            if (SearchByDialogueEvent)
            {
                SearchByActionEvent = false;
                SearchByHircId = false;
                SearchByVOActor = false;
            }

            RefreshList();
        }

        partial void OnSearchByHircIdChanged(bool value)
        {
            Reset();

            if (SearchByHircId)
            {
                SearchByActionEvent = false;
                SearchByDialogueEvent = false;
                SearchByVOActor = false;
            }

            RefreshList();
        }

        partial void OnSearchByVOActorChanged(bool value)
        {
            Reset();

            if (SearchByVOActor)
            {
                SearchByActionEvent = false;
                SearchByDialogueEvent = false;
                SearchByHircId = false;
            }

            RefreshList();
        }

        partial void OnSelectedNodeChanged(HircTreeNode value) => OnNodeSelected(value);

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

        private void OnNodeSelected(HircTreeNode selectedNode)
        {
            IsPlaySoundButtonEnabled = selectedNode?.Item is ICAkSound or ICAkMusicTrack;

            SelectedNodeText = string.Empty;

            if (selectedNode == null || selectedNode.Item == null)
                return;

            var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, WriteIndented = true };
            var hircAsString = JsonSerializer.Serialize((object)selectedNode.Item, options);
            SelectedNodeText = hircAsString;

            if (selectedNode.Item.HircType == AkBkHircType.Sound)
            {
                var parentStructures = SoundParentStructureParser.Compute(selectedNode.Item, _audioRepository);

                SelectedNodeText += "\n\nParent structure:\n";
                foreach (var parentStruct in parentStructures)
                {
                    SelectedNodeText += "\t" + parentStruct.Description + "\n";
                    foreach (var graphItem in parentStruct.GraphItems)
                        SelectedNodeText += "\t\t" + graphItem.Description + "\n";

                    SelectedNodeText += "\n";
                }
            }
        }

        private void RefreshList() => ExplorerFilter.Refresh(SearchByActionEvent, SearchByDialogueEvent, SearchByHircId, SearchByVOActor);

        private void SetSelectedLanguages()
        {
            SelectedLanguages = new ObservableCollection<Wh3Language>(Languages.Where(audioLanguage => audioLanguage.IsChecked).Select(audioLanguage => audioLanguage.Language));
        }

        [RelayCommand] public void LoadAudioRepositoryForSelectedLanguages()
        {
            var languages = SelectedLanguages
                .Select(Wh3LanguageInformation.GetLanguageAsString)
                .ToList();
            _audioRepository.Load(languages);
            Reset();
        }

        private void OnEventSelected(ExplorerListItem newValue)
        {
            if (newValue == null)
                return;

            if (newValue?.Id == SelectedNode?.Item?.Id)
                return;

            if (SearchByVOActor)
            {
                var wwiseTreeParserChildren = new HircTreeChildrenParser(_audioRepository);
                var statePathParser = new StatePathParser(_audioRepository);

                SelectedNode = null;
                TreeList.Clear();

                var dialogueEvents = _audioRepository.GetHircsByHircType(AkBkHircType.Dialogue_Event);
                foreach (var dialogueEvent in dialogueEvents)
                {
                    var dialogueEventRootNode = wwiseTreeParserChildren.BuildHierarchy(dialogueEvent);
                    var matchingChildren = dialogueEventRootNode.Children
                        .Where(child => child.DisplayName.Contains(newValue.DisplayName))
                        .ToList();

                    if (matchingChildren.Count > 0)
                    {
                        dialogueEventRootNode.Children = matchingChildren;
                        TreeList.Add(dialogueEventRootNode);
                    }
                }
            }
            else 
            {
                var wwiseTreeParserChildren = new HircTreeChildrenParser(_audioRepository);

                SelectedNode = null;
                TreeList.Clear();

                var rootNode = wwiseTreeParserChildren.BuildHierarchy(newValue.HircItem);
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

        private void Reset()
        {
            if (ExplorerFilter != null)
            {
                ExplorerFilter.ExplorerList.SelectedItemChanged -= OnEventSelected;
                ExplorerFilter.ExplorerList.SelectedItem = null;
                ExplorerFilter.ExplorerList.Filter = string.Empty;
                ExplorerFilter.ExplorerList.SelectedItemChanged += OnEventSelected;
                ExplorerFilter.ExplorerList.UpdatePossibleValues([]);
            }

            SelectedNode = null;
            SelectedNodeText = string.Empty;
            TreeList.Clear();
        }

        public void Close()
        {
            ExplorerFilter.ExplorerList.SelectedItemChanged -= OnEventSelected;

            Languages.CollectionChanged -= OnLanguagesCollectionChanged;
            foreach (var item in Languages)
                item.PropertyChanged -= OnAudioLanguageChanged;
        }
    }
}
