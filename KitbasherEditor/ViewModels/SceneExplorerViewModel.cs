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


        ISceneNode _selectedNode;
        public ISceneNode SelectedNode { get { return _selectedNode; } set { SetAndNotify(ref _selectedNode, value); OnNodeSelected(_selectedNode); } }

        ISceneNodeViewModel _selectedNodeViewModel;
        public ISceneNodeViewModel SelectedNodeViewModel { get { return _selectedNodeViewModel; } set { SetAndNotify(ref _selectedNodeViewModel, value); } }

        LodItem _selectedLodLvl;
        public LodItem SelectedLodLevel { get { return _selectedLodLvl; } set { SetAndNotify(ref _selectedLodLvl, value); UpdateLod(_selectedLodLvl.Value); } }

        SceneContainer _sceneContainer;
        SceneManager _sceneManager;
        CommandExecutor _commandExecutor;
        SelectionManager _selectionManager;

        public ICommand MakeNodeEditableCommand { get; set; }
        public ICommand DeleteNodeCommand { get; set; }


        public Rmv2ModelNode EditableMeshNode { get; set; }

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

          
            MakeNodeEditableCommand = new RelayCommand<SceneNode>(MakeNodeEditable);
            DeleteNodeCommand = new RelayCommand<SceneNode>(DeleteNode);
        }

        private void SelectionChanged(ISelectionState state)
        {
            if (state is ObjectSelectionState objectSelection)
            {
                if (objectSelection.SelectedObjects().Count == 1)
                {
                    var obj = objectSelection.SelectedObjects().First();
                    if (obj != SelectedNode)
                        SelectedNode = obj as SceneNode;

                    return;
                }
            }

            if(SelectedNode != null)
                SelectedNode = null;
        }

        private void OnNodeSelected(ISceneNode selectedNode)
        {
            SelectedNodeViewModel = SceneNodeViewFactory.Create(selectedNode, _skeletonAnimationLookUpHelper, _packFileService, _animationControllerViewModel);

            if (selectedNode != null)
            {
                var objectState = new ObjectSelectionState();
                if (selectedNode is GroupNode groupNode && groupNode.IsSelectable == true)
                {
                    foreach (var child in groupNode.Children)
                    {
                        if (child is ISelectable selectableNode && selectableNode.IsSelectable)
                            objectState.ModifySelection(selectableNode, false);
                    }
                }
                else
                {
                    if (selectedNode is ISelectable selectableNode && selectableNode.IsSelectable)
                        objectState.ModifySelection(selectableNode, false);
                }

                // Is the state actually changed?
                var currentSelection = _selectionManager.GetState() as ObjectSelectionState;
                bool selectionEqual = false;
                if (currentSelection != null)
                    selectionEqual = currentSelection.IsSelectionEqual(objectState);

                if (!selectionEqual)
                    _selectionManager.SetState(objectState);
            }
        }

        void MakeNodeEditable(SceneNode node)
        {
            if (node is Rmv2MeshNode meshNode)
            {
                node.Parent.RemoveObject(node);
                EditableMeshNode.GetLodNodes()[0].AddObject(node);
                node.IsEditable = true;
                return;
            }

            if (node is Rmv2LodNode lodNode)
            {
                var index = lodNode.LodValue;
                foreach (var lodModel in lodNode.Children)
                {
                    (lodModel as Rmv2MeshNode).IsSelectable = true;
                    EditableMeshNode.GetLodNodes()[0].AddObject(lodModel);
                }
            }

            if (node is Rmv2ModelNode modelNode)
            {
                foreach (var lodChild in modelNode.Children)
                {
                    if (lodChild  is Rmv2LodNode lodNode0)
                    {
                        var index = lodNode0.LodValue;
                        foreach (var lodModel in lodNode0.Children)
                        {
                            if (index > 3)
                                continue;
                            (lodModel as Rmv2MeshNode).IsSelectable = true;
                            EditableMeshNode.GetLodNodes()[0].AddObject(lodModel);
                        }
                        break;
                    }
                }
            }
            node.Parent.RemoveObject(node);
            node.ForeachNode(x => x.IsEditable = true);
        }

        void DeleteNode(SceneNode node)
        {
            var deleteCommand = new DeleteObjectsCommand(node);
            _commandExecutor.ExecuteCommand(deleteCommand);
        }

        private void RebuildTree()
        {
            var collection = new ObservableCollection<ISceneNode>(); ;
            collection.Add(_sceneManager.RootNode);
            SceneGraphRootNodes = collection;
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

        public ISceneNode GetActiveEditableMeshNode()
        {
            return EditableMeshNode.Children[SelectedLodLevel.Value];
        }

        public void Initialize()
        {
        }



        public Rmv2ModelNode GeEditableMeshRootNode()
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
