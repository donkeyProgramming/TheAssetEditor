using Common;
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

namespace KitbasherEditor.ViewModels
{
   



    public class SceneExplorerViewModel : NotifyPropertyChangedImpl, IEditableMeshResolver
    {
        public ObservableCollection<SceneNode> _sceneGraphRootNodes = new ObservableCollection<SceneNode>();
        public ObservableCollection<SceneNode> SceneGraphRootNodes { get { return _sceneGraphRootNodes; } set { SetAndNotify(ref _sceneGraphRootNodes, value); } }


        SceneNode _selectedNode;
        public SceneNode SelectedNode { get { return _selectedNode; } set { SetAndNotify(ref _selectedNode, value); CreateNodeViewModel(_selectedNode); } }

        ISceneNodeViewModel _selectedNodeViewModel;
        public ISceneNodeViewModel SelectedNodeViewModel { get { return _selectedNodeViewModel; } set { SetAndNotify(ref _selectedNodeViewModel, value); } }

        LodItem _selectedLodLvl;
        public LodItem SelectedLodLevel { get { return _selectedLodLvl; } set { SetAndNotify(ref _selectedLodLvl, value); UpdateLod(_selectedLodLvl.Value); } }

        SceneContainer _sceneContainer;
        SceneManager _sceneManager;
        CommandExecutor _commandExecutor;

        public ICommand MakeNodeEditableCommand { get; set; }
        public ICommand DeleteNodeCommand { get; set; }


        public Rmv2ModelNode EditableMeshNode { get; set; }
        
        public SceneExplorerViewModel(SceneContainer sceneContainer)
        {
            _selectedLodLvl = LodItem.GetAll.First();

            _sceneContainer = sceneContainer;
            _sceneManager = _sceneContainer.GetComponent<SceneManager>();
            _commandExecutor = sceneContainer.GetComponent<CommandExecutor>();

            SceneGraphRootNodes.Add(_sceneManager.RootNode);

            _sceneManager.SceneObjectAdded += (a, b) => RebuildTree();
            _sceneManager.SceneObjectRemoved += (a, b) => RebuildTree();

          

            MakeNodeEditableCommand = new RelayCommand<SceneNode>(MakeNodeEditable);
            DeleteNodeCommand = new RelayCommand<SceneNode>(DeleteNode);
        }

        void MakeNodeEditable(SceneNode node)
        {
            if (node is MeshNode meshNode)
            {
                EditableMeshNode.Children[SelectedLodLevel.Value].AddObject(meshNode);
            }

            if (node is Rmv2LodNode lodNode)
            {
                var index = lodNode.LodValue;
                foreach(var lodModel in lodNode.Children)
                    EditableMeshNode.Children[index].AddObject(lodModel);
            }

            if (node is Rmv2ModelNode modelNode)
            {
                foreach (var lodChild in modelNode.Children)
                {
                    if (lodChild is Rmv2LodNode lodNode0)
                    {
                        var index = lodNode0.LodValue;
                        foreach (var lodModel in lodNode0.Children)
                            EditableMeshNode.Children[index].AddObject(lodModel);
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
            var collection = new ObservableCollection<SceneNode>(); ;
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

        public SceneNode GetEditableMeshNode()
        {
            return EditableMeshNode.Children[SelectedLodLevel.Value];
        }

        public void Initialize()
        {
        }

        private void CreateNodeViewModel(SceneNode selectedNode)
        {
            SelectedNodeViewModel = SceneNodeViewFactory.Create(selectedNode);
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
