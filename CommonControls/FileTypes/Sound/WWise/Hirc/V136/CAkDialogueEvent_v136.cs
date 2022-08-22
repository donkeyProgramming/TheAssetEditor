using CommonControls.FileTypes.Sound.WWise;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, Size);
            AkPropBundle0 = AkPropBundle.Create(chunk);
            AkPropBundle1 = AkPropBundleMinMax.Create(chunk);
        }

        public override void ComputeSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }


    public class AkDecisionTree
    {
        [DebuggerDisplay("Node Key:[{key}] Children:[{Children.Count}] Sounds:[{SoundNodes.Count}]")]
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
            //public List<SoundNode> SoundNodes { get; set; } = new List<SoundNode>();

            public Node()
            {
            }

            public void Parse(ByteChunk chunk, uint parentCount, uint size, uint currentTreeDepth, uint maxTreeDepth)
            {
                for (uint i = 0; i < parentCount; i++)
                {
                    var key_ = chunk.ReadUInt32();

                    //is_id is a test for checking if the next bytes form an audioNodeId rather than uIdx+uCount
                    //and that's used to know if the node is a branch or leaf
                    //this is needed because there are some leafs that are not at "maxTreeDepth", it's very very few of them
                    //looks like its if either the peaked uIdx or uCount are larger than the number of bytes in the block, then it's an audioNodeId
                    //and that makes sense because it's like referring to an amount of chairs that is more than the number of atoms in the universe, nonsense
                    var peak = chunk.PeakUint32();
                    var uidx = peak >> 0 & 0xFFFF;
                    var ucnt = peak >> 16 & 0xFFFF;
                    var count_max = size;// chunk.BytesLeft;
                    var is_id = (uidx > count_max || ucnt > count_max);

                    var is_max = currentTreeDepth == maxTreeDepth;

                    if (is_max || is_id)
                    {
                        var node = new Node()
                        {
                            Key = key_,
                            AudioNodeId = chunk.ReadUInt32(),
                            uWeight = chunk.ReadUShort(),
                            uProbability = chunk.ReadUShort(),
                            IsAudioNode = true,
                        };
                        Children.Add(node);
                    }
                    else
                    {
                        var node = new Node()
                        {
                            Key = key_,
                            Children_uIdx = chunk.ReadUShort(),
                            Children_uCount = chunk.ReadUShort(),
                            uWeight = chunk.ReadUShort(),
                            uProbability = chunk.ReadUShort(),
                            IsAudioNode = false,
                        };
                        Children.Add(node);
                    }
                    
                }

                foreach (var child in Children)
                {
                    if (child.Children_uCount > 0)
                        child.Parse(chunk, child.Children_uCount, size, currentTreeDepth + 1, maxTreeDepth);
                }
            }
        }

        //This could probably be added back in (no idea how to do that)
        //public class SoundNode 
        //{
        //    public uint key;
        //    public uint audioNodeId;
        //    public ushort uWeight;
        //    public ushort uProbability;
        //
        //    public SoundNode(ByteChunk chunk)
        //    {
        //        key = chunk.ReadUInt32();
        //        audioNodeId = chunk.ReadUInt32();
        //        uWeight = chunk.ReadUShort();
        //        uProbability = chunk.ReadUShort();
        //    }
        //}

        public Node Root { get; set; }

        public AkDecisionTree(ByteChunk chunk, uint maxTreeDepth, uint size)
        {
            Root = new Node();
            Root.Parse(chunk, 1, size, 0, maxTreeDepth); //first Node is at depth 0
        }
    }

    public class ArgumentList
    {
        public List<Argument> Arguments { get; set; } = new List<Argument>();
        public ArgumentList(ByteChunk chunk, uint numItems)
        {
            for (uint i = 0; i < numItems; i++)
                Arguments.Add(new Argument(chunk));
        }

        public class Argument
        {
            public uint ulGroup { get; set; }
            public AkGroupType eGroupType { get; set; }
            public Argument(ByteChunk chunk)
            {
                ulGroup = chunk.ReadUInt32();
                eGroupType = (AkGroupType)chunk.ReadByte();
            }
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

            if (output.StreamType != SourceType.Straming)
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
