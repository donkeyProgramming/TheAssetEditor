using CommonControls.Common;
using CommonControls.Services;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using MediatR;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Input;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels
{
    public class SceneExplorerViewModel : NotifyPropertyChangedImpl,
        INotificationHandler<SelectionChangedEvent>
    {
        IComponentManager _componentManager;
        SceneManager _sceneManager;
        CommandExecutor _commandExecutor;
        SelectionManager _selectionManager;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        PackFileService _packFileService;
        ApplicationSettingsService _applicationSettingsService;
        AnimationControllerViewModel _animationControllerViewModel;

        public ObservableCollection<ISceneNode> _sceneGraphRootNodes = new ObservableCollection<ISceneNode>();
        public ObservableCollection<ISceneNode> SceneGraphRootNodes { get { return _sceneGraphRootNodes; } set { SetAndNotify(ref _sceneGraphRootNodes, value); } }

        ObservableCollection<ISceneNode> _SelectedObjects = new ObservableCollection<ISceneNode>();
        public ObservableCollection<ISceneNode> SelectedObjects { get { return _SelectedObjects; } set { SetAndNotify(ref _SelectedObjects, value); } }

        ISceneNodeViewModel _selectedNodeViewModel;
        public ISceneNodeViewModel SelectedNodeViewModel { get { return _selectedNodeViewModel; } set { SetAndNotify(ref _selectedNodeViewModel, value); } }

        public SceneExplorerContextMenuHandler ContextMenu { get; set; }

        bool _ignoreSelectionChanges = false;
        public SceneExplorerViewModel(ComponentManagerResolver componentManagerResolver, PackFileService packFileService, AnimationControllerViewModel animationControllerViewModel,
            ApplicationSettingsService applicationSettingsService, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, CommandExecutor commandExecutor, SelectionManager selectionManager, SceneManager sceneManager)
        {
            _componentManager = componentManagerResolver.ComponentManager;
            _animationControllerViewModel = animationControllerViewModel;

            _packFileService = packFileService;
            _applicationSettingsService = applicationSettingsService;

            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _sceneManager = sceneManager;
            _commandExecutor = commandExecutor;
            _selectionManager = selectionManager;


            SceneGraphRootNodes.Add(_sceneManager.RootNode);

            _sceneManager.SceneObjectAdded += (a, b) => RebuildTree();
            _sceneManager.SceneObjectRemoved += (a, b) => RebuildTree();

            ContextMenu = new SceneExplorerContextMenuHandler(_commandExecutor, _sceneManager);
            ContextMenu.SelectedNodesChanged += OnSelectedNodesChanged; // ToDo - MediatR

            SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
        }

        private void OnSelectedNodesChanged(IEnumerable<ISceneNode> selectedNodes)
        {
            foreach (var node in SelectedObjects.ToList())
            {
                SelectedObjects.Remove(node);
            }
            foreach (var node in selectedNodes)
            {
                SelectedObjects.Add(node);
            }
        }

        private void SelectedObjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                SelectedObjects.CollectionChanged -= SelectedObjects_CollectionChanged;
                _ignoreSelectionChanges = true;

                try
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        var newItem = e.NewItems[0] as ISceneNode;
                        var newItemIndex = newItem.Parent.Children.IndexOf(newItem);
                        var selectedWithoutNewItem = SelectedObjects.Except(new List<ISceneNode>(){newItem});

                        if (selectedWithoutNewItem.Any())
                        {
                            var existingSelectionIndex = selectedWithoutNewItem
                                .Select(obj => newItem.Parent.Children.IndexOf(obj))
                                .OrderBy(index => Math.Abs(index-newItemIndex))
                                .First();

                            var isAscending = newItemIndex < existingSelectionIndex;
                            var min = isAscending ? newItemIndex : existingSelectionIndex;
                            var max = isAscending ? existingSelectionIndex : newItemIndex;

                            for (int i=min; i<max; i++)
                            {
                                var element = newItem.Parent.Children.ElementAt(i);
                                SelectedObjects.Add(element);
                            }
                        }
                    }
                } catch {}

                var objectState = new ObjectSelectionState();
                foreach (var item in SelectedObjects)
                {
                    if (item is GroupNode groupNode && groupNode.IsSelectable == true)
                    {
                        var itemsToSelect = groupNode.Children.Where(x => x as ISelectable != null)
                            .Select(x => x as ISelectable)
                            .Where(x => x.IsSelectable != false)
                            .ToList();

                        objectState.ModifySelection(itemsToSelect, false);
                    }
                    else
                    {
                        if (item is ISelectable selectableNode && selectableNode.IsSelectable)
                            objectState.ModifySelectionSingleObject(selectableNode, false);
                    }
                }

                var currentSelection = _selectionManager.GetState() as ObjectSelectionState;
                bool selectionEqual = false;
                if (currentSelection != null)
                    selectionEqual = currentSelection.IsSelectionEqual(objectState);

                if (!selectionEqual)
                    _selectionManager.SetState(objectState);

            }
            finally
            {
                SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
                _ignoreSelectionChanges = false;
            }
            
            UpdateViewModelAndContextMenyBasedOnSelection();
        }

        void UpdateViewModelAndContextMenyBasedOnSelection()
        {
            if (SelectedNodeViewModel != null)
                SelectedNodeViewModel.Dispose();

            if (SelectedObjects.Count == 1)
            {
                SelectedNodeViewModel = SceneNodeViewFactory.Create(SelectedObjects.First(), _skeletonAnimationLookUpHelper, _packFileService, _animationControllerViewModel, _componentManager, _applicationSettingsService);
                ContextMenu.Create(SelectedObjects);
            }
            else
            {
                SelectedNodeViewModel = null;
                ContextMenu.Create(SelectedObjects);
            }
        }

        private void SelectionChanged(ISelectionState state)
        {
            try
            {
                SelectedObjects.CollectionChanged -= SelectedObjects_CollectionChanged;

                if (state is ObjectSelectionState objectSelection)
                {
                    if (SelectedObjects.Count != 0)
                    {
                        while (SelectedObjects.Count > 0)
                            SelectedObjects.RemoveAt(SelectedObjects.Count - 1);
                    }
                    var objects = objectSelection.SelectedObjects();
                    foreach (var obj in objects)
                        SelectedObjects.Add(obj);
                }
            }
            finally
            {
                SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
            }

            UpdateViewModelAndContextMenyBasedOnSelection();
        }

        private void RebuildTree()
        {
            //var collection = new ObservableCollection<ISceneNode>(); ;
            //collection.Add(_sceneManager.RootNode);

            SceneGraphRootNodes.Clear();
            SceneGraphRootNodes.Add(_sceneManager.RootNode);
            //SceneGraphRootNodes = collection;
            UpdateLod(0);
        }

        void UpdateLod(int newLodLevel)
        {
            var allModelNodes = _sceneManager.GetEnumeratorConditional(x => x is Rmv2ModelNode);
            foreach (var item in allModelNodes)
            {
                for (int i = 0; i < item.Children.Count(); i++)
                {
                    item.Children[i].IsVisible = i == newLodLevel;
                    item.Children[i].IsExpanded = i == newLodLevel;
                }
            }
        }

        public Task Handle(SelectionChangedEvent notification, CancellationToken cancellationToken)
        {
            if(_ignoreSelectionChanges == false)
                SelectionChanged(notification.NewState);
            return Task.CompletedTask;
        }
    }
}
