using CommonControls.Common;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using Monogame.WpfInterop.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using View3D.Commands;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels
{
    // Improve using this: https://stackoverflow.com/questions/63110566/multi-select-with-multiple-level-in-wpf-treeview

    public class SceneExplorerViewModel : NotifyPropertyChangedImpl
    {
        SceneManager _sceneManager;
        SelectionManager _selectionManager;
        private readonly SceneNodeViewFactory _sceneNodeViewFactory;
        bool _ignoreSelectionChanges = false;

        public ObservableCollection<ISceneNode> SceneGraphRootNodes { get; private set; } = new ObservableCollection<ISceneNode>();
        public ObservableCollection<ISceneNode> SelectedObjects { get; private set; } = new ObservableCollection<ISceneNode>();
        public SceneExplorerContextMenuHandler ContextMenu { get; private set; }

        ISceneNodeViewModel _selectedNodeViewModel;
        public ISceneNodeViewModel SelectedNodeViewModel { get { return _selectedNodeViewModel; } set { SetAndNotify(ref _selectedNodeViewModel, value); } }

        public SceneExplorerViewModel(SceneNodeViewFactory sceneNodeViewFactory, CommandExecutor commandExecutor, SelectionManager selectionManager, SceneManager sceneManager, CommandFactory commandFactory, EventHub eventHub)
        {
            _sceneNodeViewFactory = sceneNodeViewFactory;
            _sceneManager = sceneManager;
            _selectionManager = selectionManager;

            SceneGraphRootNodes.Add(_sceneManager.RootNode);

            ContextMenu = new SceneExplorerContextMenuHandler(commandExecutor, _sceneManager, commandFactory);
            ContextMenu.SelectedNodesChanged += OnContextMenuActionChangingSelection;

            SelectedObjects.CollectionChanged += OnSceneExplorerSelectionChanged;
            
            eventHub.Register<SelectionChangedEvent>(OnSceneSelectionChanged);
            eventHub.Register<SceneObjectAddedEvent>(x=> RebuildTree());
            eventHub.Register<SceneObjectRemovedEvent>(x => RebuildTree());
        }

        private void OnContextMenuActionChangingSelection(IEnumerable<ISceneNode> selectedNodes)
        {
            // Clear works in a strange way on multiselect observable lists. Loop over and remove one by one
            foreach (var node in SelectedObjects.ToList())
                SelectedObjects.Remove(node);

            foreach (var node in selectedNodes)
                SelectedObjects.Add(node);
        }

        private void OnSceneExplorerSelectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                SelectedObjects.CollectionChanged -= OnSceneExplorerSelectionChanged;
                _ignoreSelectionChanges = true;

                try
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        // This is to handle selecting items inbetween others while holding LeftShift
                        var newItem = e.NewItems[0] as ISceneNode;
                        var newItemIndex = newItem.Parent.Children.IndexOf(newItem);
                        var selectedWithoutNewItem = SelectedObjects.Except(new List<ISceneNode>() { newItem });

                        if (selectedWithoutNewItem.Any())
                        {
                            var existingSelectionIndex = selectedWithoutNewItem
                                .Select(obj => newItem.Parent.Children.IndexOf(obj))
                                .OrderBy(index => Math.Abs(index - newItemIndex))
                                .First();

                            var isAscending = newItemIndex < existingSelectionIndex;
                            var min = isAscending ? newItemIndex : existingSelectionIndex;
                            var max = isAscending ? existingSelectionIndex : newItemIndex;

                            for (int i = min; i < max; i++)
                            {
                                var element = newItem.Parent.Children.ElementAt(i);
                                if(SelectedObjects.Contains(element) == false)
                                    SelectedObjects.Add(element);
                            }
                        }
                    }
                }
                catch { }

                // Select the objects in the game world
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
                SelectedObjects.CollectionChanged += OnSceneExplorerSelectionChanged;
                _ignoreSelectionChanges = false;
            }

            UpdateViewModelAndContextMenyBasedOnSelection();
        }

        void UpdateViewModelAndContextMenyBasedOnSelection()
        {
            if (SelectedNodeViewModel != null)
                SelectedNodeViewModel.Dispose();

            if (SelectedObjects.Count == 1)
                SelectedNodeViewModel = _sceneNodeViewFactory.CreateEditorView(SelectedObjects.First());
            else
                SelectedNodeViewModel = null;

            ContextMenu.Create(SelectedObjects);
        }

        private void RebuildTree()
        {
            SceneGraphRootNodes.Clear();
            SceneGraphRootNodes.Add(_sceneManager.RootNode);

            // Update visibility
            var newLodLevel = 0;
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

        void OnSceneSelectionChanged(SelectionChangedEvent notification)
        {
            if (_ignoreSelectionChanges)
                return;

            if (notification.NewState is not ObjectSelectionState objectSelection)
                return;

            try
            {
                SelectedObjects.CollectionChanged -= OnSceneExplorerSelectionChanged;

                if (SelectedObjects.Count != 0)
                {
                    while (SelectedObjects.Count > 0)
                        SelectedObjects.RemoveAt(SelectedObjects.Count - 1);
                }
                var objects = objectSelection.SelectedObjects();
                foreach (var obj in objects)
                    SelectedObjects.Add(obj);
                
            }
            finally
            {
                SelectedObjects.CollectionChanged += OnSceneExplorerSelectionChanged;
            }

            UpdateViewModelAndContextMenyBasedOnSelection();
        }
    }
}
