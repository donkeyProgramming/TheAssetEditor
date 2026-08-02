using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.HircExploration;
using Editors.Audio.WaveformVisualiser.Presentation;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V112;
using Shared.Ui.Common;

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

        public WaveformVisualiserViewModel WaveformVisualiserViewModel { get; }

        public string DisplayName { get; set; } = "Audio Explorer";

        public AudioExplorerViewModel(IAudioRepository audioRepository, WaveformVisualiserViewModel waveformVisualiserViewModel)
        {
            _audioRepository = audioRepository;
            WaveformVisualiserViewModel = waveformVisualiserViewModel;

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
            SelectedNodeText = string.Empty;

            if (selectedNode == null || selectedNode.Hirc == null)
                return;

            var nodeName = selectedNode.DisplayName;
            if (nodeName.Contains("_"))
                nodeName = WpfHelpers.DuplicateUnderscores(nodeName);
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

            _ = LoadWaveformForNodeAsync(selectedNode);
        }

        private async Task LoadWaveformForNodeAsync(HircTreeNode node)
        {
            var source = CreateWemWaveformSource(node);
            if (source == null)
                return;

            await WaveformVisualiserViewModel.LoadFromWemSourceAsync(source, node.DisplayName);
        }

        public void PreloadWaveformsForNodes(IReadOnlyCollection<HircTreeNode> nodes)
        {
            var sources = nodes
                .Select(CreateWemWaveformSource)
                .Where(source => source != null)
                .ToArray();

            if (sources.Length != 0)
                WaveformVisualiserViewModel.PreloadWemWaveforms(sources);
        }

        private WemWaveformSource CreateWemWaveformSource(HircTreeNode node)
        {
            if (node?.Hirc is ICAkSound sound)
            {
                // From at least V136 and newer, AkMediaInformation no longer stores a FileOffset. To get the wem data you would search the DidxChunk
                // for the SourceId. While some Warhammer 3 AkBankSourceData are AKBKSourceType.Data_BNK and therefore should appear in the DidxChunk,
                // no Warhammer 3 wems are in there and instead all wems are stored in Packs so they're actually AKBKSourceType.Streaming.
                // This could be explained by Wwiser's Enum for AKBKSourceType in V136 mapping incorrectly, or V136 not supporting data bnks but who knows?
                // So, as there are no data wems in Warhammer 3, functionality to find wem data in V136 is not implemented as they can only be streamed.
                if (sound.GetStreamType() == AKBKSourceType.Data_BNK && sound is CAkSound_V112 soundV112)
                {
                    var mediaInformation = soundV112.AkBankSourceData.AkMediaInformation;
                    return new WemWaveformSource(
                        $"data-wem:{mediaInformation.FileId}:{mediaInformation.FileOffset}:{mediaInformation.InMemoryMediaSize}",
                        () => _audioRepository.FindDataWem(mediaInformation.FileId, (int)mediaInformation.FileOffset, (int)mediaInformation.InMemoryMediaSize));
                }

                var sourceId = sound.GetSourceId();
                var wemFile = _audioRepository.FindWem(sourceId.ToString());
                if (wemFile == null)
                    return null;

                return new WemWaveformSource($"wem:{sourceId}:{wemFile.Name}", wemFile.DataSource.ReadData);
            }

            if (node?.Hirc is ICAkMusicTrack musicTrack)
            {
                var sourceId = musicTrack.GetChildren().FirstOrDefault();
                var wemFile = _audioRepository.FindWem(sourceId.ToString());
                if (wemFile == null)
                    return null;

                return new WemWaveformSource($"wem:{sourceId}:{wemFile.Name}", wemFile.DataSource.ReadData);
            }

            return null;
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
                var hircTreeChildrenParser = new HircTreeChildrenParser(_audioRepository, lazyLoadChildren: true);

                SelectedNode = null;
                TreeList.Clear();

                var dialogueEvents = _audioRepository.GetHircs(AkBkHircType.Dialogue_Event);
                foreach (var dialogueEvent in dialogueEvents)
                {
                    var dialogueEventRootNode = hircTreeChildrenParser.BuildHierarchy(dialogueEvent);
                    if (FilterTreeByVOActor(dialogueEventRootNode, newValue.DisplayName))
                        TreeList.Add(dialogueEventRootNode);
                }

                return;
            }
            else
            {
                var hircTreeChildrenParser = new HircTreeChildrenParser(_audioRepository, lazyLoadChildren: true);

                SelectedNode = null;
                TreeList.Clear();

                var rootNode = hircTreeChildrenParser.BuildHierarchy(newValue.HircItem);
                rootNode.IsExpanded = true;

                TreeList.Add(rootNode);
            }
        }

        public static void RunDepthFirstSearchToSound(HircTreeNode selectedNode)
        {
            var currentNode = selectedNode;
            var visitedNodes = new System.Collections.Generic.HashSet<HircTreeNode>();

            while (currentNode != null && visitedNodes.Add(currentNode))
            {
                if (currentNode.Hirc?.HircType == AkBkHircType.Sound)
                    return;

                // Expanding resolves this node's pending HIRCs in one breadth-first batch
                currentNode.IsExpanded = true;

                // A branch needs a user choice, only an unambiguous path goes deeper
                if (currentNode.Children == null || currentNode.Children.Count != 1)
                    return;

                currentNode = currentNode.Children[0];
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
