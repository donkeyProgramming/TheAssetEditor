using System.Collections.ObjectModel;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.Views;

namespace Editors.KitbasherEditor.Core.SceneExplorer
{
    public static class SceneExplorerNodeBuilder
    {
        public static void Update(ObservableCollection<SceneExplorerNode> sceneExplorerNodes, ISceneNode sceneGraphRoot, bool isFirstTime)
        {
            // Get current states so we can update later
            var expandedNodes = GetExpandedState(sceneExplorerNodes);
            var visibleNodes = GetVisabilityState(sceneExplorerNodes);

            // Add nodes
            sceneExplorerNodes.Clear();
            AddRecursive(sceneExplorerNodes, sceneGraphRoot);

            // Add default states
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

            // Update state based on original data
            UpdateStates(sceneExplorerNodes, expandedNodes, visibleNodes);
        }

        static void UpdateStates(ObservableCollection<SceneExplorerNode> sceneExplorerNodes, List<(ISceneNode Node, bool State)> expandedState, List<(ISceneNode Node, bool State)> visibleState)
        {
            foreach (var sceneExplorerNode in sceneExplorerNodes)
            {
                var hasExpandedState = expandedState.Any(x => x.Node == sceneExplorerNode.Content);
                if (hasExpandedState)
                    sceneExplorerNode.Content.IsExpanded = expandedState.First(x => x.Node == sceneExplorerNode.Content).State;

                var hasVisibleState = visibleState.Any(x => x.Node == sceneExplorerNode.Content);
                if (hasVisibleState)
                    sceneExplorerNode.Content.IsVisible = visibleState.First(x => x.Node == sceneExplorerNode.Content).State;

                UpdateStates(sceneExplorerNode.Children, expandedState, visibleState);
            }
        }

        static List<(ISceneNode Node, bool State)> GetExpandedState(ObservableCollection<SceneExplorerNode> sceneExplorerNodes)
        {
            var output = new List<(ISceneNode, bool)>();

            foreach (var sceneExplorerNode in sceneExplorerNodes)
            {
                output.Add((sceneExplorerNode.Content, sceneExplorerNode.Content.IsExpanded));
                var result = GetExpandedState(sceneExplorerNode.Children);
                output.AddRange(result);
            }

            return output;
        }

        static List<(ISceneNode Node, bool State)> GetVisabilityState(ObservableCollection<SceneExplorerNode> sceneExplorerNodes)
        {
            var output = new List<(ISceneNode, bool)>();

            foreach (var sceneExplorerNode in sceneExplorerNodes)
            {
                output.Add((sceneExplorerNode.Content, sceneExplorerNode.Content.IsVisible));
                var result = GetVisabilityState(sceneExplorerNode.Children);
                output.AddRange(result);
            }

            return output;
        }

        static void AddRecursive(ObservableCollection<SceneExplorerNode> sceneExplorerNodes, ISceneNode currentNode, bool isParentReferance = false)
        {
            if (currentNode.Name == SpecialNodes.ReferenceMeshs)
                isParentReferance = true;

            var newNode = new SceneExplorerNode(currentNode, isParentReferance);
            sceneExplorerNodes.Add(newNode);

            foreach (var child in currentNode.Children)
            {
                AddRecursive(newNode.Children, child, isParentReferance);
            }
        }

    }
}
