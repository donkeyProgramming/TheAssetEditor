using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V112;
using Shared.GameFormats.Wwise.Hirc.V136;
using static Editors.Audio.Shared.Wwise.HircExploration.StatePathParser.StatePath;
using static Shared.GameFormats.Wwise.Hirc.ICAkDialogueEvent;

namespace Editors.Audio.Shared.Wwise.HircExploration
{
    public class StatePathParser(IAudioRepository audioRepository)
    {
        private readonly IAudioRepository _audioRepository = audioRepository;

        public class Result
        {
            public StatePath Header { get; set; }
            public List<StatePath> StatePaths { get; set; } = [];
        }

        public class StatePath
        {
            public List<StatePathItem> Items { get; set; } = [];
            public uint ChildNodeId { get; set; }
            public string GetAsString(string separator = ".") => string.Join(separator, Items.Select(x => x.DisplayName));

            public class StatePathItem
            {
                public uint Value { get; set; }
                public string DisplayName { get; set; }
            }
        }

        public Result GetStatePaths(ICAkDialogueEvent dialogueEvent)
        {
            return dialogueEvent switch
            {
                CAkDialogueEvent_V136 v136 => GetStatePaths(v136.AkDecisionTree, v136.Arguments),
                CAkDialogueEvent_V112 v112 => GetStatePaths(v112.AkDecisionTree, v112.Arguments),
                _ => throw new NotImplementedException()
            };
        }

        public Result GetStatePaths(CAkMusicSwitchCntr_V136 musicSwitch) => GetStatePaths(musicSwitch.AkDecisionTree, musicSwitch.Arguments.Cast<IAkGameSync>().ToList());

        public Result GetStatePaths(IAkDecisionTree decisionTree, IEnumerable<IAkGameSync> argumentsEnumerable)
        {
            var root = decisionTree.GetDecisionTree();
            var statePaths = BuildStatePaths(root);

            var headerItems = argumentsEnumerable
                .Select(argument => new StatePathItem
                {
                    DisplayName = _audioRepository.GetNameFromId(argument.GroupId),
                    Value = argument.GroupId
                })
                .ToList();

            var statePath = new List<StatePath>(statePaths.Count);
            for (var p = 0; p < statePaths.Count; p++)
            {
                var nodes = statePaths[p];
                var leaf = nodes[^1];

                var decisionPath = new StatePath { ChildNodeId = leaf.GetAudioNodeId() };

                for (var i = 1; i < nodes.Count; i++)
                {
                    var key = nodes[i].GetKey();
                    var name = key == 0 ? "Any" : _audioRepository.GetNameFromId(key);
                    decisionPath.Items.Add(new StatePathItem { DisplayName = name, Value = key });
                }

                statePath.Add(decisionPath);
            }

            return new Result
            {
                Header = new StatePath { Items = headerItems },
                StatePaths = statePath
            };
        }

        private static List<List<IAkDecisionNode>> BuildStatePaths(IAkDecisionNode root)
        {
            var results = new List<List<IAkDecisionNode>>();
            var current = new List<IAkDecisionNode>(16);
            Traverse(root, current, results);
            return results;
        }

        private static void Traverse(IAkDecisionNode node, List<IAkDecisionNode> current, List<List<IAkDecisionNode>> results)
        {
            if (node == null)
                return;

            current.Add(node);
            var childrenCount = node.GetChildrenCount();

            if (childrenCount == 0)
                results.Add(new List<IAkDecisionNode>(current));
            else
            {
                for (var i = 0; i < childrenCount; i++)
                    Traverse(node.GetChildAtIndex(i), current, results);
            }

            current.RemoveAt(current.Count - 1);
        }
    }
}
