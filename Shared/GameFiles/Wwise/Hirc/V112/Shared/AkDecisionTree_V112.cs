using Shared.Core.ByteParsing;
using static Shared.GameFormats.Wwise.Hirc.ICAkDialogueEvent;

namespace Shared.GameFormats.Wwise.Hirc.V112.Shared
{
    public class AkDecisionTree_V112 : IAkDecisionTree
    {
        public Node_V112 DecisionTree { get; set; } = new Node_V112(); // Root node of the decision tree in hierarchical form
        public List<Node_V112> Nodes { get; set; } = []; // Flattened list of all nodes in the decision tree in sequential order  for read / write

        public void ReadData(ByteChunk chunk, uint uTreeDataSize, uint maxTreeDepth)
        {
            Nodes = new List<Node_V112>();
            uint currentDepth = 0;
            var countMax = uTreeDataSize / new Node_V112().GetSize();

            for (var i = 0; i < countMax; i++)
            {
                Nodes.Add(Node_V112.ReadData(chunk, countMax, currentDepth, maxTreeDepth));
                currentDepth ++;
            }

            ushort childrenCount = 1;
            DecisionTree = ReadDecisionTree(Nodes, 0, maxTreeDepth, 0, ref childrenCount, (ushort)countMax);
        }

        private static Node_V112 ReadDecisionTree(List<Node_V112> nodes, int index, uint maxDepth, uint currentDepth, ref ushort count, ushort countMax)
        {
            if (index >= nodes.Count)
                throw new ArgumentOutOfRangeException("Something went wrong with the number of Decision Tree nodes");

            var node = nodes[index];

            var isOver = node.ChildrenIdx + node.ChildrenCount > countMax;
            var isAudioNode = node.ChildrenIdx > countMax || node.ChildrenCount > countMax || isOver;
            var isMax = currentDepth == maxDepth;
            if (!(isAudioNode || isMax))
            {
                var treeNodeChildren = new List<Node_V112>();
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

        public IAkDecisionNode GetDecisionTree() => DecisionTree;

        public class Node_V112 : IAkDecisionNode
        {
            public uint Key { get; set; }
            public uint AudioNodeId { get; set; }
            public ushort ChildrenIdx { get; set; }
            public ushort ChildrenCount { get; set; }
            public ushort Weight { get; set; }
            public ushort Probability { get; set; }
            public List<Node_V112> Nodes { get; set; } = [];

            public static Node_V112 ReadData(ByteChunk chunk, uint countMax, uint currentDepth, uint maxDepth)
            {
                var node = new Node_V112();
                node.Key = chunk.ReadUInt32();

                var idChildrenPeek = chunk.PeakUint32();
                node.ChildrenIdx = (ushort)((idChildrenPeek >> 0) & 0xFFFF);
                node.ChildrenCount = (ushort)((idChildrenPeek >> 16) & 0xFFFF);

                var isAudioNode = node.ChildrenIdx > countMax || node.ChildrenCount > countMax;
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
                // Either ChildrenIdx and ChildrenCount are used or AudioNodeId is used but in either case the same amount of bytes are used so doesn't matter which one is used to calculate the size here
                var idSize = ByteHelper.GetPropertyTypeSize(Key);
                var childrenIdxSize = ByteHelper.GetPropertyTypeSize(ChildrenIdx);
                var childrenCountSize = ByteHelper.GetPropertyTypeSize(ChildrenCount);
                var weightSize = ByteHelper.GetPropertyTypeSize(Weight);
                var probabilitySize = ByteHelper.GetPropertyTypeSize(Probability);
                return idSize + childrenIdxSize + childrenCountSize + weightSize + probabilitySize;
            }

            public uint GetKey() => Key;
            public uint GetAudioNodeId() => AudioNodeId;
            public int GetChildrenCount() => Nodes.Count;
            public IAkDecisionNode GetChildAtIndex(int index) => Nodes[index];
        }
    }
}
