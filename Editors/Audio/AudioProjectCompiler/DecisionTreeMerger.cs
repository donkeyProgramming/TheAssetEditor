using System.Collections.Generic;
using System.Linq;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.AudioProjectCompiler
{
    public static class DecisionTreeMerger
    {
        public static AkDecisionTree_V136.Node_V136 MergeDecisionTrees(AkDecisionTree_V136.Node_V136 target,  AkDecisionTree_V136.Node_V136 source)
        {
            if (target == null) 
                return CloneNode(source);
            if (source == null) 
                return CloneNode(target);

            var targetIsLeaf = target.Nodes == null || target.Nodes.Count == 0 || target.AudioNodeId != 0;
            if (targetIsLeaf)
                return CloneNode(target);

            var mergedChildren = new List<AkDecisionTree_V136.Node_V136>(target.Nodes);
            var index = target.Nodes.ToDictionary(n => n.Key);

            foreach (var sourceChild in source.Nodes ?? [])
            {
                if (index.TryGetValue(sourceChild.Key, out var targetChild))
                {
                    var merged = MergeDecisionTrees(targetChild, sourceChild);
                    if (!ReferenceEquals(targetChild, merged))
                        mergedChildren[mergedChildren.FindIndex(node => node.Key == targetChild.Key)] = merged;
                }
                else
                    mergedChildren.Add(CloneNode(sourceChild));
            }

            mergedChildren = SortChildren(mergedChildren);

            return new AkDecisionTree_V136.Node_V136
            {
                Key = target.Key ?? source.Key,
                AudioNodeId = 0,
                Weight = target.Weight == 0 ? source.Weight : target.Weight,
                Probability = target.Probability == 0 ? source.Probability : target.Probability,
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
            node.Nodes = SortChildren(node.Nodes);

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
    }
}
