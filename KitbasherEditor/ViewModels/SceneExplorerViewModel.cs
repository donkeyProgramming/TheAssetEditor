using Common;
using CommonControls.Services;
using GalaSoft.MvvmLight.Command;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Scene;
using View3D.SceneNodes;
using View3D.Utility;

namespace KitbasherEditor.ViewModels
{
    public class SceneExplorerViewModel : NotifyPropertyChangedImpl, IEditableMeshResolver
    {
        public ObservableCollection<ISceneNode> _sceneGraphRootNodes = new ObservableCollection<ISceneNode>();
        public ObservableCollection<ISceneNode> SceneGraphRootNodes { get { return _sceneGraphRootNodes; } set { SetAndNotify(ref _sceneGraphRootNodes, value); } }



        ObservableCollection<ISceneNode> _SelectedObjects = new ObservableCollection<ISceneNode>();
        public ObservableCollection<ISceneNode> SelectedObjects { get { return _SelectedObjects; } set { SetAndNotify(ref _SelectedObjects, value); } }

       //ISceneNode _selectedNode;
       //public ISceneNode SelectedNode { get { return _selectedNode; } set { SetAndNotify(ref _selectedNode, value); OnNodeSelected(_selectedNode); } }

        ISceneNodeViewModel _selectedNodeViewModel;
        public ISceneNodeViewModel SelectedNodeViewModel { get { return _selectedNodeViewModel; } set { SetAndNotify(ref _selectedNodeViewModel, value); } }

        LodItem _selectedLodLvl;
        public LodItem SelectedLodLevel { get { return _selectedLodLvl; } set { SetAndNotify(ref _selectedLodLvl, value); UpdateLod(_selectedLodLvl.Value); } }

        SceneContainer _sceneContainer;
        SceneManager _sceneManager;
        CommandExecutor _commandExecutor;
        SelectionManager _selectionManager;

        public SceneExplorerContextMenuHandler ContextMenu { get; set; }

        MainEditableNode _editableMeshNode;
        public MainEditableNode EditableMeshNode { get => _editableMeshNode; set { _editableMeshNode = value; ContextMenu.EditableMeshNode = value; } }

        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        PackFileService _packFileService;
        AnimationControllerViewModel _animationControllerViewModel;
        public SceneExplorerViewModel(SceneContainer sceneContainer, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, PackFileService packFileService, AnimationControllerViewModel animationControllerViewModel)
        {
            _selectedLodLvl = LodItem.GetAll.First();

            _sceneContainer = sceneContainer;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _packFileService = packFileService;
            _animationControllerViewModel = animationControllerViewModel;
            _sceneManager = _sceneContainer.GetComponent<SceneManager>();
            _commandExecutor = sceneContainer.GetComponent<CommandExecutor>();
            _selectionManager = sceneContainer.GetComponent<SelectionManager>();
            _selectionManager.SelectionChanged += SelectionChanged;

            SceneGraphRootNodes.Add(_sceneManager.RootNode);

            _sceneManager.SceneObjectAdded += (a, b) => RebuildTree();
            _sceneManager.SceneObjectRemoved += (a, b) => RebuildTree();

            ContextMenu = new SceneExplorerContextMenuHandler(_commandExecutor);

            SelectedObjects.CollectionChanged += SelectedObjects_CollectionChanged;
        }

        private void SelectedObjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                SelectedObjects.CollectionChanged -= SelectedObjects_CollectionChanged;
                _selectionManager.SelectionChanged -= SelectionChanged;

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
                _selectionManager.SelectionChanged += SelectionChanged;
            }
            
            UpdateViewModelAndContextMenyBasedOnSelection();
        }

        void UpdateViewModelAndContextMenyBasedOnSelection()
        {
            if (SelectedNodeViewModel != null)
                SelectedNodeViewModel.Dispose();

            if (SelectedObjects.Count == 1)
            {
                SelectedNodeViewModel = SceneNodeViewFactory.Create(SelectedObjects.First(), _skeletonAnimationLookUpHelper, _packFileService, _animationControllerViewModel, _sceneContainer);
                ContextMenu.Create(SelectedObjects.First());
            }
            else
            {
                SelectedNodeViewModel = null;
                ContextMenu.Create(null);
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
            UpdateLod(SelectedLodLevel.Value);
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

        public void Initialize()
        {
        }

        public MainEditableNode GeEditableMeshRootNode()
        {
            return EditableMeshNode;
        }
    }

    public class LodItem
    { 
        public string Name { get; set; }
        public int Value { get; set; }


        static List<LodItem> _items;
        public static List<LodItem> GetAll
        {
            get
            {
                if (_items == null)
                {
                    _items = new List<LodItem>()
                    {
                        new LodItem(){ Name = "Lod 0 - Highest", Value = 0},
                        new LodItem(){ Name = "Lod 1", Value = 1},
                        new LodItem(){ Name = "Lod 2", Value = 2},
                        new LodItem(){ Name = "Lod 3 - Lowest", Value = 3},
                    };
                }
                return _items;
            }
        }
    }
}
