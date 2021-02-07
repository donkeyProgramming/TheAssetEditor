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
        public ObservableCollection<Node> _sceneGraphRootNodes = new ObservableCollection<Node>();
        public ObservableCollection<Node> SceneGraphRootNodes { get { return _sceneGraphRootNodes; } set { SetAndNotify(ref _sceneGraphRootNodes, value); } }

        Node _selectedNode;
        public Node SelectedNode { get { return _selectedNode; } set { SetAndNotify(ref _selectedNode, value); } }


        SceneContainer _sceneContainer;
        SceneManager _sceneManager;

        public SceneExplorerViewModel(SceneContainer sceneContainer)
        {
            _sceneContainer = sceneContainer;
            _sceneManager = _sceneContainer.GetComponent<SceneManager>();

            var root = new RootNode();

            root.AddChild(new AnimationNode());
            var models = root.AddChild(new ModelsNode());
            models.AddChild(new ModelNode("Face"));
            models.AddChild(new ModelNode("Ass"));

            var refs = root.AddChild(new ReferenceModelsNode());
            refs.AddChild(new ModelNode("Reference 0", true));
            refs.AddChild(new ModelNode("Reference 1", true));

            SceneGraphRootNodes.Add(root);
        }





    }


    public abstract class Node
    {
        public virtual bool IsReference { get;} = false; 
        public virtual string DisplayName { get; }
        public ObservableCollection<Node> Children { get; set; } = new ObservableCollection<Node>();
        public Node AddChild(Node node)
        {
            Children.Add(node);
            return node;
        }

        public bool IsChecked { get; set; } = true;
        public bool IsExpanded { get; set; } = true;
    }


    public class RootNode : Node
    {
        public override string DisplayName => "Root";
    }

    public class AnimationNode : Node 
    {
        public override string DisplayName => "Animation";
    }

    public class ModelsNode : Node
    {
        public override string DisplayName => "Editable Object";
    }

    public class ReferenceModelsNode : Node
    {
        public override string DisplayName => "References";
        public override bool IsReference { get => true; }
    }

    public class ModelNode : Node
    {
        string _displaName;
        public override string DisplayName => _displaName;

        bool _isReference = false;
        public override bool IsReference { get => _isReference; }

        public ModelNode(string name, bool isRef = false)
        {
            _isReference = isRef;
            _displaName = name;
        }
    }
}
