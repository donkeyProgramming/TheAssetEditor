using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Navigation;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;

namespace Audio.Utility
{
    public class DecisionPathHelper
    {
        private readonly IAudioRepository _audioRepository;

        public class DecisionPathCollection
        {
            public DecisionPath Header { get; set; }
            public List<DecisionPath> Paths { get; set; } = new List<DecisionPath>();
        }

        public class DecisionPath
        {
            public List<DecisionPathItem> Items { get; set; } = new List<DecisionPathItem>();
            public uint ChildNodeId { get; set; }

            public string GetAsString(string separator = ".") => string.Join(separator, Items.Select(x => x.DisplayName));
        }

        public class DecisionPathItem
        {
            public uint Value { get; set; }
            public string DisplayName { get; set; }
        }

        public DecisionPathHelper(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public DecisionPathCollection GetDecisionPaths(CAkDialogueEvent_v136 dialogEvent) => GetDecisionPaths(dialogEvent.AkDecisionTree, dialogEvent.ArgumentList);
        public DecisionPathCollection GetDecisionPaths(CAkMusicSwitchCntr_v136 musicSwitch) => GetDecisionPaths(musicSwitch.AkDecisionTree, musicSwitch.ArgumentList);

        DecisionPathCollection GetDecisionPaths(AkDecisionTree decisionTree, ArgumentList argumentsList)
        {
            var paths = GetDecisionPaths(decisionTree);
            List<DecisionPath> decisionPath = new List<DecisionPath>();
            foreach (var path in paths)
            {
                DecisionPath currentPath = new DecisionPath() { ChildNodeId = path.Item2 };
                foreach (var item in path.Item1.Skip(1))
                {
                    var name = _audioRepository.GetNameFromHash(item.Key);
                    if (item.Key == 0)
                        name = "Any";
                    currentPath.Items.Add(new DecisionPathItem() { DisplayName = name, Value = item.Key });
                }

                decisionPath.Add(currentPath);
            }

            var arguments = argumentsList.Arguments
                .Select(x =>
                {
                    var name = _audioRepository.GetNameFromHash(x.ulGroupId);
                    return new { Name = name, x.ulGroupId };
                }).ToList();

            var decisionPathCollection = new DecisionPathCollection()
            {
                Header = new DecisionPath() { Items = arguments.Select(x => new DecisionPathItem() { DisplayName = x.Name, Value = x.ulGroupId }).ToList() },
                Paths = decisionPath
            };

            return decisionPathCollection;
        }


        List<(AkDecisionTree.Node[], uint)> GetDecisionPaths(AkDecisionTree decisionTree)
        {
            var decisionPaths = new List<(AkDecisionTree.Node[], uint)>();
            var stack = new Stack<AkDecisionTree.Node>();
            stack.Push(decisionTree.Root);
            GetDecisionPathsInternal(stack, decisionPaths);
            stack.Pop();
            return decisionPaths;
        }

        void GetDecisionPathsInternal(Stack<AkDecisionTree.Node> stack, List<(AkDecisionTree.Node[], uint)> decisionPaths)
        {
            var peek = stack.Peek();
            if (peek.IsAudioNode())
                decisionPaths.Add((stack.Select(e => e).Reverse().ToArray(), peek.AudioNodeId));

            peek.Children.ForEach(e =>
            {
                stack.Push(e);
                GetDecisionPathsInternal(stack, decisionPaths);
                stack.Pop();
            });
        }


        public class WritePath
        {
            public List<string> Legs { get; set; }

        }



        [DebuggerDisplay("WriteNode {Id}")]
        class WriteNode
        {
            public string Id;
            public List<WriteNode> Children = new List<WriteNode>();

            public WriteNode AddChild(string id)
            {
                var result = Children.FirstOrDefault(x => x.Id == id);
                if (result != null)
                {
                    return result;
                }
                else
                {
                    var newNode = new WriteNode { Id = id };
                    Children.Add(newNode);
                    return newNode;
                }
            }
        }


        public void Write()
        {
            var rootNode = new WriteNode();
            var paths = new List<WritePath>()
            { 
                new WritePath(){ Legs = new List<string>() {"Any","Horse", "Walking" }},
                new WritePath(){ Legs = new List<string>() {"Any","Dog", "Walking" }},
                new WritePath(){ Legs = new List<string>() {"Any","Dog", "Running" }},
                new WritePath(){ Legs = new List<string>() {"Any","Dog", "Swimming" }},
                new WritePath(){ Legs = new List<string>() {"Any","Cat", "Any" }},
                new WritePath(){ Legs = new List<string>() {"Any","Cat", "Running" }},
                new WritePath(){ Legs = new List<string>() {"Any","Cat", "Walking" }},
                new WritePath(){ Legs = new List<string>() {"Any","Cow", "Walking" }},
            };

            foreach (var path in paths)
            {
                var currentNode = rootNode;
                foreach (var pathLeg in path.Legs)
                    currentNode = currentNode.AddChild(pathLeg);
            }


            //var t = new AkDecisionTree.Node()
            //{ 
            //
            //}

            // Convert to flat list

        }


        public void CalculateCount(AkDecisionTree.Node node)
        { 
            foreach(var child in node.Children)
                CalculateCount(child);

            node.Children_uCount = (ushort)(node.Children.Count + node.Children.Sum(x => x.Children_uCount));
        }

        public void CalculateIndex(AkDecisionTree.Node node, ushort currentIndex = 1)
        {
            node.Children_uIdx = currentIndex;
            var newIndex = currentIndex + node.Children.Count;

            foreach (var child in node.Children)
                CalculateIndex(child, (ushort)newIndex++);
        }
    }
}
