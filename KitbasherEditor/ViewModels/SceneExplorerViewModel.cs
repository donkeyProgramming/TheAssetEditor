
using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        SceneContainer _sceneContainer;
        SceneManager _sceneManager;

        public SceneExplorerViewModel(SceneContainer sceneContainer)
        {
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
    }
}
