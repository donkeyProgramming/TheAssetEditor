using CommonControls.FileTypes.Sound.WWise;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CommonControls.FileTypes.Sound.WWise.Hirc.V136
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
            AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, Size, uTreeDataSize);
            AkPropBundle0 = AkPropBundle.Create(chunk);
            AkPropBundle1 = AkPropBundleMinMax.Create(chunk);
        }

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }


    public class AkDecisionTree
    {
        [DebuggerDisplay("Node Key:[{Key}] Children:[{Children.Count}]")]
        public class Node
        {
            public uint Key { get; set; }
            public uint AudioNodeId { get; set; }
            public ushort Children_uIdx { get; set; }
            public ushort Children_uCount { get; set; }
            public ushort uWeight { get; set; }
            public ushort uProbability { get; set; }
            public bool IsAudioNode { get; set; }
            public List<Node> Children { get; set; } = new List<Node>();

            public Node()
            {
            }

            public void Parse(ByteChunk chunk, uint parentCount, uint size, uint currentTreeDepth, uint maxTreeDepth)
            {
                for (uint i = 0; i < parentCount; i++)
                {
                    var key = chunk.ReadUInt32();

                    //is_id is a test for checking if the next bytes form an audioNodeId rather than uIdx+uCount
                    //and that's used to know if the node is a branch or leaf
                    //this is needed because there are some leafs that are not at "maxTreeDepth", it's very very few of them
                    //looks like its if either the peaked uIdx or uCount are larger than the number of bytes in the block, then it's an audioNodeId
                    //and that makes sense because it's like referring to an amount of chairs that is more than the number of atoms in the universe, nonsense
                    var peak = chunk.PeakUint32();

                    var uidx = peak >> 0 & 0xFFFF;
                    var ucnt = peak >> 16 & 0xFFFF;
                    var count_max = size;

                    var is_id = (uidx > count_max || ucnt > count_max);
                    var is_max = currentTreeDepth == maxTreeDepth;
                    var node = new Node();
                    node.Key = key;
                    node.IsAudioNode = is_max || is_id;

                    if (node.IsAudioNode)
                    {
                        node.AudioNodeId = chunk.ReadUInt32();
                    }
                    else
                    {
                        node.Children_uIdx = chunk.ReadUShort();
                        node.Children_uCount = chunk.ReadUShort(); 
                    }

                    node.uWeight = chunk.ReadUShort();
                    node.uProbability = chunk.ReadUShort();
                    Children.Add(node);
                }

                foreach (var child in Children)
                {
                    if (child.Children_uCount > 0)
                        child.Parse(chunk, child.Children_uCount, size, currentTreeDepth + 1, maxTreeDepth);
                }
            }
        }

        public Node Root { get; set; }
        public Node RootNew { get; set; }
        public List<Node> NewApporach { get; set; } = new List<Node>();

        public AkDecisionTree(ByteChunk chunk, uint maxTreeDepth, uint size, uint uTreeDataSize)
        {
            var indexRec = chunk.Index;
            Root = new Node();
            Root.Parse(chunk, 1, size, 0, maxTreeDepth); //first Node is at depth 0

            chunk.Index = indexRec;
            
            var numNodes = uTreeDataSize / 12;
            for (int i = 0; i < numNodes; i++)
            {
                var node = new Node();
                node.Key = chunk.ReadUInt32();

                node.AudioNodeId = chunk.PeakUint32();
                node.Children_uIdx = chunk.ReadUShort();
                node.Children_uCount = chunk.ReadUShort();

                node.uWeight = chunk.ReadUShort();
                node.uProbability = chunk.ReadUShort();
                NewApporach.Add(node);
            }

            RootNew = NewApporach.First();
            ConvertToTree(RootNew, maxTreeDepth, 0);
        }

        void ConvertToTree(Node root, uint maxDepth, uint currentDepth)
        {
            var childCount = root.Children_uCount;
            var firstChildIndex = root.Children_uIdx;

            for (int i = 0; i < childCount; i++)
            {
                var isAtMaxDepth = maxDepth == currentDepth;
                var isOutsideRange = firstChildIndex + i >= NewApporach.Count;  // Can be replaced with key == 0???!?!?!?
                if (isOutsideRange && isAtMaxDepth == false)
                {
                    if (root.Key != 0)
                    {

                    }
                }
                else
                {
                    if (root.Key == 0)
                    { 
                    }
                }
                if (isAtMaxDepth || isOutsideRange)
                {
                    root.IsAudioNode = true;
                    root.Children_uCount = 0;
                    root.Children_uIdx = 0;
                }
                else
                {
                    root.AudioNodeId = 0;
                    var child = NewApporach[firstChildIndex + i];
                    root.Children.Add(child);
                    ConvertToTree(child, maxDepth, currentDepth + 1);
                }
            }
        }

        void ConvertTo2DArray()
        {
            Node[][] array;
        }

        public void UpdateSortingAndIndex()
        { 
            // Sort
            // Update index
            // Update count
        }
    }

    public class ArgumentList
    {
        public List<Argument> Arguments { get; set; } = new List<Argument>();
        public ArgumentList(ByteChunk chunk, uint numItems)
        {
            for (int i = 0; i < numItems; i++)
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
    }
    
}
