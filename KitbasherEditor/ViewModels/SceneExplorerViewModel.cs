using CommonControls.Common;
using CommonControls.Services;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using MonoGame.Framework.WpfInterop;
using System.Collections.ObjectModel;
using System.Linq;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels
{
    public class SceneExplorerViewModel : NotifyPropertyChangedImpl, IEditableMeshResolver
    {
        IComponentManager _componentManager;
        SceneManager _sceneManager;
        CommandExecutor _commandExecutor;
        SelectionManager _selectionManager;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        PackFileService _packFileService;
        AnimationControllerViewModel _animationControllerViewModel;

        public ObservableCollection<ISceneNode> _sceneGraphRootNodes = new ObservableCollection<ISceneNode>();
        public ObservableCollection<ISceneNode> SceneGraphRootNodes { get { return _sceneGraphRootNodes; } set { SetAndNotify(ref _sceneGraphRootNodes, value); } }

        ObservableCollection<ISceneNode> _SelectedObjects = new ObservableCollection<ISceneNode>();
        public ObservableCollection<ISceneNode> SelectedObjects { get { return _SelectedObjects; } set { SetAndNotify(ref _SelectedObjects, value); } }

        ISceneNodeViewModel _selectedNodeViewModel;
        public ISceneNodeViewModel SelectedNodeViewModel { get { return _selectedNodeViewModel; } set { SetAndNotify(ref _selectedNodeViewModel, value); } }

        public SceneExplorerContextMenuHandler ContextMenu { get; set; }

        MainEditableNode _editableMeshNode;
        public MainEditableNode EditableMeshNode { get => _editableMeshNode; set { _editableMeshNode = value; ContextMenu.EditableMeshNode = value; } }


        public SceneExplorerViewModel(IComponentManager componentManager, PackFileService packFileService, AnimationControllerViewModel animationControllerViewModel)
        {
            _componentManager = componentManager;
            _animationControllerViewModel = animationControllerViewModel;

            _packFileService = packFileService;

            _skeletonAnimationLookUpHelper = _componentManager.GetComponent<SkeletonAnimationLookUpHelper>();
            _sceneManager = _componentManager.GetComponent<SceneManager>();
            _commandExecutor = componentManager.GetComponent<CommandExecutor>();
            _selectionManager = componentManager.GetComponent<SelectionManager>();
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
                SelectedNodeViewModel = SceneNodeViewFactory.Create(SelectedObjects.First(), _skeletonAnimationLookUpHelper, _packFileService, _animationControllerViewModel, _componentManager);
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

        public void Initialize()
        {
        }

        public MainEditableNode GeEditableMeshRootNode()
        {
            return EditableMeshNode;
        }
    }
}
