using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Utilities;
using Editors.Audio.Shared.Wwise.HircExploration;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V112;

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
        [ObservableProperty] private string _wwiseObjectLabel;
        [ObservableProperty] private ObservableCollection<AudioLanguage> _languages = [];
        [ObservableProperty] private ObservableCollection<Wh3Language> _selectedLanguages = [];
        [ObservableProperty] private bool _searchByActionEvent = false;
        [ObservableProperty] private bool _searchByDialogueEvent = true;
        [ObservableProperty] private bool _searchByHircId = false;
        [ObservableProperty] private bool _searchByVOActor = false;
        [ObservableProperty] private bool _isPlayAudioButtonEnabled = false;

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

            WwiseObjectLabel = "Wwise Object Data";
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

        private void OnNodeSelected(HircTreeNode selectedNode)
        {
            IsPlayAudioButtonEnabled = selectedNode?.Hirc is ICAkSound or ICAkMusicTrack;

            SelectedNodeText = string.Empty;

            if (selectedNode == null || selectedNode.Hirc == null)
                return;

            var nodeName = selectedNode.DisplayName;
            if (nodeName.Contains("_"))
                nodeName = TableHelpers.DuplicateUnderscores(nodeName);
            WwiseObjectLabel = $"Wwise Object Data - {nodeName}";

            var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, WriteIndented = true };
            var hircAsString = JsonSerializer.Serialize((object)selectedNode.Hirc, options);
            SelectedNodeText = hircAsString;

            if (selectedNode.Hirc.HircType == AkBkHircType.Sound)
            {
                var parentStructures = SoundParentStructureParser.Compute(selectedNode.Hirc, _audioRepository);

                SelectedNodeText += "\n\nParent structure:\n";
                foreach (var parentStruct in parentStructures)
                {
                    SelectedNodeText += "\t" + parentStruct.Description + "\n";
                    foreach (var graphItem in parentStruct.GraphItems)
                        SelectedNodeText += "\t\t" + graphItem.Description + "\n";

                    SelectedNodeText += "\n";
                }
            }

            ExpandNodes(selectedNode);
        }

        private static void ExpandNodes(HircTreeNode selectedNode)
        {
            // Expand ancestors and collapse siblings at branching levels
            var currentNode = selectedNode;
            while (currentNode.Parent != null)
            {
                var parentNode = currentNode.Parent;

                parentNode.IsExpanded = true;

                if (parentNode.Children != null && parentNode.Children.Count > 1)
                {
                    foreach (var siblingNode in parentNode.Children)
                        siblingNode.IsExpanded = false;
                }

                currentNode.IsExpanded = true;
                currentNode = parentNode;
            }

            // Expand where there's only one child
            currentNode = selectedNode;
            while (currentNode.Children != null && currentNode.Children.Count == 1)
            {
                currentNode.IsExpanded = true;
                currentNode = currentNode.Children[0];
            }

            if (currentNode.Children != null && currentNode.Children.Count > 0)
                currentNode.IsExpanded = true;
        }

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

        private void RefreshList() => ExplorerFilter.Refresh(SearchByActionEvent, SearchByDialogueEvent, SearchByHircId, SearchByVOActor);

        private void SetSelectedLanguages()
        {
            SelectedLanguages = new ObservableCollection<Wh3Language>(Languages
                .Where(audioLanguage => audioLanguage.IsChecked)
                .Select(audioLanguage => audioLanguage.Language));
        }

        [RelayCommand] public void LoadAudioRepositoryForSelectedLanguages()
        {
            var languages = SelectedLanguages.Select(Wh3LanguageInformation.GetLanguageAsString).ToList();
            _audioRepository.Load(languages);
            Reset();
        }

        private void OnEventSelected(ExplorerListItem newValue)
        {
            if (newValue == null)
                return;

            if (newValue?.Id == SelectedNode?.Hirc?.Id)
                return;

            if (SearchByVOActor)
            {
                var wwiseTreeParserChildren = new HircTreeChildrenParser(_audioRepository);

                SelectedNode = null;
                TreeList.Clear();

                var dialogueEvents = _audioRepository.GetHircsByHircType(AkBkHircType.Dialogue_Event);
                foreach (var dialogueEvent in dialogueEvents)
                {
                    var dialogueEventRootNode = wwiseTreeParserChildren.BuildHierarchy(dialogueEvent);
                    if (FilterTreeByVOActor(dialogueEventRootNode, newValue.DisplayName))
                        TreeList.Add(dialogueEventRootNode);
                }

                return;
            }
            else
            {
                var wwiseTreeParserChildren = new HircTreeChildrenParser(_audioRepository);

                SelectedNode = null;
                TreeList.Clear();

                var rootNode = wwiseTreeParserChildren.BuildHierarchy(newValue.HircItem);
                rootNode.IsExpanded = true;

                TreeList.Add(rootNode);
            }
        }

        private static bool FilterTreeByVOActor(HircTreeNode currentNode, string voActor)
        {
            var currentNodeMatches = currentNode.DisplayName.Contains(voActor, StringComparison.OrdinalIgnoreCase);
            if (currentNodeMatches)
                return true;

            if (currentNode.Children == null || currentNode.Children.Count == 0)
                return false;

            var anyMatches = false;
            for (var i = currentNode.Children.Count - 1; i >= 0; i--)
            {
                var childNode = currentNode.Children[i];

                var isMatch = FilterTreeByVOActor(childNode, voActor);
                if (!isMatch)
                    currentNode.Children.RemoveAt(i);
                else
                    anyMatches = true;
            }

            return anyMatches;
        }

        [RelayCommand] public void PlayAudio()
        {
            // From at least V136 and newer, AkMediaInformation no longer stores and FileOffset. To get the wem data you would search the DidxChunk
            // for the SourceId. While some Warhammer 3 AkBankSourceData are AKBKSourceType.Data_BNK and therefore should appear in the DidxChunk,
            // no Warhammer 3 wems are in there and instead all wems are stored in Packs so they're actually AKBKSourceType.Streaming.
            // This could be explained by Wwiser's Enum for AKBKSourceType in V136 mapping incorrectly, or V136 not supporting data bnks but who knows?
            // So, as there are no data wems in Warhammer 3, functionality to find wem data in V136 is not implemented as they can only be streamed.
            if (SelectedNode?.Hirc is ICAkSound sound)
            {
                if (sound.GetStreamType() == AKBKSourceType.Data_BNK && sound is CAkSound_V112 sound_V112)
                {
                    _soundPlayer.PlayDataWem(
                        sound_V112.AkBankSourceData.AkMediaInformation.SourceId,
                        sound_V112.AkBankSourceData.AkMediaInformation.FileId,
                        (int)sound_V112.AkBankSourceData.AkMediaInformation.FileOffset,
                        (int)sound_V112.AkBankSourceData.AkMediaInformation.InMemoryMediaSize
                    );
                }
                else
                    _soundPlayer.PlayStreamedWem(sound.GetSourceId().ToString());
            }
            else if (SelectedNode?.Hirc is ICAkMusicTrack musicTrack)
            {
                // There is normally only one MusicTrack?
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
