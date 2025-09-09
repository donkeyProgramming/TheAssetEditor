using System.Collections.Generic;
using System.Linq;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.AudioProjectCompiler
{
    public static class DecisionTreeMerger
    {
        public static AkDecisionTree_V136.Node_V136 MergeDecisionTrees(AkDecisionTree_V136.Node_V136 decisionTree1, AkDecisionTree_V136.Node_V136 decisionTree2)
        {
            if (decisionTree1 == null)
                return CloneNode(decisionTree2);
            if (decisionTree2 == null)
                return CloneNode(decisionTree1);

            var children1 = decisionTree1.Nodes ?? [];
            var children2 = decisionTree2.Nodes ?? [];

            var keys = children1.Select(child => child.Key)
                .Concat(children2.Select(child => child.Key))
                .Distinct();

            var mergedChildren = new List<AkDecisionTree_V136.Node_V136>();
            foreach (var key in keys)
            {
                var tree1Child = children1.FirstOrDefault(child => child.Key == key);
                var tree2Child = children2.FirstOrDefault(child => child.Key == key);

                if (tree1Child != null && tree2Child != null)
                    mergedChildren.Add(MergeDecisionTrees(tree1Child, tree2Child));
                else
                    mergedChildren.Add(CloneNode(tree1Child ?? tree2Child));
            }

            mergedChildren = SortChildren(mergedChildren).ToList();

            return new AkDecisionTree_V136.Node_V136
            {
                Key = GetMergedKey(decisionTree1, decisionTree2),
                AudioNodeId = GetMergedAudioNodeId(decisionTree1, decisionTree2, mergedChildren.Count),
                Weight = GetMergedWeight(decisionTree1, decisionTree2),
                Probability = GetMergedProbability(decisionTree1, decisionTree2),
                Nodes = mergedChildren
            };

        }

        public static List<AkDecisionTree_V136.Node_V136> FlattenDecisionTree(AkDecisionTree_V136.Node_V136 rootNode)
        {
            if (rootNode == null)
                return [];

            var flattenedDecisionTree = new List<AkDecisionTree_V136.Node_V136> { rootNode };
            PrepareAndFlattenChildren(rootNode, flattenedDecisionTree);
            return flattenedDecisionTree;
        }

        private static void PrepareAndFlattenChildren(AkDecisionTree_V136.Node_V136 node, List<AkDecisionTree_V136.Node_V136> flattened)
        {
            var hasChildren = node.Nodes != null && node.Nodes.Count > 0;
            if (!hasChildren)
            {
                node.ChildrenIdx = 0;
                node.ChildrenCount = 0;
                return;
            }

            node.AudioNodeId = 0;
            node.Nodes = SortChildren(node.Nodes).ToList();

            node.ChildrenIdx = (ushort)flattened.Count;
            node.ChildrenCount = (ushort)node.Nodes.Count;

            foreach (var child in node.Nodes)
                flattened.Add(child);

            foreach (var child in node.Nodes)
                PrepareAndFlattenChildren(child, flattened);
        }

        private static AkDecisionTree_V136.Node_V136 CloneNode(AkDecisionTree_V136.Node_V136 node)
        {
            if (node == null) 
                return null;

            var clonedNodes = new List<AkDecisionTree_V136.Node_V136>();
            if (node.Nodes != null)
            {
                foreach (var childNode in node.Nodes)
                    clonedNodes.Add(CloneNode(childNode));
            }

            return new AkDecisionTree_V136.Node_V136
            {
                Key = node.Key,
                AudioNodeId = node.AudioNodeId,
                ChildrenIdx = node.ChildrenIdx,
                ChildrenCount = node.ChildrenCount,
                Weight = node.Weight,
                Probability = node.Probability,
                Nodes = clonedNodes
            };
        }

        private static List<AkDecisionTree_V136.Node_V136> SortChildren(IEnumerable<AkDecisionTree_V136.Node_V136> nodes)
        {
            return nodes
                .OrderBy(node => node.Key.HasValue ? 0 : 1)
                .ThenBy(node => node.Key ?? 0U)
                .ToList();
        }

        private static uint? GetMergedKey(AkDecisionTree_V136.Node_V136 decisionTree1, AkDecisionTree_V136.Node_V136 decisionTree2)
        {
            if (decisionTree1.Key != null)
                return decisionTree1.Key;

            return decisionTree2.Key;
        }

        private static uint GetMergedAudioNodeId(AkDecisionTree_V136.Node_V136 decisionTree1, AkDecisionTree_V136.Node_V136 decisionTree2, int mergedChildrenCount)
        {
            var isLeaf = mergedChildrenCount == 0;
            if (!isLeaf)
                return 0;

            if (decisionTree1.AudioNodeId != 0)
                return decisionTree1.AudioNodeId;

            return decisionTree2.AudioNodeId;
        }

        private static ushort GetMergedWeight(AkDecisionTree_V136.Node_V136 decisionTree1, AkDecisionTree_V136.Node_V136 decisionTree2)
        {
            if (decisionTree1.Weight != 0)
                return decisionTree1.Weight;

            return decisionTree2.Weight;
        }

        private static ushort GetMergedProbability(AkDecisionTree_V136.Node_V136 decisionTree1, AkDecisionTree_V136.Node_V136 decisionTree2)
        {
            if (decisionTree1.Probability != 0)
                return decisionTree1.Probability;

            return decisionTree2.Probability;
        }
    }
}
