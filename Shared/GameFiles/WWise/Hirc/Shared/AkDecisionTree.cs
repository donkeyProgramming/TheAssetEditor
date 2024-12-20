using System.Text;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.Shared
{
    public class AkDecisionTree
    {
        public Node Root { get; set; }
        public static List<BinaryNode> FlattenedTree;

        public class BinaryNode
        {
            public uint? Key { get; set; }
            public uint AudioNodeId { get; set; }
            public ushort Children_uIdx { get; set; }
            public ushort Children_uCount { get; set; }
            public ushort UWeight { get; set; }
            public ushort UProbability { get; set; }
            public static readonly int SerializationByteSize = 12;

            public BinaryNode(ByteChunk chunk)
            {
                Key = chunk.ReadUInt32();
                AudioNodeId = chunk.PeakUint32();
                Children_uIdx = chunk.ReadUShort();
                Children_uCount = chunk.ReadUShort();
                UWeight = chunk.ReadUShort();
                UProbability = chunk.ReadUShort();
            }

            public BinaryNode()
            {
                Key = 0;
                AudioNodeId = 0;
                Children_uIdx = 0;
                Children_uCount = 0;
                UWeight = 0;
                UProbability = 0;
            }
        }

        public class Node
        {
            public bool IsAudioNode() => Children.Count == 0;
            public uint? Key { get; set; }
            public uint AudioNodeId { get; set; }
            public ushort Children_uIdx { get; set; }
            public ushort Children_uCount { get; set; }
            public ushort UWeight { get; set; }
            public ushort UProbability { get; set; }
            public List<Node> Children { get; set; } = [];

            public Node(BinaryNode sNode)
            {
                Key = sNode.Key;
                AudioNodeId = sNode.AudioNodeId;
                Children_uIdx = sNode.Children_uIdx;
                Children_uCount = sNode.Children_uCount;
                UWeight = sNode.UWeight;
                UProbability = sNode.UProbability;
            }
        }

        private static void PrintBinaryNodes(List<BinaryNode> binaryNodes, int depth)
        {
            foreach (var node in binaryNodes)
            {
                var indent = new string(' ', depth * 2);
                if (node.AudioNodeId != 0)
                    Console.WriteLine(new string(' ', depth * 2) + $"Key: {node.Key}, AudioNodeId: {node.AudioNodeId}, uWeight: {node.UWeight}, uProbability: {node.UProbability}");

                else
                    Console.WriteLine(new string(' ', depth * 2) + $"Key: {node.Key}, Children_uIdx: {node.Children_uIdx}, Children_uCount: {node.Children_uCount}, uWeight: {node.UWeight}, uProbability: {node.UProbability}");
            }
        }

        private static void PrintGraph(Node node, int depth)
        {
            if (node.AudioNodeId != 0)
                Console.WriteLine(new string(' ', depth * 2) + $"Key: {node.Key}, AudioNodeId: {node.AudioNodeId}, uWeight: {node.UWeight}, uProbability: {node.UProbability}");
            else
                Console.WriteLine(new string(' ', depth * 2) + $"Key: {node.Key}, Children_uIdx: {node.Children_uIdx}, Children_uCount: {node.Children_uCount}, uWeight: {node.UWeight}, uProbability: {node.UProbability}");

            foreach (var child in node.Children)
                PrintGraph(child, depth + 1);
        }


        // Sorts nodes into order by ID recursively at each depth level
        private static void SortNodes(Node node)
        {
            // Sort children of the current node
            node.Children.Sort((node1, node2) => node1.Key.Value.CompareTo(node2.Key.Value));

            // Recursively sort children of each child node
            foreach (var child in node.Children)
                SortNodes(child);
        }

        private static void UpdateChildrenData(Node node, ref ushort childrenId)
        {
            node.Children_uIdx = childrenId;

            var childrenCount = node.Children.Count;
            node.Children_uCount = (ushort)childrenCount;

            childrenId += (ushort)childrenCount; // Incrementing the value in the caller's scope

            foreach (var child in node.Children)
                UpdateChildrenData(child, ref childrenId); // Pass by reference
        }

        private static int CountNodeDescendants(Node node)
        {
            var count = node.Children.Count;

            foreach (var child in node.Children)
                count += CountNodeDescendants(child);

            return count;
        }

        private static string ConvertGraphToString(Node node, int depth)
        {
            var stringBuilder = new StringBuilder();

            if (node.AudioNodeId != 0)
                stringBuilder.AppendLine(new string(' ', depth * 2) + $"Key: {node.Key}, AudioNodeId: {node.AudioNodeId}, uWeight: {node.UWeight}, uProbability: {node.UProbability}");
            else
                stringBuilder.AppendLine(new string(' ', depth * 2) + $"Key: {node.Key}, Children_uIdx: {node.Children_uIdx}, Children_uCount: {node.Children_uCount}, uWeight: {node.UWeight}, uProbability: {node.UProbability}");

            foreach (var child in node.Children)
                stringBuilder.Append(ConvertGraphToString(child, depth + 1));

            return stringBuilder.ToString();
        }

        static Node ConvertListToGraph(List<BinaryNode> flattenedTree, uint maxTreeDepth, ushort parentsFirstChildIndex, ushort childIndex, uint currentDepth)
        {
            var sNode = flattenedTree[parentsFirstChildIndex + childIndex];
            var isAtMaxDepth = currentDepth == maxTreeDepth;
            var isOutsideRange = sNode.Children_uIdx >= flattenedTree.Count;

            if (isAtMaxDepth || isOutsideRange)
            {
                sNode.Children_uIdx = 0;
                sNode.Children_uCount = 0;
                return new Node(sNode);
            }
            else
            {
                sNode.AudioNodeId = 0;
                var pathNode = new Node(sNode);
                for (var i = 0; i < sNode.Children_uCount; i++)
                {
                    var childNode = ConvertListToGraph(flattenedTree, maxTreeDepth, sNode.Children_uIdx, (ushort)i, currentDepth + 1);
                    pathNode.Children.Add(childNode);
                }
                return pathNode;
            }
        }

        static int ConvertGraphToList(Node node, List<BinaryNode> flattenedTree, int currentIndex)
        {
            // Add the current node to the flattened tree
            var binaryNode = new BinaryNode
            {
                Key = node.Key,
                AudioNodeId = node.AudioNodeId,
                Children_uIdx = node.Children_uIdx,
                Children_uCount = node.Children_uCount,
                UWeight = node.UWeight,
                UProbability = node.UProbability
            };

            // Insert the current node at the currentIndex
            flattenedTree.Insert(currentIndex, binaryNode);

            // Calculate the insert index for children based on current index
            var insertIndex = currentIndex + 1;

            // Traverse children in a depth-first manner
            foreach (var child in node.Children)
            {
                currentIndex++;
                currentIndex = ConvertGraphToList(child, flattenedTree, insertIndex);
                insertIndex++;
            }

            return currentIndex;
        }

        public static List<BinaryNode> ReflattenTree(Node rootNode, uint uTreeDepth)
        {
            // Sort nodes into order by ID at each depth level
            SortNodes(rootNode);

            // Update / verify all child ID and Count data in nodes
            ushort childrenId = 1;
            UpdateChildrenData(rootNode, ref childrenId);

            Console.WriteLine($"======================= PRINTING CUSTOM DECISION TREE GRAPH =======================");
            PrintGraph(rootNode, 0);

            FlattenedTree = new List<BinaryNode>();
            ConvertGraphToList(rootNode, FlattenedTree, 0);

            Console.WriteLine($"======================= PRINTING FLATTENED CUSTOM DECISION TREE =======================");
            PrintBinaryNodes(FlattenedTree, 0);

            return FlattenedTree;
        }

        public AkDecisionTree(ByteChunk chunk, uint maxTreeDepth, uint uTreeDataSize)
        {
            // Produce initial flattenedTree
            FlattenedTree = new List<BinaryNode>();
            var numNodes = uTreeDataSize / BinaryNode.SerializationByteSize;

            foreach (var item in Enumerable.Range(0, (int)numNodes))
                FlattenedTree.Add(new BinaryNode(chunk));

            // Convert flattenedTree into a graph
            Root = ConvertListToGraph(FlattenedTree, maxTreeDepth, 0, 0, 0);

            // Sort nodes into order by ID at each depth level
            SortNodes(Root);

            // Update / verify all child ID and Count data in nodes
            ushort childrenId = 1;
            UpdateChildrenData(Root, ref childrenId);

            //Console.WriteLine($"======================= PRINTING FLATTENEDTREE WITH DEPTH {maxTreeDepth} =======================");
            //PrintBinaryNodes(flattenedTree, 0);
            //Console.WriteLine($"======================= PRINTING GRAPH WITH DEPTH {maxTreeDepth} =======================");
            //PrintGraph(Root, 0);
        }

        public static byte[] GetAsBytes()
        {
            using var memStream = new MemoryStream();
            foreach (var binaryNode in FlattenedTree)
            {
                //Console.WriteLine($"Writing node: {binaryNode.Key}");
                memStream.Write(ByteParsers.UInt32.EncodeValue(binaryNode.Key ?? 0, out _), 0, 4);

                if (binaryNode.AudioNodeId != 0)
                {
                    // Write AudioNodeId if not empty
                    memStream.Write(ByteParsers.UInt32.EncodeValue(binaryNode.AudioNodeId, out _), 0, 4);
                }
                else
                {
                    // Write Children_uIdx and Children_uCount if AudioNodeId is empty
                    memStream.Write(ByteParsers.UShort.EncodeValue(binaryNode.Children_uIdx, out _), 0, 2);
                    memStream.Write(ByteParsers.UShort.EncodeValue(binaryNode.Children_uCount, out _), 0, 2);
                }

                memStream.Write(ByteParsers.UShort.EncodeValue(binaryNode.UWeight, out _), 0, 2);
                memStream.Write(ByteParsers.UShort.EncodeValue(binaryNode.UProbability, out _), 0, 2);
            }

            return memStream.ToArray();
        }

        public static byte[] GetAsBytes(List<BinaryNode> flattenedTree)
        {
            using var memStream = new MemoryStream();
            foreach (var binaryNode in flattenedTree)
            {
                // Write binary node properties to memory stream
                memStream.Write(ByteParsers.UInt32.EncodeValue(binaryNode.Key ?? 0, out _), 0, 4);

                if (binaryNode.AudioNodeId != 0)
                {
                    memStream.Write(ByteParsers.UInt32.EncodeValue(binaryNode.AudioNodeId, out _), 0, 4);
                }
                else
                {
                    memStream.Write(ByteParsers.UShort.EncodeValue(binaryNode.Children_uIdx, out _), 0, 2);
                    memStream.Write(ByteParsers.UShort.EncodeValue(binaryNode.Children_uCount, out _), 0, 2);
                }

                memStream.Write(ByteParsers.UShort.EncodeValue(binaryNode.UWeight, out _), 0, 2);
                memStream.Write(ByteParsers.UShort.EncodeValue(binaryNode.UProbability, out _), 0, 2);
            }

            return memStream.ToArray();
        }
    }
}
