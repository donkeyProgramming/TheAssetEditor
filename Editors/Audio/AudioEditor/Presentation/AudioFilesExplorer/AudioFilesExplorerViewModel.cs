using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Commands;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.AudioEditor.Presentation.Shared.Table;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer
{
    public partial class AudioFilesExplorerViewModel : ObservableObject
    {
        private readonly IGlobalEventHub _globalEventHub;
        private readonly IEventHub _eventHub;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IPackFileService _packFileService;
        private readonly IAudioEditorStateService _audioEditorStateService;
        private readonly IAudioFilesTreeBuilderService _audioFilesTreeBuilder;
        private readonly IAudioFilesTreeSearchFilterService _audioFilesTreeFilter;

        [ObservableProperty] private string _audioFilesExplorerLabel;
        [ObservableProperty] private bool _isSetAudioFilesButtonEnabled = false;
        [ObservableProperty] private bool _isAddAudioFilesButtonEnabled = false;
        [ObservableProperty] private bool _isPlayAudioButtonEnabled = false;
        [ObservableProperty] private string _filterQuery;
        [ObservableProperty] private ObservableCollection<AudioFilesTreeNode> _audioFilesTree;
        public ObservableCollection<AudioFilesTreeNode> SelectedTreeNodes { get; set; } = [];
        private CancellationTokenSource _filterQueryDebounceCancellationTokenSource;

        public AudioFilesExplorerViewModel(
            IGlobalEventHub globalEventHub,
            IEventHub eventHub,
            IUiCommandFactory uiCommandFactory,
            IPackFileService packFileService,
            IAudioEditorStateService audioEditorStateService,
            IAudioFilesTreeBuilderService audioFilesTreeBuilder,
            IAudioFilesTreeSearchFilterService audioFilesTreeFilter)
        {
            _globalEventHub = globalEventHub;
            _eventHub = eventHub;
            _uiCommandFactory = uiCommandFactory;
            _packFileService = packFileService;
            _audioEditorStateService = audioEditorStateService;
            _audioFilesTreeBuilder = audioFilesTreeBuilder;
            _audioFilesTreeFilter = audioFilesTreeFilter;

            SelectedTreeNodes.CollectionChanged += OnSelectedTreeNodesChanged;

            _eventHub.Register<AudioProjectExplorerNodeSelectedEvent>(this, OnAudioProjectExplorerNodeSelected);
            _eventHub.Register<AudioFilesChangedEvent>(this, OnAudioFilesChanged);
            _globalEventHub.Register<PackFileContainerSetAsMainEditableEvent>(this, x => OnPackFileContainerSetAsMainEditable(x.Container));
            _globalEventHub.Register<PackFileContainerFilesAddedEvent>(this, x => RefreshAudioFilesTree(x.Container));
            _globalEventHub.Register<PackFileContainerFilesRemovedEvent>(this, x => RefreshAudioFilesTree(x.Container));
            _globalEventHub.Register<PackFileContainerFilesUpdatedEvent>(this, x => RefreshAudioFilesTree(x.Container));
            _globalEventHub.Register<PackFileContainerFolderRemovedEvent>(this, x => RefreshAudioFilesTree(x.Container));

            var editablePack = _packFileService.GetEditablePack();
            if (editablePack == null)
                return;

            AudioFilesExplorerLabel = $"Audio Files Explorer - {TableHelpers.DuplicateUnderscores(editablePack.Name)}";

            AudioFilesTree = _audioFilesTreeBuilder.BuildTree(editablePack);
            SetupIsExpandedHandlers(AudioFilesTree);

            CacheRootWavFilesInWaveformVisualiser();
        }

        private void SetupIsExpandedHandlers(ObservableCollection<AudioFilesTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.NodeIsExpandedChanged -= OnNodeIsExpandedChanged;
                node.NodeIsExpandedChanged += OnNodeIsExpandedChanged;

                if (node.Children is { Count: > 0 })
                    SetupIsExpandedHandlers(node.Children);
            }
        }

        private void CacheRootWavFilesInWaveformVisualiser()
        {
            var wavFilePaths = new List<string>();
            foreach (var node in AudioFilesTree)
            {
                if (node.Parent == null && node.Type == AudioFilesTreeNodeType.WavFile)
                    wavFilePaths.Add(node.FilePath);
            }

            if (wavFilePaths.Count > 0)
                _eventHub.Publish(new AddToWaveformCacheRequestedEvent(wavFilePaths));
        }

        private void OnNodeIsExpandedChanged(object sender, bool isExpanded)
        {
            var node = sender as AudioFilesTreeNode;
            if (node.Children != null)
            {
                var wavFilePaths = new List<string>();
                foreach (var child in node.Children)
                {
                    if (child.Type == AudioFilesTreeNodeType.WavFile)
                        wavFilePaths.Add(child.FilePath);
                }

                if (wavFilePaths.Count == 0)
                    return;

                if (isExpanded)
                    _eventHub.Publish(new AddToWaveformCacheRequestedEvent(wavFilePaths));
                else
                    _eventHub.Publish(new RemoveFromWaveformCacheRequestedEvent(wavFilePaths));
            }
        }

        private void OnAudioProjectExplorerNodeSelected(AudioProjectExplorerNodeSelectedEvent e)
        {
            IsSetAudioFilesButtonEnabled = false;
            IsAddAudioFilesButtonEnabled = false;
        }

        private void OnAudioFilesChanged(AudioFilesChangedEvent e) => SetButtonEnablement();

        private void OnPackFileContainerSetAsMainEditable(PackFileContainer packFileContainer)
        {
            if (packFileContainer == null)
                return;

            AudioFilesExplorerLabel = $"Audio Files Explorer - {TableHelpers.DuplicateUnderscores(packFileContainer.Name)}";
            RefreshAudioFilesTree(packFileContainer);
        }

        private void RefreshAudioFilesTree(PackFileContainer packFileContainer)
        {
            AudioFilesTree = _audioFilesTreeBuilder.BuildTree(packFileContainer);
            CacheRootWavFilesInWaveformVisualiser();
        }

        private void OnSelectedTreeNodesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetSelectedTreeNodes(e);

            if (SelectedTreeNodes.Count == 1)
            {
                var selectedNode = e.NewItems[0] as AudioFilesTreeNode;
                if (selectedNode.Type == AudioFilesTreeNodeType.WavFile)
                {
                    var selectedAudioFile = SelectedTreeNodes[0];
                    _eventHub.Publish(new DisplayWaveformVisualiserRequestedEvent(selectedAudioFile.FilePath));
                }
            }

            SetButtonEnablement();
        }

        private void SetSelectedTreeNodes(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (AudioFilesTreeNode addedNode in e.NewItems)
                {
                    if (addedNode.Type != AudioFilesTreeNodeType.WavFile)
                        SelectedTreeNodes.Remove(addedNode);
                }
            }
        }

        private void SetButtonEnablement()
        {
            IsPlayAudioButtonEnabled = SelectedTreeNodes.Count == 1;

            var selectedAudioProjectExplorerNode = _audioEditorStateService.SelectedAudioProjectExplorerNode;
            if (selectedAudioProjectExplorerNode == null)
                return;

            if (SelectedTreeNodes.Count > 0)
            {
                if (selectedAudioProjectExplorerNode.Type == AudioProjectTreeNodeType.ActionEventType
                    || selectedAudioProjectExplorerNode.Type == AudioProjectTreeNodeType.DialogueEvent)
                {
                    IsSetAudioFilesButtonEnabled = true;

                    if (_audioEditorStateService.AudioFiles.Count > 0)
                        IsAddAudioFilesButtonEnabled = true;
                    else
                        IsAddAudioFilesButtonEnabled = false;
                }
            }
            else
            {
                IsSetAudioFilesButtonEnabled = false;
                IsAddAudioFilesButtonEnabled = false;
            }
        }

        partial void OnFilterQueryChanged(string value) => DebounceFilterAudioFilesTreeForFilterQuery();

        private void DebounceFilterAudioFilesTreeForFilterQuery()
        {
            _filterQueryDebounceCancellationTokenSource?.Cancel();
            _filterQueryDebounceCancellationTokenSource?.Dispose();

            _filterQueryDebounceCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _filterQueryDebounceCancellationTokenSource.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, cancellationToken);

                    var application = Application.Current;
                    if (application is not null && application.Dispatcher is not null)
                        application.Dispatcher.Invoke(() => _audioFilesTreeFilter.FilterTree(AudioFilesTree, FilterQuery));
                    else
                        _audioFilesTreeFilter.FilterTree(AudioFilesTree, FilterQuery);
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);
        }

        [RelayCommand] public void CollapseOrExpandTree()
        {
            if (AudioFilesTree == null || AudioFilesTree.Count == 0)
                return;

            var isVisibleAndExpanded = AudioFilesTree.Any(node => node.IsVisible && node.IsExpanded);
            foreach (var rootNode in AudioFilesTree)
                ToggleNodeExpansion(rootNode, !isVisibleAndExpanded);
        }

        private static void ToggleNodeExpansion(AudioFilesTreeNode node, bool shouldExpand)
        {
            if (node.IsVisible)
                node.IsExpanded = shouldExpand;

            foreach (var child in node.Children)
                ToggleNodeExpansion(child, shouldExpand);
        }

        [RelayCommand] public void SetAudioFiles() => _uiCommandFactory.Create<SetAudioFilesCommand>().Execute(SelectedTreeNodes, false);

        [RelayCommand] public void AddToAudioFiles() => _uiCommandFactory.Create<SetAudioFilesCommand>().Execute(SelectedTreeNodes, true);

        [RelayCommand] public void PlayWav()
        {
            if (!IsPlayAudioButtonEnabled)
                return;

            var selectedAudioFile = SelectedTreeNodes[0];
            _uiCommandFactory.Create<PlayAudioFileCommand>().Execute(selectedAudioFile.FileName, selectedAudioFile.FilePath);
        }

        [RelayCommand] public void ClearText() => FilterQuery = string.Empty;
    }
}
