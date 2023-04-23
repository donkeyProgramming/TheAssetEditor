using Filetypes.ByteParsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MoreLinq;

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
            #if DEBUG
                var treebytes = chunk.PeakChunk((int) uTreeDataSize);
                AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, uTreeDataSize);
                var x = AkDecisionTree.GetAsBytes();
                if (!treebytes.Buffer.SequenceEqual(x))
                {
                    Console.WriteLine(OwnerFile);
                    Console.WriteLine(Id);
                }
                Debug.Assert(treebytes.Buffer.SequenceEqual(x));
            #else
                AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, uTreeDataSize);
            #endif
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

            #if DEBUG //Reparse
                var copyInstance = new CAkDialogueEvent_v136();
                copyInstance.Parse(new ByteChunk(byteArray));
                Debug.Assert(uProbability == copyInstance.uProbability);
                Debug.Assert(uTreeDepth == copyInstance.uTreeDepth);
                Debug.Assert(uTreeDataSize == copyInstance.uTreeDataSize);
                Debug.Assert(uMode == copyInstance.uMode);
            #endif

            return byteArray;
        }
    }


    public class AkDecisionTree
    {

        public abstract class BaseNode
        {
            public uint Key { get; set; }
            public bool IsAudioNode { get; set; }
            public uint AudioNodeId { get; set; }
            public ushort uWeight { get; set; }
            public ushort uProbability { get; set; }
        }
        public class SerializedNode: BaseNode
        {
            public ushort Children_uIdx { get; set; }
            public ushort Children_uCount { get; set; }
            
            private int _SerializationByteSize() => Marshal.SizeOf(Key) + 
                                                    Marshal.SizeOf(AudioNodeId) + // == Marshal.SizeOf(Children_uIdx) + Marshal.SizeOf(Children_uCount)
                                                    Marshal.SizeOf(uWeight) + 
                                                    Marshal.SizeOf(uProbability);
            
            public static readonly int SerializationByteSize = new SerializedNode()._SerializationByteSize();

            private SerializedNode()
            {
            }

            public SerializedNode(Node node)
            {
                Key = node.Key;
                IsAudioNode = node.IsAudioNode;
                AudioNodeId = node.AudioNodeId;
                uWeight = node.uWeight;
                uProbability = node.uProbability;
                Children_uCount = (ushort) node.Children.Count;
                Children_uIdx = 0;
            }
            
            public SerializedNode(ByteChunk chunk)
            {
                Key = chunk.ReadUInt32();
                AudioNodeId = chunk.PeakUint32();
                Children_uIdx = chunk.ReadUShort();
                Children_uCount = chunk.ReadUShort();
                uWeight = chunk.ReadUShort();
                uProbability = chunk.ReadUShort();
            }
            
            public byte[] GetAsBytes()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(Key, out _));
                if (IsAudioNode)
                {
                    memStream.Write(ByteParsers.UInt32.EncodeValue(AudioNodeId, out _));
                } else
                {
                    memStream.Write(ByteParsers.UShort.EncodeValue(Children_uIdx, out _));
                    memStream.Write(ByteParsers.UShort.EncodeValue(Children_uCount, out _));
                }
                memStream.Write(ByteParsers.UShort.EncodeValue(uWeight, out _));
                memStream.Write(ByteParsers.UShort.EncodeValue(uProbability, out _));
                var byteArray = memStream.ToArray();

                #if DEBUG //Reparse
                    var copyInstance = new SerializedNode(new ByteChunk(byteArray));
                    Debug.Assert(Key == copyInstance.Key);
                    if (IsAudioNode)
                    {
                        Debug.Assert(AudioNodeId == copyInstance.AudioNodeId);
                    } else
                    {
                        Debug.Assert(Children_uIdx == copyInstance.Children_uIdx);
                        Debug.Assert(Children_uCount == copyInstance.Children_uCount);
                    }
                    Debug.Assert(uWeight == copyInstance.uWeight);
                    Debug.Assert(uProbability == copyInstance.uProbability);
                #endif
                
                return byteArray;
            }

            public void VerifyState()
            {
                if (IsAudioNode){
                    if (Children_uIdx > 0){
                        throw new ArgumentException($"AudioNode has invalid Children_uIdx: {Children_uIdx}. Should be 0");
                    }
                    if (Children_uCount > 0){
                        throw new ArgumentException($"AudioNode has invalid Children_uCount: {Children_uCount}. Should be 0");
                    }
                }
                else{
                    if (Children_uCount == 0){
                        throw new ArgumentException($"LogicNode has invalid Children_uCount: {Children_uCount}. Should be greater 0");
                    }
                }
            }
        }
        
        [DebuggerDisplay("Node Key:[{Key}] Children:[{Children.Count}]")]
        public class Node: BaseNode
        {
            public List<Node> Children { get; set; } = new List<Node>();

            public Node(SerializedNode sNode)
            {
                Key = sNode.Key;
                IsAudioNode = sNode.IsAudioNode;
                AudioNodeId = sNode.AudioNodeId;
                uWeight = sNode.uWeight;
                uProbability = sNode.uProbability;
            }

        }

        // TODO: a hack for now. Not sure if it's a const.
        // Maybe we want to calculate it dynamically via methods or keep track of it on edit/remove node methods
        private readonly uint _maxTreeDepth;
        public Node Root { get; set; }
        
        public AkDecisionTree(ByteChunk chunk, uint maxTreeDepth, uint uTreeDataSize)
        {
            _maxTreeDepth = maxTreeDepth;
            var numNodes = uTreeDataSize / SerializedNode.SerializationByteSize;
            var flattenTree = new List<SerializedNode>();
            Enumerable.Range(0, (int) numNodes).ForEach(_ => flattenTree.Add(new SerializedNode(chunk)));
            
            Node ConvertNode(ushort parentsFirstChildIndex, ushort childIndex, uint currentDepth)
            {
                var sNode = flattenTree[parentsFirstChildIndex + childIndex];
                var isAtMaxDepth = currentDepth == maxTreeDepth;
                var isOutsideRange = sNode.Children_uIdx >= flattenTree.Count;
                if (isAtMaxDepth || isOutsideRange){
                    sNode.IsAudioNode = true;
                    sNode.Children_uCount = 0;
                    sNode.Children_uIdx = 0;
                    return new Node(sNode);
                }
                sNode.AudioNodeId = 0;
                var node = new Node(sNode);
                Enumerable.Range(0, sNode.Children_uCount).ForEach(i => node.Children.Add(
                    ConvertNode(sNode.Children_uIdx, (ushort) i,currentDepth + 1)));
                return node;
            }

            Root = ConvertNode(0, 0, 0);
            #if DEBUG
                flattenTree.ForEach(e => e.VerifyState());
            #endif
        }

        public void VerifyState()
        {
            void Verify(Node node)
            {
                if (node.IsAudioNode){ 
                    Debug.Assert(node.Children.Count == 0);// is leaf
                }
                else{
                    //LogicNode
                    Debug.Assert(node.Children.Count > 0); // is not leaf
                    if (!node.Children.First().IsAudioNode){
                        //Debug.Assert(node.Children.First().Key == 0); // Not TRUE: the first children of logicalNodes has key == 0
                    }
                }
            }
            BfsTreeTraversal(Verify);
        }

        public void BfsTreeTraversal(Action<Node> func)
        {
            void TreeTraversalInternal(Queue<Node> queue)
            {
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    func(node);
                    node.Children.ForEach(queue.Enqueue);
                }
            }

            var queue = new Queue<Node>();
            queue.Enqueue(Root);
            TreeTraversalInternal(queue);
        }
        
        public void DfsTreeTraversal(Action<Node> func)
        {
            void TreeTraversalInternal(Stack<Node> stack)
            {
                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    func(node);
                    node.Children.ForEach(stack.Push);
                }
            }

            var stack = new Stack<Node>();
            stack.Push(Root);
            TreeTraversalInternal(stack);
        }
        
        public void MixedTreeTraversal(Action<Node> func)
        {
            var visitedNodes = new HashSet<Node>();
            
            void VisitAndDo(Node node)
            {
                visitedNodes.Add(node);
                func(node);
            }
            
            void TreeTraversalInternal(Node node)
            {
                if(!visitedNodes.Contains(node)) {
                    VisitAndDo(node);
                }
                node.Children.ForEach(VisitAndDo);
                node.Children.ForEach(TreeTraversalInternal);
            }

            TreeTraversalInternal(Root);
        }

        void ConvertTo2DArray()
        {
            Node[][] array;
        }

        public List<SerializedNode> Flatten()
        {
            var flattenTree = new List<SerializedNode>();
            var idxHashMap = new Dictionary<Node, int>();

            void FlattenInternal(Node node)
            {
                flattenTree.Add(new SerializedNode(node));
                var idx = flattenTree.Count - 1;
                idxHashMap.Add(node, idx);
            }
            
            void UpdateIndex(Node node)
            {
                if (node.Children.Count == 0){
                    return;
                }
                var idxNode = idxHashMap[node];
                var sNode = flattenTree[idxNode];
                sNode.Children_uIdx = (ushort) idxHashMap[node.Children.First()];
            }

            MixedTreeTraversal(FlattenInternal);
            BfsTreeTraversal(UpdateIndex);
            return flattenTree;
        }


        public byte[] GetAsBytes()
        {
            using var memStream = new MemoryStream();
            var flattenTree = Flatten();
            flattenTree.ForEach(e => memStream.Write(e.GetAsBytes()));
            var byteArray = memStream.ToArray();
            #if DEBUG //Reparse
                var copyInstance = new AkDecisionTree(new ByteChunk(byteArray), _maxTreeDepth , 
                    (uint) (flattenTree.Count * SerializedNode.SerializationByteSize));
            #endif
            return byteArray;
        }
    }

    public class ArgumentList
    {
        public List<Argument> Arguments { get; set; } = new List<Argument>();
        public ArgumentList(ByteChunk chunk, uint numItems)
        {
            var range = Enumerable.Range(0, (int) numItems).ToList();
            range.ForEach(_ => Arguments.Add(new Argument()));
            range.ForEach(i => Arguments[i].ulGroupId = chunk.ReadUInt32());
            range.ForEach(i => Arguments[i].eGroupType = (AkGroupType)chunk.ReadByte());
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
            Arguments.ForEach(e => memStream.Write(ByteParsers.Byte.EncodeValue((byte) e.eGroupType, out _)));
            var byteArray = memStream.ToArray();

            #if DEBUG //Reparse
                var copyInstance = new ArgumentList(new ByteChunk(byteArray), (uint) Arguments.Count);
                Enumerable.Range(0, Arguments.Count).ForEach(i =>
                    {
                        Debug.Assert(Arguments[i].ulGroupId == copyInstance.Arguments[i].ulGroupId);
                        Debug.Assert(Arguments[i].eGroupType == copyInstance.Arguments[i].eGroupType);
                    });
            #endif
            
            return byteArray;
        }
    }

}
