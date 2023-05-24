using Filetypes.ByteParsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommonControls.Common;
using static CommonControls.Common.CustomExtensions;
using MoreLinq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace Audio.FileFormats.WWise.Hirc.V136
{
    public class CAkDialogueEvent_v136 : HircItem
    {
        public byte uProbability { get; set; }
        public uint uTreeDepth { get; set; }
        public ArgumentList ArgumentList { get; set; }
        public uint uTreeDataSize { get; set; }
        public byte uMode { get; set; }
        public AkDecisionTree AkDecisionTree { get; set; }
        public AkPropBundle AkPropBundle0 { get; set; }
        public AkPropBundleMinMax AkPropBundle1 { get; set; }

        protected override void CreateSpesificData(ByteChunk chunk)
        {
            uProbability = chunk.ReadByte();
            uTreeDepth = chunk.ReadUInt32();
            ArgumentList = new ArgumentList(chunk, uTreeDepth);
            uTreeDataSize = chunk.ReadUInt32();
            uMode = chunk.ReadByte();

            AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, uTreeDataSize);
          
            AkPropBundle0 = AkPropBundle.Create(chunk);
            AkPropBundle1 = AkPropBundleMinMax.Create(chunk);
        }
        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(ByteParsers.Byte.EncodeValue(uProbability, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(uTreeDepth, out _));
            memStream.Write(ArgumentList.GetAsBytes());
            memStream.Write(ByteParsers.UInt32.EncodeValue(uTreeDataSize, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(uMode, out _));
            memStream.Write(AkDecisionTree.GetAsBytes());
            memStream.Write(AkPropBundle0.GetAsBytes());
            memStream.Write(AkPropBundle1.GetAsBytes());
            var byteArray = memStream.ToArray();
            return byteArray;
        }
    }


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
            Enumerable.Range(0, (int)numNodes).ForEach(i=>flattenTree.Add(new BinaryNode(chunk)));

            Node ConvertNode(ushort parentsFirstChildIndex, ushort childIndex, uint currentDepth)
            {
                var sNode = flattenTree[parentsFirstChildIndex + childIndex];
                var isAtMaxDepth = currentDepth == maxTreeDepth;
                var isOutsideRange = sNode.Children_uIdx >= flattenTree.Count;
                if( isAtMaxDepth || isOutsideRange)
                {
                    sNode.Children_uCount = 0;
                    sNode.Children_uIdx = 0;
                    return new Node(sNode);
                }

                sNode.AudioNodeId = 0;
                var node = new Node(sNode);
                Enumerable.Range(0, sNode.Children_uCount).ForEach(i=>node.Children.Add(ConvertNode(sNode.Children_uIdx, (ushort)i, currentDepth + 1)));
                return node;
            }

            Root = ConvertNode(0, 0, 0);
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

    public class ArgumentList
    {
        public List<Argument> Arguments { get; set; } = new List<Argument>();
        public ArgumentList(ByteChunk chunk, uint numItems)
        {
            for (uint i = 0; i < numItems; i++)
                Arguments.Add(new Argument());

            for (int i = 0; i < numItems; i++)
                Arguments[i].ulGroupId = chunk.ReadUInt32();

            for (int i = 0; i < numItems; i++)
                Arguments[i].eGroupType = (AkGroupType)chunk.ReadByte();
        }

        public class Argument
        {
            public uint ulGroupId { get; set; }
            public AkGroupType eGroupType { get; set; }
        }

        public byte[] GetAsBytes()
        {
            using var memStream = new MemoryStream();
            Arguments.ForEach(e => memStream.Write(ByteParsers.UInt32.EncodeValue(e.ulGroupId, out _)));
            Arguments.ForEach(e => memStream.Write(ByteParsers.Byte.EncodeValue((byte)e.eGroupType, out _)));
            var byteArray = memStream.ToArray();
            return byteArray;
        }
    }
}
