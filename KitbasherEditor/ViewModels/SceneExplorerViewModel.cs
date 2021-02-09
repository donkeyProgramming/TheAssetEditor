
using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using View3D.Components.Component;
using View3D.Scene;

namespace KitbasherEditor.ViewModels
{
    public class SceneExplorerViewModel : NotifyPropertyChangedImpl
    {
        public ObservableCollection<SceneNode> _sceneGraphRootNodes = new ObservableCollection<SceneNode>();
        public ObservableCollection<SceneNode> SceneGraphRootNodes { get { return _sceneGraphRootNodes; } set { SetAndNotify(ref _sceneGraphRootNodes, value); } }


        SceneNode _selectedNode;
        public SceneNode SelectedNode { get { return _selectedNode; } set { SetAndNotify(ref _selectedNode, value); } }

        LodItem _selectedLodLvl;
        public LodItem SelectedLodLevel { get { return _selectedLodLvl; } set { SetAndNotify(ref _selectedLodLvl, value); UpdateLod(_selectedLodLvl.Value); } }

        SceneContainer _sceneContainer;
        SceneManager _sceneManager;

        public SceneExplorerViewModel(SceneContainer sceneContainer)
        {
            _selectedLodLvl = LodItem.GetAll.First();

            _sceneContainer = sceneContainer;
            _sceneManager = _sceneContainer.GetComponent<SceneManager>();

            SceneGraphRootNodes.Add(_sceneManager.RootNode);

            _sceneManager.SceneObjectAdded += (a, b) => RebuildTree();
            _sceneManager.SceneObjectRemoved += (a, b) => RebuildTree();
        }

        private void RebuildTree()
        {
            var collection = new ObservableCollection<SceneNode>(); ;
            collection.Add(_sceneManager.RootNode);
            SceneGraphRootNodes = collection;
        }

        void UpdateLod(int newLodLevel)
        {
            var allModelNodes = _sceneManager.GetEnumeratorConditional(x => x is Rmv2ModelNode);
            foreach (var item in allModelNodes)
            {
                for (int i = 0; i < item.Children.Count(); i++)
                    item.Children[i].IsVisible = i == newLodLevel;
            }
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
