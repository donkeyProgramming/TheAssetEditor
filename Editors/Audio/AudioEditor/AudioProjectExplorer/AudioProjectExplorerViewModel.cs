using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Table;
using Editors.Audio.GameSettings.Warhammer3;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;

namespace Editors.Audio.AudioEditor.AudioProjectExplorer
{
    public partial class AudioProjectExplorerViewModel : ObservableObject
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioProjectTreeBuilderService _audioProjectTreeBuilder;
        private readonly IAudioProjectTreeFilterService _audioProjectTreeFilterService;

        private readonly ILogger _logger = Logging.Create<AudioProjectExplorerViewModel>();

        [ObservableProperty] private string _audioProjectExplorerLabel;
        [ObservableProperty] private bool _showEditedAudioProjectItemsOnly;
        [ObservableProperty] private bool _isDialogueEventPresetFilterEnabled = false;
        [ObservableProperty] private DialogueEventPreset? _selectedDialogueEventPreset;
        [ObservableProperty] private ObservableCollection<DialogueEventPreset> _dialogueEventPresets = [];
        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] public ObservableCollection<AudioProjectTreeNode> _audioProjectTree = [];
        [ObservableProperty] public AudioProjectTreeNode _selectedNode;

        public AudioProjectExplorerViewModel(
            IEventHub eventHub,
            IAudioEditorStateService audioEditorStateService,
            IAudioProjectTreeBuilderService audioProjectTreeBuilder,
            IAudioProjectTreeFilterService audioProjectTreeFilterService)
        {
            _eventHub = eventHub;
            _audioEditorStateService = audioEditorStateService;
            _audioProjectTreeBuilder = audioProjectTreeBuilder;
            _audioProjectTreeFilterService = audioProjectTreeFilterService;

            AudioProjectExplorerLabel = $"Audio Project Explorer";

            _eventHub.Register<AudioProjectInitialisedEvent>(this, OnAudioProjectInitialised);
        }

        private void OnAudioProjectInitialised(AudioProjectInitialisedEvent e)
        {
            var audioProject = _audioEditorStateService.AudioProject;
            AudioProjectTree = _audioProjectTreeBuilder.BuildTree(audioProject, ShowEditedAudioProjectItemsOnly);

            var audioProjectFileName = _audioEditorStateService.AudioProjectFileName.Replace(".aproj", string.Empty);
            AudioProjectExplorerLabel = $"Audio Project Explorer - {TableHelpers.DuplicateUnderscores(audioProjectFileName)}";
        }

        partial void OnSelectedNodeChanged(AudioProjectTreeNode value)
        {
            _audioEditorStateService.SelectedAudioProjectExplorerNode = SelectedNode;

            _eventHub.Publish(new AudioProjectExplorerNodeSelectedEvent(SelectedNode));

            IsDialogueEventPresetFilterEnabled = false;

            if (SelectedNode.IsDialogueEventSoundBank())
            {
                InitialiseDialogueEventPresetFilter();
                _logger.Here().Information($"Loaded Dialogue Event SoundBank: {SelectedNode.Name}");
            }
        }

        private void FilterAudioProjectTree()
        {
            if (AudioProjectTree is null)
                return;

            var filterSettings = new AudioProjectTreeFilterSettings(
                SearchQuery,
                ShowEditedAudioProjectItemsOnly,
                _audioEditorStateService.AudioProject);

            _audioProjectTreeFilterService.FilterTree(AudioProjectTree, filterSettings);
        }

        partial void OnSelectedDialogueEventPresetChanged(DialogueEventPreset? value)
        {
            if (SelectedNode?.IsDialogueEventSoundBank() != true)
                return;

            SelectedNode.PresetFilter = value ?? DialogueEventPreset.ShowAll;

            // Set the filtered by text
            SelectedNode.PresetFilterDisplayText = value.HasValue && value.Value != DialogueEventPreset.ShowAll
                ? $" (Filtered by {GetDialogueEventPresetDisplayString(value.Value)} preset)"
                : null;

            FilterAudioProjectTree();
        }

        partial void OnSearchQueryChanged(string value) => FilterAudioProjectTree();

        partial void OnShowEditedAudioProjectItemsOnlyChanged(bool value) => FilterAudioProjectTree();

        [RelayCommand] public void CollapseOrExpandAudioProjectTree() => CollapseAndExpandNodes();

        private void CollapseAndExpandNodes()
        {
            foreach (var node in AudioProjectTree)
            {
                node.IsNodeExpanded = !node.IsNodeExpanded;
                CollapseAndExpandNodesInner(node);
            }
        }

        private static void CollapseAndExpandNodesInner(AudioProjectTreeNode parentNode)
        {
            foreach (var node in parentNode.Children)
            {
                node.IsNodeExpanded = !node.IsNodeExpanded;
                CollapseAndExpandNodesInner(node);
            }
        }

        [RelayCommand] public void ClearFilterText() => SearchQuery = "";

        private void InitialiseDialogueEventPresetFilter()
        {
            var soundBankSubtype = SoundBanks.GetSoundBankSubtype(SelectedNode.Name);

            DialogueEventPresets = new ObservableCollection<DialogueEventPreset>(DialogueEventData
                .Where(dialogueEvent => dialogueEvent.SoundBank == soundBankSubtype)
                .SelectMany(dialogueEvent => dialogueEvent.DialogueEventPreset)
                .Distinct());

            SelectedDialogueEventPreset = SelectedNode.PresetFilter != DialogueEventPreset.ShowAll
                    ? SelectedNode.PresetFilter
                    : null;

            IsDialogueEventPresetFilterEnabled = true;
        }

        public void ResetDialogueEventFilterComboBoxSelectedItem() => SelectedDialogueEventPreset = null;
    }
}
