using System.Collections.ObjectModel;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.Views;

namespace Editors.KitbasherEditor.Core.SceneExplorer
{
    public static class SceneExplorerNodeBuilder
    {
        public static void Update(ObservableCollection<SceneExplorerNode> sceneExplorerNodes, ISceneNode sceneGraphRoot, bool isFirstTime)
        {
            // Get all selected nodes
            // Get all expanded nodes

            sceneExplorerNodes.Clear();
            AddRecursive(sceneExplorerNodes, sceneGraphRoot);

            var rootNode = sceneExplorerNodes.FirstOrDefault();
            var modelNodes = rootNode?.Children
                .Where(x => x.Content is Rmv2ModelNode)
                .ToList();

            if (modelNodes == null)
                return;

            foreach (var modelNode in modelNodes)
            {
                for (var i = 0; i < modelNode.Children.Count(); i++)
                {
                    modelNode.Children[i].Content.IsExpanded = i == 0;
                    modelNode.Children[i].Content.IsVisible = i == 0;
                }
            }
        }

        static void AddRecursive(ObservableCollection<SceneExplorerNode> sceneExplorerNodes, ISceneNode currentNode)
        {
            var newNode = new SceneExplorerNode(currentNode);
            sceneExplorerNodes.Add(newNode);

            foreach (var child in currentNode.Children)
            {
                AddRecursive(newNode.Children, child);
            }
        }

    }
}
