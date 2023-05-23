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
        public class NodeContent
        {
            public uint Key { get; set; }
            public ushort uWeight { get; set; }
            public ushort uProbability { get; set; }

            public NodeContent(uint key)
            {
                Key = key;
                uWeight = 50;
                uProbability = 100;
            }
            
            public NodeContent(uint key, ushort uweight, ushort uprobability)
            {
                Key = key;
                uWeight = uweight;
                uProbability = uprobability;
            }
        }

        public abstract class BaseNode
        {
            public NodeContent Content { get; set; } = new NodeContent(0,0,0);
            
            // Some Nodes at the _maxDepth have AudioNodeId == 0 and no children so we cannot use AudioNodeId = 0 to check
            // so we should check for children instead - works for now
            public abstract bool IsAudioNode(); 
            public uint AudioNodeId { get; set; }
        }

        public class SerializedNode: BaseNode
        {
            public ushort Children_uIdx { get; set; }
            public ushort Children_uCount { get; set; }
            public override bool IsAudioNode() => Children_uCount == 0;
            
            public static readonly int SerializationByteSize = 12;

            public SerializedNode(Node node)
            {
                Content = node.Content;
                AudioNodeId = node.AudioNodeId;
                Children_uCount = (ushort) node.Children.Count;
                Children_uIdx = 0;
            }
            
            public SerializedNode(ByteChunk chunk)
            {
                Content.Key = chunk.ReadUInt32();
                AudioNodeId = chunk.PeakUint32();
                Children_uIdx = chunk.ReadUShort();
                Children_uCount = chunk.ReadUShort();
                Content.uWeight = chunk.ReadUShort();
                Content.uProbability = chunk.ReadUShort();
            }
            
            public byte[] GetAsBytes()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(Content.Key, out _));
                if (IsAudioNode())
                {
                    memStream.Write(ByteParsers.UInt32.EncodeValue(AudioNodeId, out _));
                } 
                else
                {
                    memStream.Write(ByteParsers.UShort.EncodeValue(Children_uIdx, out _));
                    memStream.Write(ByteParsers.UShort.EncodeValue(Children_uCount, out _));
                }
                memStream.Write(ByteParsers.UShort.EncodeValue(Content.uWeight, out _));
                memStream.Write(ByteParsers.UShort.EncodeValue(Content.uProbability, out _));
                var byteArray = memStream.ToArray();
                return byteArray;
            }

            public void VerifyState()
            {
                if (IsAudioNode()){
                    if (Children_uIdx > 0)
                        throw new ArgumentException($"AudioNode has invalid Children_uIdx: {Children_uIdx}. Should be 0");
                    if (Children_uCount > 0)
                        throw new ArgumentException($"AudioNode has invalid Children_uCount: {Children_uCount}. Should be 0");
                    if (Content.uWeight != 50 && Content.uProbability != 100)
                        throw new ArgumentException($"AudioNode can only have uWeight or uProbability modified");
                }
                else
                {
                    if (Children_uCount == 0)
                        throw new ArgumentException($"LogicNode has invalid Children_uCount: {Children_uCount}. Should be greater 0");
                    if (Content.uWeight != 50)
                        throw new ArgumentException($"LogicNode should have uWeight{Content.uWeight} equal to 50");
                    if (Content.uProbability != 100)
                        throw new ArgumentException($"LogicNode should have uProbability{Content.uProbability} equal to 100");
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
                If(Content.uProbability > 100).Then(_ => 
                    throw new ArgumentException($"uProbability ({Content.uProbability}) is greater than 100"));
                
                Content.Key = key;
                AudioNodeId = audioNodeId;
                Content.uWeight = uweight;
                Content.uProbability = uprobability;
            }
            
            public Node(SerializedNode sNode)
            {
                Content = sNode.Content;
                AudioNodeId = sNode.AudioNodeId;
            }

            public Node Copy()
            {
                var copy = new Node(Content.Key, AudioNodeId, Content.uWeight, Content.uProbability);
                return copy;
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
                            Debug.Assert(Content.uWeight == 50);
                            Debug.Assert(Content.uProbability == 100);
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

        private AkDecisionTree(uint maxTreeDepth)
        {
            _maxTreeDepth = maxTreeDepth;
            Root = null;
        }
        
        public AkDecisionTree(ByteChunk chunk, uint maxTreeDepth, uint uTreeDataSize)
        {
            _maxTreeDepth = maxTreeDepth;
            var numNodes = uTreeDataSize / SerializedNode.SerializationByteSize;
            var flattenTree = new List<SerializedNode>();
            For(numNodes, _ => flattenTree.Add(new SerializedNode(chunk)));
            
            Node ConvertNode(ushort parentsFirstChildIndex, ushort childIndex, uint currentDepth)
            {
                var sNode = flattenTree[parentsFirstChildIndex + childIndex];
                var isAtMaxDepth = currentDepth == maxTreeDepth;
                var isOutsideRange = sNode.Children_uIdx >= flattenTree.Count;
                if(isAtMaxDepth || isOutsideRange)
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

        // Returns a copy of the tree with a root only
        public AkDecisionTree BaseCopy()
        {
            var copy = new AkDecisionTree(_maxTreeDepth);
            copy.Root = Root.Copy(); // No children are copied
            return copy;
        }


        public Node AddAudioNode(List<NodeContent> nodes, uint audioNodeId)
        {
            if(nodes.Count != _maxTreeDepth)// the root is not counted
                throw new ArgumentException($"DecisionPathChain is too Long or too short");

            var cNode = Root;
            nodes.GetRange(0, nodes.Count - 1).ForEach(e =>
            {
                var selected = cNode.Children.Where(x => x.Content.Key == e.Key);
                
                if(selected.Count() > 1)
                    throw new ArgumentException($"Many nodes were selected");

                if(!selected.Any()){
                    var node = Node.CreateDecisionNode(e.Key, e.uWeight, e.uProbability);
                    cNode.Children.Add(node);
                    cNode = node;
                    return;
                }

                cNode = selected.First();
            });
            
            // Add audio Node
            var aNode = nodes.Last();
            var selected = cNode.Children.Where(x => x.Content.Key == aNode.Key);
            if (selected.Any())
                throw new ArgumentException($"AudioNode with a key ({aNode.Key}) already exists."); //TODO: it will print the hash of the key. Should be more explicit
            var audioNode = Node.CreateAudioNode(aNode.Key,  aNode.uWeight, aNode.uProbability, audioNodeId);
            cNode.Children.Add(audioNode);
            return cNode.Children.Last();
        }

        public List<(NodeContent[], uint)> GetDecisionPaths()
        {
            var decisionPaths = new List<(NodeContent[], uint)>();
            
            var stack = new Stack<Node>();
            
            
            stack.Push(Root);
            GetDecisionPathsInternal(stack, decisionPaths);
            stack.Pop();
            return decisionPaths;
        }

        void GetDecisionPathsInternal(Stack<Node> stack, List<(NodeContent[], uint)> decisionPaths)
        {
            var peek = stack.Peek();
            If(peek.IsAudioNode()).Then(_ =>
                decisionPaths.Add((stack.Select(e => e.Content).Reverse().ToArray(), peek.AudioNodeId)));

            peek.Children.ForEach(e =>
            {
                stack.Push(e);
                GetDecisionPathsInternal(stack, decisionPaths);
                stack.Pop();
            });
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
        public void VerifyState() => BfsTreeTraversal(VerifyState_Verify);

        void VerifyState_Verify(Node node)
        {
            node.VerifyState();
            if (!node.IsAudioNode())
            {
                if (!node.Children.First().IsAudioNode())
                {
                    //Debug.Assert(node.Children.First().Key == 0); // Not TRUE: the first children of logicalNodes has key == 0
                }
            }
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
        
        public void BfsTreeTraversal(Action<Node, int> func) =>  BfsTreeTraversal((node, depth, childIdx) => func(node, depth));
        public void BfsTreeTraversal(Action<Node> func) => BfsTreeTraversal((node, depth, childIdx) => func(node));
        
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
        
        public void DfsTreeTraversal(Action<Node, int> func) =>  DfsTreeTraversal((node, depth, childIdx) => func(node, depth));
        
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
                if (node.Children.Count == 0)
                    return;
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
            return byteArray;
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
