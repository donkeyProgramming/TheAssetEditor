using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Storage;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.Shared;
using Shared.GameFormats.Wwise.Hirc.V136;

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

        public DecisionPathCollection GetDecisionPaths(ICAkDialogueEvent dialogueEvent) => GetDecisionPaths(dialogueEvent.AkDecisionTree, dialogueEvent.ArgumentList);
        public DecisionPathCollection GetDecisionPaths(CAkMusicSwitchCntr_v136 musicSwitch) => GetDecisionPaths(musicSwitch.AkDecisionTree, musicSwitch.ArgumentList);

        DecisionPathCollection GetDecisionPaths(AkDecisionTree decisionTree, ArgumentList argumentsList)
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

            var arguments = argumentsList.Arguments
                .Select(x =>
                {
                    var name = _audioRepository.GetNameFromHash(x.UlGroupId);
                    return new { Name = name, x.UlGroupId };
                }).ToList();

            var decisionPathCollection = new DecisionPathCollection()
            {
                Header = new DecisionPath() { Items = arguments.Select(x => new DecisionPathItem() { DisplayName = x.Name, Value = x.UlGroupId }).ToList() },
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
    }
}
