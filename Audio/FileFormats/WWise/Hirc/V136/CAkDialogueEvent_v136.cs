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
            
            // Some Nodes at the _maxDepth have AudioNodeId == 0 and no children so we cannot use AudioNodeId = 0 to check
            // so we should check for children instead - works for now
            public abstract bool IsAudioNode(); 
            public uint AudioNodeId { get; set; }
            public ushort uWeight { get; set; }
            public ushort uProbability { get; set; }
        }
        public class SerializedNode: BaseNode
        {
            public ushort Children_uIdx { get; set; }
            public ushort Children_uCount { get; set; }
            public override bool IsAudioNode() => Children_uCount == 0;
            
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
                if (IsAudioNode())
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
                    if (IsAudioNode())
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
                if (IsAudioNode()){
                    If(Children_uIdx > 0).Then(_ =>
                        throw new ArgumentException($"AudioNode has invalid Children_uIdx: {Children_uIdx}. Should be 0"));
                    If(Children_uCount > 0).Then(_ =>
                        throw new ArgumentException($"AudioNode has invalid Children_uCount: {Children_uCount}. Should be 0"));
                }
                else{
                    If(Children_uCount == 0).Then(_ =>
                        throw new ArgumentException($"LogicNode has invalid Children_uCount: {Children_uCount}. Should be greater 0"));
                }
            }
        }
        
        [DebuggerDisplay("Node Key:[{Key}] Children:[{Children.Count}]")]
        public class Node: BaseNode
        {
            public override bool IsAudioNode() => Children.Count == 0;
            public List<Node> Children { get; set; } = new List<Node>();

            private Node(uint key, uint audioNodeId, ushort uweight, ushort uprobability)
            {
                If(uProbability > 100).Then(_ => 
                    throw new ArgumentException($"uProbability ({uProbability}) is greater than 100"));
                
                Key = key;
                AudioNodeId = audioNodeId;
                uWeight = uweight;
                uProbability = uprobability;
            }
            
            public Node(SerializedNode sNode)
            {
                Key = sNode.Key;
                AudioNodeId = sNode.AudioNodeId;
                uWeight = sNode.uWeight;
                uProbability = sNode.uProbability;
            }
            
            public void VerifyState()
            {
                If(IsAudioNode())
                    .Then(_ => 
                            Debug.Assert(Children.Count == 0))
                    .Else(_ =>
                        {
                            Debug.Assert(AudioNodeId == 0);
                            Debug.Assert(Children.Count > 0);
                        }
                    );
            }

            public static Node CreateDecisionNode(uint key, ushort uWeight, ushort uProbability) => 
                new Node(key,  0, uWeight, uProbability);
            
            public static Node CreateAudioNode(uint key, ushort uWeight, ushort uProbability, uint audioNodeId) =>
                new Node(key, audioNodeId, uWeight, uProbability);
        }

        // It's immutable. _maxTreeDepth equals to the actual depth of three. AudioNode should be at this level.
        //But it's not always true. CA uses some kind of 'optimization' and AudioNode might be on the same level as DecisionNodes...
        public readonly uint _maxTreeDepth;
        public Node Root { get; set; }
        
        public AkDecisionTree(ByteChunk chunk, uint maxTreeDepth, uint uTreeDataSize)
        {
            _maxTreeDepth = maxTreeDepth;
            var numNodes = uTreeDataSize / SerializedNode.SerializationByteSize;
            var flattenTree = new List<SerializedNode>();
            For(numNodes, _ => 
                    flattenTree.Add(new SerializedNode(chunk)));
            
            Node ConvertNode(ushort parentsFirstChildIndex, ushort childIndex, uint currentDepth)
            {
                var sNode = flattenTree[parentsFirstChildIndex + childIndex];
                var isAtMaxDepth = currentDepth == maxTreeDepth;
                var isOutsideRange = sNode.Children_uIdx >= flattenTree.Count;
                if(isAtMaxDepth || isOutsideRange){
                    sNode.Children_uCount = 0;
                    sNode.Children_uIdx = 0;
                    return new Node(sNode);
                }
                sNode.AudioNodeId = 0;
                var node = new Node(sNode);
                For(sNode.Children_uCount, i => 
                        node.Children.Add(ConvertNode(sNode.Children_uIdx, (ushort) i,currentDepth + 1)));
                return node;
            }

            Root = ConvertNode(0, 0, 0);
            #if DEBUG
                flattenTree.ForEach(e => e.VerifyState());
            #endif
        }


        public Node AddAudioNode(
            List<(uint key, ushort weight, ushort probability)> decisionNodes, 
            (uint key, ushort audioId, ushort weight, ushort probability) audioNode)
        {
            If(decisionNodes.Count + 1 + 1 > _maxTreeDepth).Then( _ => // 1 for the root and 1 for a leaf
                throw new ArgumentException($"DecisionPathChain is too Long"));

            var cNode = Root;
            decisionNodes.ForEach(e =>
            {
                var selected = cNode.Children.Where(x => x.Key == e.key);
                
                If(selected.Count() > 1).Then(_ =>
                    throw new ArgumentException($"Many nodes were selected"));

                if(!selected.Any()){
                    cNode = Node.CreateDecisionNode(e.key, e.weight, e.probability);
                    cNode.Children.Add(cNode);
                    return;
                }

                cNode = selected.First();
            });
            
            var aNode = Node.CreateAudioNode(audioNode.key,  audioNode.weight, audioNode.probability, audioNode.audioId);
            cNode.Children.Add(aNode);
            return cNode.Children.Last();
        }


        public List<Node> GetAudioNodes()
        {
            var audioNodes = new List<Node>();
            void GetAudioNode(Node node)
            {
                If(node.IsAudioNode()).Then(_ =>
                    audioNodes.Add(node));
            }

            DfsTreeTraversal(GetAudioNode);
            return audioNodes;
        }
        public List<List<Node>> GetDecisionPaths()
        {
            var decisionPaths = new List<List<Node>>();
            
            var stack = new Stack<Node>();
            void GetDecisionPathsInternal()
            {
                while (stack.Count > 0){

                    var peek = stack.Peek();
                    If(peek.IsAudioNode()).Then(_ =>
                        decisionPaths.Add(stack.ToList()));
                    var node = stack.Pop();
                    node.Children.ForEachReverse(stack.Push);
                }
            }
            
            stack.Push(Root);
            GetDecisionPathsInternal();
            return decisionPaths;
        }

        public int NodeCount()
        {
            int count = 0;
            BfsTreeTraversal(_ => count += 1);
            return count;
        }

        public int Depth()
        {
            int depth = 0;
            DfsTreeTraversal((_, d) => depth = (d > depth) ? d : depth);
            return depth;
        }
        public void VerifyState()
        {
            void Verify(Node node)
            {
                node.VerifyState();
                if (!node.IsAudioNode()){ 
                    if (!node.Children.First().IsAudioNode()){
                        //Debug.Assert(node.Children.First().Key == 0); // Not TRUE: the first children of logicalNodes has key == 0
                    }
                }
            }
            BfsTreeTraversal(Verify);
            
            //False: Leaves are at maxDepth
            // BfsTreeTraversal((node, d) =>
            // {
            //     if (node.Children.Count == 0){
            //         Debug.Assert(d == _maxTreeDepth);
            //     }
            //                      
            // });
            
        }
        
        public void BfsTreeTraversal(Action<Node, int, int> func)
        {
            void TreeTraversalInternal(Queue<(Node, int, int)> queue)
            {
                while (queue.Count > 0)
                {
                    var (node, depth, childIdx) = queue.Dequeue();
                    func(node, depth, childIdx);
                    node.Children.ForEach((e, i) => queue.Enqueue((e, depth+1, i)));
                }
            }

            var queue = new Queue<(Node, int, int)>();
            queue.Enqueue((Root, 0, 0));
            TreeTraversalInternal(queue);
        }
        
        public void BfsTreeTraversal(Action<Node, int> func) =>
            BfsTreeTraversal((node, depth, childIdx) => func(node, depth));
        public void BfsTreeTraversal(Action<Node> func) =>
            BfsTreeTraversal((node, depth, childIdx) => func(node));
        
        public void DfsTreeTraversal(Action<Node, int, int> func)
        {
            void TreeTraversalInternal(Stack<(Node, int, int)> stack)
            {
                while (stack.Count > 0)
                {
                    var (node, depth, childIdx) = stack.Pop();
                    func(node, depth, childIdx);
                    node.Children.ForEach((e, i) => stack.Push((e, depth+1, i)));
                }
            }

            var stack = new Stack<(Node, int, int)>();
            stack.Push((Root, 0, 0));
            TreeTraversalInternal(stack);
        }
        
        public void DfsTreeTraversal(Action<Node, int> func) =>
            DfsTreeTraversal((node, depth, childIdx) => func(node, depth));
        public void DfsTreeTraversal(Action<Node> func) =>
            DfsTreeTraversal((node, depth, childIdx) => func(node));
        
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
                If(!visitedNodes.Contains(node)).Then(_ =>
                    VisitAndDo(node));
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
            For(numItems, _ 
                    => Arguments.Add(new Argument()));
            For((int) numItems, i 
                    => Arguments[i].ulGroupId = chunk.ReadUInt32());
            For((int) numItems, i 
                    => Arguments[i].eGroupType = (AkGroupType)chunk.ReadByte());
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
                For(Arguments.Count, i =>
                    {
                        Debug.Assert(Arguments[i].ulGroupId == copyInstance.Arguments[i].ulGroupId);
                        Debug.Assert(Arguments[i].eGroupType == copyInstance.Arguments[i].eGroupType);
                    });
            #endif
            
            return byteArray;
        }
    }

}
