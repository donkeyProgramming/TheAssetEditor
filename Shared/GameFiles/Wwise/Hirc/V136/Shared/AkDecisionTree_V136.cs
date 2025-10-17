using Shared.Core.ByteParsing;
using static Shared.GameFormats.Wwise.Hirc.ICAkDialogueEvent;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkDecisionTree_V136 : IAkDecisionTree
    {
        // Root node of the decision tree in hierarchical form
        public Node_V136 DecisionTree { get; set; } = new Node_V136(); 
        // Flattened list of all nodes in the decision tree in sequential order for read / write
        public List<Node_V136> Nodes { get; set; } = []; 

        public void ReadData(ByteChunk chunk, uint treeDataSize, uint maxTreeDepth)
        {
            Nodes = [];
            uint currentDepth = 0;
            var countMax = treeDataSize / new Node_V136().GetSize();

            for (var i = 0; i < countMax; i++)
            {
                Nodes.Add(Node_V136.ReadData(chunk, countMax, currentDepth, maxTreeDepth));
                currentDepth++;
            }

            ushort childrenCount = 1;
            var startIndex = 0;
            uint startDepth = 0;
            DecisionTree = ReadDecisionTree(Nodes, startIndex, maxTreeDepth, startDepth, ref childrenCount, (ushort)countMax);
        }

        private static Node_V136 ReadDecisionTree(List<Node_V136> nodes, int index, uint maxDepth, uint currentDepth, ref ushort count, ushort countMax)
        {
            if (index >= nodes.Count)
                throw new ArgumentOutOfRangeException("Something went wrong with the number of Decision Tree nodes");

            var node = nodes[index];

            var isOver = node.ChildrenIdx + node.ChildrenCount > countMax;
            var isAudioNode = node.ChildrenIdx > countMax || node.ChildrenCount > countMax || isOver;
            var isMax = currentDepth == maxDepth;
            if (!(isAudioNode || isMax))
            {
                var treeNodeChildren = new List<Node_V136>();
                for (var i = 0; i < node.ChildrenCount; i++)
                {
                    var childNode = ReadDecisionTree(nodes, node.ChildrenIdx + i, maxDepth, currentDepth + 1, ref count, countMax);
                    if (childNode != null)
                        treeNodeChildren.Add(childNode);
                }
                node.Nodes = treeNodeChildren;
            }

            count += (ushort)node.Nodes.Count;
            return node;
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            foreach (var node in Nodes)
            {
                memStream.Write(ByteParsers.UInt32.EncodeValue(node.Key, out _), 0, 4);

                var hasChildren = node.Nodes != null && node.Nodes.Count > 0;
                if (!hasChildren)
                    memStream.Write(ByteParsers.UInt32.EncodeValue(node.AudioNodeId, out _), 0, 4);
                else
                {
                    memStream.Write(ByteParsers.UShort.EncodeValue(node.ChildrenIdx, out _), 0, 2);
                    memStream.Write(ByteParsers.UShort.EncodeValue(node.ChildrenCount, out _), 0, 2);
                }

                memStream.Write(ByteParsers.UShort.EncodeValue(node.Weight, out _), 0, 2);
                memStream.Write(ByteParsers.UShort.EncodeValue(node.Probability, out _), 0, 2);
            }
            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var nodeSize = new Node_V136().GetSize();
            return (uint)Nodes.Count * nodeSize;
        }

        public AkDecisionTree_V136 Clone()
        {
            return new AkDecisionTree_V136
            {
                DecisionTree = DecisionTree.Clone(),
                Nodes = Nodes.Select(node => node.CloneWithoutChildren()).ToList()
            };
        }

        public static Node_V136 MergeDecisionTrees(Node_V136 baseDecisionTree, Node_V136 mergingDecisionTree)
        {
            if (baseDecisionTree == null)
                return mergingDecisionTree.Clone();

            if (mergingDecisionTree == null)
                return baseDecisionTree.Clone();

            var targetIsLeaf = baseDecisionTree.Nodes == null || baseDecisionTree.Nodes.Count == 0 || baseDecisionTree.AudioNodeId != 0;
            if (targetIsLeaf)
                return baseDecisionTree.Clone();

            var mergedNodes = new List<Node_V136>(baseDecisionTree.Nodes!);
            var index = baseDecisionTree.Nodes!.ToDictionary(node => node.Key);

            foreach (var mergingChild in mergingDecisionTree.Nodes ?? [])
            {
                if (index.TryGetValue(mergingChild.Key, out var baseChild))
                {
                    var mergedDecisionTree = MergeDecisionTrees(baseChild, mergingChild);
                    if (!ReferenceEquals(baseChild, mergedDecisionTree))
                        mergedNodes[mergedNodes.FindIndex(node => node.Key == baseChild.Key)] = mergedDecisionTree;
                }
                else
                    mergedNodes.Add(mergingChild.Clone());
            }

            mergedNodes = SortNodes(mergedNodes);

            return CreateMergedNode(baseDecisionTree, mergingDecisionTree, mergedNodes);
        }

        private static uint GetKey(Node_V136 baseDecisionTree, Node_V136 mergingDecisionTree)
        {
            if (baseDecisionTree.Key != 0)
                return baseDecisionTree.Key;

            return mergingDecisionTree.Key;
        }

        private static ushort GetWeight(Node_V136 baseDecisionTree, Node_V136 mergingDecisionTree)
        {
            if (baseDecisionTree.Weight != 0)
                return baseDecisionTree.Weight;

            return mergingDecisionTree.Weight;
        }

        private static ushort GetProbability(Node_V136 baseDecisionTree, Node_V136 mergingDecisionTree)
        {
            if (baseDecisionTree.Probability != 0)
                return baseDecisionTree.Probability;

            return mergingDecisionTree.Probability;
        }

        public static List<Node_V136> FlattenDecisionTree(Node_V136 rootNode)
        {
            if (rootNode == null)
                return [];

            var flattenedDecisionTree = new List<Node_V136> { rootNode };
            PrepareAndFlattenChildren(rootNode, flattenedDecisionTree);
            return flattenedDecisionTree;
        }

        private static void PrepareAndFlattenChildren(Node_V136 node, List<Node_V136> flattened)
        {
            var hasChildren = node.Nodes != null && node.Nodes.Count > 0;
            if (!hasChildren)
            {
                node.ChildrenIdx = 0;
                node.ChildrenCount = 0;
                return;
            }

            node.AudioNodeId = 0;
            node.Nodes = SortNodes(node.Nodes!);

            node.ChildrenIdx = (ushort)flattened.Count;
            node.ChildrenCount = (ushort)node.Nodes.Count;

            foreach (var child in node.Nodes)
                flattened.Add(child);

            foreach (var child in node.Nodes)
                PrepareAndFlattenChildren(child, flattened);
        }

        private static List<Node_V136> SortNodes(IEnumerable<Node_V136> nodes)
        {
            return nodes
                .OrderBy(node => node.Key)
                .ToList();
        }

        private static Node_V136 CreateMergedNode(Node_V136 baseDecisionTree, Node_V136 mergingDecisionTree, List<Node_V136> mergedNodes)
        {
            return new Node_V136
            {
                Key = GetKey(baseDecisionTree, mergingDecisionTree),
                AudioNodeId = 0,
                Weight = GetWeight(baseDecisionTree, mergingDecisionTree),
                Probability = GetProbability(baseDecisionTree, mergingDecisionTree),
                Nodes = mergedNodes
            };
        }

        public IAkDecisionNode GetDecisionTree() => DecisionTree;

        public class Node_V136 : IAkDecisionNode
        {
            public uint Key { get; set; }
            public uint AudioNodeId { get; set; }
            public ushort ChildrenIdx { get; set; }
            public ushort ChildrenCount { get; set; }
            public ushort Weight { get; set; }
            public ushort Probability { get; set; }
            public List<Node_V136> Nodes { get; set; } = [];

            public static Node_V136 ReadData(ByteChunk chunk, uint countMax, uint currentDepth, uint maxDepth)
            {
                var node = new Node_V136();
                node.Key = chunk.ReadUInt32();

                var idChildrenPeek = chunk.PeakUint32();
                node.ChildrenIdx = (ushort)((idChildrenPeek >> 0) & 0xFFFF);
                node.ChildrenCount = (ushort)((idChildrenPeek >> 16) & 0xFFFF);

                var isOver = node.ChildrenIdx + node.ChildrenCount > countMax;
                var isAudioNode = node.ChildrenIdx > countMax || node.ChildrenCount > countMax || isOver;
                var isMax = currentDepth == maxDepth;
                if (isAudioNode || isMax)
                    node.AudioNodeId = chunk.ReadUInt32();
                else
                {
                    node.ChildrenIdx = chunk.ReadUShort();
                    node.ChildrenCount = chunk.ReadUShort();
                }

                node.Weight = chunk.ReadUShort();
                node.Probability = chunk.ReadUShort();
                return node;
            }

            public uint GetSize()
            {
                // Either ChildrenIdx and ChildrenCount are used or AudioNodeId is used but in either case the
                // same amount of bytes are used so doesn't matter which one is used to calculate the size here
                var idSize = ByteHelper.GetPropertyTypeSize(Key);
                var childrenIdxSize = ByteHelper.GetPropertyTypeSize(ChildrenIdx);
                var childrenCountSize = ByteHelper.GetPropertyTypeSize(ChildrenCount);
                var weightSize = ByteHelper.GetPropertyTypeSize(Weight);
                var probabilitySize = ByteHelper.GetPropertyTypeSize(Probability);
                return idSize + childrenIdxSize + childrenCountSize + weightSize + probabilitySize;
            }

            public Node_V136 Clone()
            {
                var clonedNode = CloneWithoutChildren();
                clonedNode.Nodes = (Nodes != null) ? Nodes.Select(child => child.Clone()).ToList() : [];
                return clonedNode;
            }

            public Node_V136 CloneWithoutChildren()
            {
                return new Node_V136
                {
                    Key = Key,
                    AudioNodeId = AudioNodeId,
                    ChildrenIdx = ChildrenIdx,
                    ChildrenCount = ChildrenCount,
                    Weight = Weight,
                    Probability = Probability,
                    Nodes = []
                };
            }

            public uint GetKey() => Key;
            public uint GetAudioNodeId() => AudioNodeId;
            public int GetChildrenCount() => Nodes.Count;
            public IAkDecisionNode GetChildAtIndex(int index) => Nodes[index];
        }
    }
}
