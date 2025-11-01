using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer
{
    public partial class AudioProjectExplorerViewModel : ObservableObject
    {
        private readonly IEventHub _eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioProjectTreeBuilderService _audioProjectTreeBuilder;
        private readonly IAudioProjectTreeFilterService _audioProjectTreeFilterService;

        [ObservableProperty] private string _audioProjectExplorerLabel;
        [ObservableProperty] private bool _showEditedItemsOnly;
        [ObservableProperty] private bool _showActionEvents = true;
        [ObservableProperty] private bool _showDialogueEvents = true;
        [ObservableProperty] private bool _isDialogueEventFilterEnabled = false;
        [ObservableProperty] private ObservableCollection<Wh3DialogueEventType> _dialogueEventTypes = [];
        [ObservableProperty] private Wh3DialogueEventType? _selectedDialogueEventType;
        [ObservableProperty] private ObservableCollection<Wh3DialogueEventUnitProfile> _dialogueEventProfiles = [];
        [ObservableProperty] private Wh3DialogueEventUnitProfile? _selectedDialogueEventProfile;
        [ObservableProperty] private string _searchQuery;
        [ObservableProperty] public ObservableCollection<AudioProjectTreeNode> _audioProjectTree = [];
        [ObservableProperty] public AudioProjectTreeNode _selectedNode;
        private CancellationTokenSource _searchQueryDebounceCancellationTokenSource;

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

            _eventHub.Register<AudioProjectLoadedEvent>(this, OnAudioProjectInitialised);
        }

        private void OnAudioProjectInitialised(AudioProjectLoadedEvent e)
        {
            ResetFilters();

            var audioProject = _audioEditorStateService.AudioProject;
            AudioProjectTree = _audioProjectTreeBuilder.BuildTree(audioProject, ShowEditedItemsOnly);

            var audioProjectFileName = Path.GetFileNameWithoutExtension(_audioEditorStateService.AudioProjectFileName);
            AudioProjectExplorerLabel = $"Audio Project Explorer - {TableHelpers.DuplicateUnderscores(audioProjectFileName)}";
        }

        partial void OnSelectedNodeChanged(AudioProjectTreeNode value)
        {
            _audioEditorStateService.StoreSelectedAudioProjectExplorerNode(SelectedNode);

            _eventHub.Publish(new AudioProjectExplorerNodeSelectedEvent(SelectedNode));

            IsDialogueEventFilterEnabled = false;

            if (SelectedNode.IsDialogueEvents())
                InitialiseDialogueEventFilters();
        }

        private void FilterAudioProjectTree()
        {
            if (AudioProjectTree is null)
                return;

            var filterSettings = new AudioProjectTreeFilterSettings(SearchQuery, ShowEditedItemsOnly, ShowActionEvents, ShowDialogueEvents);
            _audioProjectTreeFilterService.FilterTree(AudioProjectTree, filterSettings);
        }

        partial void OnSelectedDialogueEventTypeChanged(Wh3DialogueEventType? value)
        {
            SelectedNode.DialogueEventTypeFilter = value ?? Wh3DialogueEventType.TypeShowAll;
            SetDialogueEventFilterDisplayText();
            FilterAudioProjectTree();
        }

        partial void OnSelectedDialogueEventProfileChanged(Wh3DialogueEventUnitProfile? value)
        {
            SelectedNode.DialogueEventProfileFilter = value ?? Wh3DialogueEventUnitProfile.ProfileShowAll;
            SetDialogueEventFilterDisplayText();
            FilterAudioProjectTree();
        }

        private void SetDialogueEventFilterDisplayText()
        {
            var setTypeFilterText = SelectedDialogueEventType != null && SelectedDialogueEventType != Wh3DialogueEventType.TypeShowAll;
            var setProfileFilterText = SelectedDialogueEventProfile != null && SelectedDialogueEventProfile != Wh3DialogueEventUnitProfile.ProfileShowAll;
            if (setTypeFilterText && setProfileFilterText)
            {
                var typeFilterText = $"Type: {Wh3DialogueEventInformation.GetDialogueEventTypeDisplayName(SelectedDialogueEventType)}";
                var profileFilterText = $"Profile: {Wh3DialogueEventInformation.GetDialogueEventProfileDisplayName(SelectedDialogueEventProfile)}";
                SelectedNode.DialogueEventFilterDisplayText = $"({typeFilterText}, {profileFilterText})";
            }
            else if (setTypeFilterText)
            {
                var typeFilterText = $"Type: {Wh3DialogueEventInformation.GetDialogueEventTypeDisplayName(SelectedDialogueEventType)}";
                SelectedNode.DialogueEventFilterDisplayText = $"({typeFilterText})";
            }
            else if (setProfileFilterText)
            {
                var profileFilterText = $"Profile: {Wh3DialogueEventInformation.GetDialogueEventProfileDisplayName(SelectedDialogueEventProfile)}";
                SelectedNode.DialogueEventFilterDisplayText = $"({profileFilterText})";
            }
            else
                SelectedNode.DialogueEventFilterDisplayText = null;
        }

        partial void OnSearchQueryChanged(string value) => DebounceFilterAudioProjectTreeForSearchQuery();

        private void DebounceFilterAudioProjectTreeForSearchQuery()
        {
            _searchQueryDebounceCancellationTokenSource?.Cancel();
            _searchQueryDebounceCancellationTokenSource?.Dispose();

            _searchQueryDebounceCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _searchQueryDebounceCancellationTokenSource.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, cancellationToken);

                    var application = Application.Current;
                    if (application is not null && application.Dispatcher is not null)
                        application.Dispatcher.Invoke(FilterAudioProjectTree);
                    else
                        FilterAudioProjectTree();
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);
        }

        partial void OnShowEditedItemsOnlyChanged(bool value) => FilterAudioProjectTree();

        partial void OnShowActionEventsChanged(bool value) => FilterAudioProjectTree();

        partial void OnShowDialogueEventsChanged(bool value) => FilterAudioProjectTree();

        [RelayCommand] public void CollapseOrExpandTree()
        {
            var isVisibleAndExpanded = AudioProjectTree.Any(node => node.IsVisible && node.IsExpanded);
            foreach (var rootNode in AudioProjectTree)
                ToggleNodeExpansion(rootNode, !isVisibleAndExpanded);
        }

        private static void ToggleNodeExpansion(AudioProjectTreeNode node, bool shouldExpand)
        {
            if (node.IsVisible)
                node.IsExpanded = shouldExpand;

            foreach (var child in node.Children)
                ToggleNodeExpansion(child, shouldExpand);
        }

        private void InitialiseDialogueEventFilters()
        {
            var soundBankName = Wh3SoundBankInformation.GetName(SelectedNode.GameSoundBank);
            var soundBank = Wh3SoundBankInformation.GetSoundBank(soundBankName);

            DialogueEventTypes = new ObservableCollection<Wh3DialogueEventType>(Wh3DialogueEventInformation.Information
                .Where(dialogueEventDefinition => dialogueEventDefinition.SoundBank == soundBank)
                .SelectMany(dialogueEventDefinition => dialogueEventDefinition.DialogueEventTypes)
                .Distinct()
                .OrderBy(type => (int)type)); //Order by enum order

            DialogueEventProfiles = new ObservableCollection<Wh3DialogueEventUnitProfile>(Wh3DialogueEventInformation.Information
                .Where(dialogueEventDefinition => dialogueEventDefinition.SoundBank == soundBank)
                .SelectMany(dialogueEventDefinition => dialogueEventDefinition.UnitProfiles)
                .Distinct()
                .OrderBy(profile => (int)profile)); //Order by enum order

            if (SelectedNode.DialogueEventTypeFilter != null && SelectedNode.DialogueEventTypeFilter != Wh3DialogueEventType.TypeShowAll)
                SelectedDialogueEventType = SelectedNode.DialogueEventTypeFilter;
            else
                ResetDialogueEventTypeFilterComboBoxSelectedItem();

            if (SelectedNode.DialogueEventProfileFilter != null && SelectedNode.DialogueEventProfileFilter != Wh3DialogueEventUnitProfile.ProfileShowAll)
                SelectedDialogueEventProfile = SelectedNode.DialogueEventProfileFilter;
            else
                ResetDialogueEventProfileFilterComboBoxSelectedItem();

            IsDialogueEventFilterEnabled = true;
        }

        [RelayCommand] public void ResetFilters()
        {
            ShowEditedItemsOnly = false;
            ShowActionEvents = true;
            ShowDialogueEvents = true;
            ResetSearchQuery();

            var dialogueEventNodes = FlattenAudioProjectTree(AudioProjectTree)
                .Where(node => node.Name == AudioProjectTreeBuilderService.DialogueEventsNodeName)
                .ToList();
            foreach (var dialogueEventNode in dialogueEventNodes)
            {
                dialogueEventNode.DialogueEventTypeFilter = Wh3DialogueEventType.TypeShowAll;
                dialogueEventNode.DialogueEventProfileFilter = Wh3DialogueEventUnitProfile.ProfileShowAll;
                dialogueEventNode.DialogueEventFilterDisplayText = null;
            }
        }

        private static IEnumerable<AudioProjectTreeNode> FlattenAudioProjectTree(IEnumerable<AudioProjectTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;

                foreach (var child in FlattenAudioProjectTree(node.Children))
                    yield return child;
            }
        }

        [RelayCommand] public void ResetSearchQuery() => SearchQuery = "";

        public void ResetDialogueEventTypeFilterComboBoxSelectedItem() => SelectedDialogueEventType = null;

        public void ResetDialogueEventProfileFilterComboBoxSelectedItem() => SelectedDialogueEventProfile = null;
    }
}
