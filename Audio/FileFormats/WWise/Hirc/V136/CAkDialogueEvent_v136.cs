using Filetypes.ByteParsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, uTreeDataSize, Size);
                var x = AkDecisionTree.GetAsBytes();
                if (!treebytes.Buffer.SequenceEqual(x))
                {
                    Console.WriteLine(OwnerFile);
                    Console.WriteLine(Id);
                }
                Debug.Assert(treebytes.Buffer.SequenceEqual(x));
            #else
                AkDecisionTree = new AkDecisionTree(chunk, uTreeDepth, uTreeDataSize, Size);
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
            private int _SerializationByteSize() => Marshal.SizeOf(Key) + 
                                                    Marshal.SizeOf(AudioNodeId) + // == Marshal.SizeOf(Children_uIdx) + Marshal.SizeOf(Children_uCount)
                                                    Marshal.SizeOf(uWeight) + 
                                                    Marshal.SizeOf(uProbability);
            
            public static readonly int SerializationByteSize = new Node()._SerializationByteSize();
            
            public Node()
            {
            }
            
            //TODO: How to make this constructor available only for outer class?
            public Node(ByteChunk chunk)
            {
                Key = chunk.ReadUInt32();

                AudioNodeId = chunk.PeakUint32();
                Children_uIdx = chunk.ReadUShort();
                Children_uCount = chunk.ReadUShort();

                uWeight = chunk.ReadUShort();
                uProbability = chunk.ReadUShort();
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
                    if (Children_uIdx == 0){
                        throw new ArgumentException($"LogicNode has invalid Children_uIdx: {Children_uIdx}. Should be greater 0");
                    }
                    if (Children_uCount == 0){
                        throw new ArgumentException($"LogicNode has invalid Children_uCount: {Children_uCount}. Should be greater 0");
                    }
                }
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
                    var copyInstance = new Node(new ByteChunk(byteArray));
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
        }

        public Node Root { get; set; }

        // TODO: a hack for now. Not sure if it's a const.
        // Maybe we want to calculate it dynamically via methods or keep track of it on edit/remove node methods
        private uint _maxTreeDepth;

        public AkDecisionTree(ByteChunk chunk, uint maxTreeDepth, uint uTreeDataSize, uint size)
        {
            _maxTreeDepth = maxTreeDepth;
            var numNodes = uTreeDataSize / Node.SerializationByteSize;
            List<Node> flattenTree = new List<Node>((int) numNodes);
            Enumerable.Range(0, (int) numNodes).ForEach(_ => flattenTree.Add(new Node(chunk)));

            Node rootNew = flattenTree.First();
            ConvertToTree(rootNew, maxTreeDepth, 0, flattenTree);
            Root = rootNew;
        }

        private static void ConvertToTree(Node node, uint maxDepth, uint currentDepth, IReadOnlyList<Node> flattenTree)
        {
            var childCount = node.Children_uCount;
            var firstChildIndex = node.Children_uIdx;

            for (int i = 0; i < childCount; i++)
            {
                var isAtMaxDepth = maxDepth == currentDepth;
                var isOutsideRange = firstChildIndex + i >= flattenTree.Count;  // Can be replaced with key == 0???!?!?!?
                /*if (isOutsideRange && isAtMaxDepth == false)
                {
                    if (node.Key != 0)
                    {

                    }
                }
                else
                {
                    if (node.Key == 0)
                    {
                    }
                }*/
                if (isAtMaxDepth || isOutsideRange)
                {
                    node.IsAudioNode = true;
                    node.Children_uCount = 0;
                    node.Children_uIdx = 0;
                }
                else
                {
                    node.AudioNodeId = 0;
                    var child = flattenTree[firstChildIndex + i];
                    node.Children.Add(child);
                    ConvertToTree(child, maxDepth, currentDepth + 1, flattenTree);
                }
            }
        }

        public void VerifyState()
        {
            void Verify(Node node)
            {
                node.VerifyState();
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
        
        public void UpdateSortingAndIndex()
        {
            // Sort
            // Update index
            // Update count
        }

        public List<Node> Flatten()
        {
            var flattenTree = new List<Node>();
            void FlattenInternal(Node node) => flattenTree.Add(node);
            MixedTreeTraversal(FlattenInternal);
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
                    (uint) (flattenTree.Count * Node.SerializationByteSize),0);
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
