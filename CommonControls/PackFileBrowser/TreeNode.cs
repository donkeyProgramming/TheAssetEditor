using Common;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace CommonControls.PackFileBrowser
{
    public enum NodeType
    { 
        Root,
        Directory,
        File
    }

    class TreeNodeController
    {
        bool _packFileTreeRebuildSuspended;

        void ExpandNode(TreeNode node)
        { 
            // Find the full path of the node being expanded

            // Find all files in that directory 

            // Add the files and folder

            // Check if folders have sub items, if so, add an empty child
        }

       /* private void PackFileContainerLoaded(PackFileContainer container)
        {
            if (_packFileTreeRebuildSuspended)
                return;
            var existingNode = Files.FirstOrDefault(x => x.FileOwner == container);
            if (existingNode != null)
                Files.Remove(existingNode);

            var root = new TreeNode(container.Name, NodeType.Root, container, null);
            root.IsMainEditabelPack = _packFileService.GetEditablePack() == container;
            Dictionary<string, TreeNode> directoryMap = new Dictionary<string, TreeNode>();

            foreach (var item in container.FileList)
            {
                var fullPath = item.Key;
                var numSeperators = fullPath.Count(x => x == Path.DirectorySeparatorChar);

                var directoryEnd = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
                var fileName = fullPath.Substring(directoryEnd + 1);

                if (numSeperators == 0)
                {
                    root.Children.Add(new TreeNode(fileName, NodeType.File, container, root, item.Value));
                }
                else
                {
                    var directory = fullPath.Substring(0, directoryEnd);
                    var res = directoryMap.TryGetValue(directory, out var node);
                    if (!res)
                    {
                        var currentIndex = 0;
                        var lastIndex = 0;

                        TreeNode lastNode = root;
                        for (int i = 0; i < numSeperators; i++)
                        {
                            currentIndex = fullPath.IndexOf(Path.DirectorySeparatorChar, currentIndex);
                            var subStr = fullPath.Substring(0, currentIndex);
                            if (directoryMap.ContainsKey(subStr) == false)
                            {
                                var nodeName = subStr;
                                if (lastIndex != 0)
                                    nodeName = fullPath.Substring(lastIndex + 1, currentIndex - lastIndex - 1);
                                var currentNode = new TreeNode(nodeName, NodeType.Directory, container, lastNode);
                                lastNode.Children.Add(currentNode);
                                directoryMap.Add(subStr, currentNode);
                                lastNode = currentNode;
                            }
                            else
                            {
                                lastNode = directoryMap[subStr];
                            }
                            lastIndex = currentIndex;
                            currentIndex++;
                        }
                    }
                    directoryMap[directory].Children.Add(new TreeNode(fileName, NodeType.File, container, directoryMap[directory], item.Value));
                }
            }
            Files.Add(root);
            root.IsNodeExpanded = true;
        }*/


    }

    public class TreeNode : NotifyPropertyChangedImpl
    {
        public PackFileContainer FileOwner { get; set; }
        public IPackFile Item { get; set; }

        bool _isExpanded = false;
        public bool IsNodeExpanded
        {
            get => _isExpanded;
            set 
            {  
                SetAndNotify(ref _isExpanded, value);
            }
        }

        public NodeType NodeType { get; set; }
        public TreeNode Parent { get; set; }
        public ObservableCollection<TreeNode> Children { get; set; } = new ObservableCollection<TreeNode>();


        bool _unsavedChanged;
        public bool UnsavedChanged { get => _unsavedChanged; set => SetAndNotify(ref _unsavedChanged, value); }

        bool _isMainEditabelPack;
        public bool IsMainEditabelPack { get => _isMainEditabelPack; set => SetAndNotify(ref _isMainEditabelPack, value); }

        bool _Visibility = true;
        public bool IsVisible { get => _Visibility; set => SetAndNotify(ref _Visibility, value); }

        string _name = "";
        public string Name { get => _name; set => SetAndNotify(ref _name, value); }
        public TreeNode(string name, NodeType type, PackFileContainer ower, TreeNode parent, IPackFile packFile = null)
        {
            Name = name;
            NodeType = type;
            Item = packFile;
            FileOwner = ower;
            Parent = parent;
        }

        public string GetFullPath()
        {
            if (NodeType == NodeType.Root)
                return "";

            var currentParent = Parent;
            var path = Name;
            while (currentParent != null)
            {
                if (currentParent.NodeType == NodeType.Root)
                    break;

                path = currentParent.Name + "\\" + path;
                currentParent = currentParent.Parent;
            }

            return path;
        }

        public override string ToString()
        {
            return Name;
        }


        public List<TreeNode> GetAllChildFileNodes()
        {
            var output = new List<TreeNode>();

            var nodes = new Stack<TreeNode>(new[] { this });
            while (nodes.Any())
            {
                TreeNode node = nodes.Pop();
                if(node.NodeType == NodeType.File)
                    output.Add(node);

                foreach (var n in node.Children) 
                    nodes.Push(n);
            }


            return output;
        }

        public void RemoveSelf()
        {
            foreach (var child in Children)
                child.RemoveSelf();

            Children.Clear();
            Parent = null;
        }

        public void ForeachNode(Action<TreeNode> func)
        {
            func.Invoke(this);
            foreach (var child in Children)
                child.ForeachNode(func);
        }

        public void ExpandIfVisible(bool includeChildren = true)
        {
            if (IsVisible)
            {
                IsNodeExpanded = true;
                if (includeChildren)
                {
                    foreach (var child in Children)
                        child.ExpandIfVisible(includeChildren);
                }
            }
        }
    }
}
