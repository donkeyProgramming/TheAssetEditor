using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Audio.FileFormats.WWise.Hirc.Shared
{
    public class AkDecisionTree
    {
        public class BinaryNode
        {
            public uint Key { get; set; }
            public uint AudioNodeId { get; set; }
            public ushort Children_uIdx { get; set; }
            public ushort Children_uCount { get; set; }

            public ushort uWeight { get; set; }
            public ushort uProbability { get; set; }


            public static readonly int SerializationByteSize = 12;

            public BinaryNode(ByteChunk chunk)
            {
                Key = chunk.ReadUInt32();
                AudioNodeId = chunk.PeakUint32();
                Children_uIdx = chunk.ReadUShort();
                Children_uCount = chunk.ReadUShort();
                uWeight = chunk.ReadUShort();
                uProbability = chunk.ReadUShort();
            }
        }

        [DebuggerDisplay("Node Key:[{Key}] Children:[{Children.Count}]")]
        public class Node
        {
            // Some Nodes at the _maxDepth have AudioNodeId == 0 and no children so we cannot use AudioNodeId = 0 to check
            // so we should check for children instead - works for now
            public bool IsAudioNode() => Children.Count == 0;

            public uint Key { get; set; }
            public uint AudioNodeId { get; set; }
            public ushort Children_uIdx { get; set; }
            public ushort Children_uCount { get; set; }

            public ushort uWeight { get; set; }
            public ushort uProbability { get; set; }

            public List<Node> Children { get; set; } = new List<Node>();

            public Node(BinaryNode sNode)
            {
                Key = sNode.Key;
                AudioNodeId = sNode.AudioNodeId;
                Children_uIdx = sNode.Children_uIdx;
                Children_uCount = sNode.Children_uCount;
                uWeight = sNode.uWeight;
                uProbability = sNode.uProbability;
            }
        }


        // It's immutable. _maxTreeDepth equals to the actual depth of three. AudioNode should be at this level.
        //But it's not always true. CA uses some kind of 'optimization' and AudioNode might be on the same level as DecisionNodes...
        public readonly uint _maxTreeDepth;
        public Node Root { get; set; }

        public AkDecisionTree(ByteChunk chunk, uint maxTreeDepth, uint uTreeDataSize)
        {
            _maxTreeDepth = maxTreeDepth;
            var numNodes = uTreeDataSize / BinaryNode.SerializationByteSize;
            var flattenTree = new List<BinaryNode>();
            foreach (var item in Enumerable.Range(0, (int)numNodes))
                flattenTree.Add(new BinaryNode(chunk));

            Root = ConvertListToGraph(flattenTree, _maxTreeDepth, 0, 0, 0);
        }

        Node ConvertListToGraph(List<BinaryNode> flattenTree, uint maxTreeDepth, ushort parentsFirstChildIndex, ushort childIndex, uint currentDepth)
        {
            var sNode = flattenTree[parentsFirstChildIndex + childIndex];
            var isAtMaxDepth = currentDepth == maxTreeDepth;
            var isOutsideRange = sNode.Children_uIdx >= flattenTree.Count;

            if (isAtMaxDepth || isOutsideRange)
            {
                sNode.Children_uCount = 0;
                sNode.Children_uIdx = 0;
                return new Node(sNode); // Audio node
            }
            else
            {
                sNode.AudioNodeId = 0;
                var pathNode = new Node(sNode);
                for (int i = 0; i < sNode.Children_uCount; i++)
                {
                    var childNode = ConvertListToGraph(flattenTree, maxTreeDepth, sNode.Children_uIdx, (ushort)i, currentDepth + 1);
                    pathNode.Children.Add(childNode);
                }
                return pathNode;
            }
        }

        public byte[] GetAsBytes()
        {
            throw new NotImplementedException();
            //using var memStream = new MemoryStream();
            //var flattenTree = Flatten();
            //flattenTree.ForEach(e => memStream.Write(e.GetAsBytes()));
            //var byteArray = memStream.ToArray();
            //return byteArray;
        }
    }




    /*
      public class AkBankSourceData
    {
        public uint PluginId { get; set; }
        public ushort PluginId_type { get; set; }
        public ushort PluginId_company { get; set; }
        public SourceType StreamType { get; set; }

        public AkMediaInformation akMediaInformation { get; set; }
        public uint uSize { get; set; }
        public static AkBankSourceData Create(ByteChunk chunk)
        {
            var output = new AkBankSourceData()
            {
                PluginId = chunk.ReadUInt32(),
                //PluginId_type = chunk.ReadUShort(),
                //PluginId_company = chunk.ReadUShort(),
                StreamType = (SourceType)chunk.ReadByte()
            };

         
            output.PluginId_type = (ushort)((output.PluginId >> 0) & 0x000F);
            output.PluginId_company = (ushort)((output.PluginId >> 4) & 0x03FF);

            if (output.StreamType != SourceType.Streaming)
            {
             //   throw new Exception();
            }

            if (output.PluginId_type == 0x02)
                output.uSize = chunk.ReadUInt32();

            output.akMediaInformation = AkMediaInformation.Create(chunk);

            return output;
        }
    }
     */
}
