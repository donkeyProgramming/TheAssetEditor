using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Storage;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V112;
using Shared.GameFormats.Wwise.Hirc.V112.Shared;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;
using static Shared.GameFormats.Wwise.Hirc.V112.Shared.AkDecisionTree_V112;
using static Shared.GameFormats.Wwise.Hirc.V136.Shared.AkDecisionTree_V136;

namespace Editors.Audio.Utility
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

        public DecisionPathCollection GetDecisionPaths(ICAkDialogueEvent dialogueEvent) =>
            dialogueEvent switch
            {
                CAkDialogueEvent_v136 event136 => GetDecisionPaths(event136.AkDecisionTree, event136.Arguments),
                CAkDialogueEvent_v112 event112 => GetDecisionPaths(event112.AkDecisionTree, event112.Arguments),
                _ => throw new NotImplementedException(),
            };

        public DecisionPathCollection GetDecisionPaths(CAkMusicSwitchCntr_v136 musicSwitch) => GetDecisionPaths(musicSwitch.AkDecisionTree, musicSwitch.Arguments);
        
        DecisionPathCollection GetDecisionPaths(AkDecisionTree_V136 decisionTree, List<AkGameSync_V136> argumentsList)
        {
            var paths = GetDecisionPaths(decisionTree);
            var decisionPath = new List<DecisionPath>();
            foreach (var path in paths)
            {
                var currentPath = new DecisionPath() { ChildNodeId = path.Item2 };
                foreach (var item in path.Item1.Skip(1))
                {
                    var name = _audioRepository.GetNameFromHash(item.Key);
                    if (item.Key == 0)
                        name = "Any";
                    currentPath.Items.Add(new DecisionPathItem() { DisplayName = name, Value = (uint)item.Key });
                }

                decisionPath.Add(currentPath);
            }

            var arguments = argumentsList
                .Select(x =>
                {
                    var name = _audioRepository.GetNameFromHash(x.GroupId);
                    return new { Name = name, x.GroupId };
                }).ToList();

            var decisionPathCollection = new DecisionPathCollection()
            {
                Header = new DecisionPath() { Items = arguments.Select(x => new DecisionPathItem() { DisplayName = x.Name, Value = x.GroupId }).ToList() },
                Paths = decisionPath
            };

            return decisionPathCollection;
        }

        private DecisionPathCollection GetDecisionPaths(AkDecisionTree_V112 decisionTree, List<AkGameSync_V112> argumentsList)
        {
            var paths = GetDecisionPaths(decisionTree);
            var decisionPath = new List<DecisionPath>();
            foreach (var path in paths)
            {
                var currentPath = new DecisionPath() { ChildNodeId = path.Item2 };
                foreach (var item in path.Item1.Skip(1))
                {
                    var name = _audioRepository.GetNameFromHash(item.Key);
                    if (item.Key == 0)
                        name = "Any";
                    currentPath.Items.Add(new DecisionPathItem() { DisplayName = name, Value = (uint)item.Key });
                }

                decisionPath.Add(currentPath);
            }

            var arguments = argumentsList
                .Select(x =>
                {
                    var name = _audioRepository.GetNameFromHash(x.GroupId);
                    return new { Name = name, x.GroupId };
                }).ToList();

            var decisionPathCollection = new DecisionPathCollection()
            {
                Header = new DecisionPath() { Items = arguments.Select(x => new DecisionPathItem() { DisplayName = x.Name, Value = x.GroupId }).ToList() },
                Paths = decisionPath
            };

            return decisionPathCollection;
        }

        private static List<(Node_V136[], uint)> GetDecisionPaths(AkDecisionTree_V136 decisionTree)
        {
            var decisionPaths = new List<(Node_V136[], uint)>();
            var stack = new Stack<Node_V136>();
            stack.Push(decisionTree.DecisionTree);
            GetDecisionPathsInternal(stack, decisionPaths);
            stack.Pop();
            return decisionPaths;
        }

        private static List<(Node_V112[], uint)> GetDecisionPaths(AkDecisionTree_V112 decisionTree)
        {
            var decisionPaths = new List<(Node_V112[], uint)>();
            var stack = new Stack<Node_V112>();
            stack.Push(decisionTree.DecisionTree);
            GetDecisionPathsInternal(stack, decisionPaths);
            stack.Pop();
            return decisionPaths;
        }

        private static void GetDecisionPathsInternal(Stack<Node_V136> stack, List<(Node_V136[], uint)> decisionPaths)
        {
            var peek = stack.Peek();
            if (peek.Nodes.Count == 0)
                decisionPaths.Add((stack.Select(e => e).Reverse().ToArray(), peek.AudioNodeId));

            peek.Nodes.ForEach(e =>
            {
                stack.Push(e);
                GetDecisionPathsInternal(stack, decisionPaths);
                stack.Pop();
            });
        }

        private static void GetDecisionPathsInternal(Stack<Node_V112> stack, List<(Node_V112[], uint)> decisionPaths)
        {
            var peek = stack.Peek();
            if (peek.Nodes.Count == 0)
                decisionPaths.Add((stack.Select(e => e).Reverse().ToArray(), peek.AudioNodeId));

            peek.Nodes.ForEach(e =>
            {
                stack.Push(e);
                GetDecisionPathsInternal(stack, decisionPaths);
                stack.Pop();
            });
        }
    }
}
