using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioExplorer;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.Shared.Wwise.HircExploration
{
    public class HircTreeChildrenParser : HircTreeBaseParser
    {
        private record ArgumentPathLookupKey(HircTreeNode ParentNode, int Depth, uint State);

        public HircTreeChildrenParser(IAudioRepository audioRepository) : base(audioRepository)
        {
            HircProcessChildMap.Add(AkBkHircType.Event, ProcessEvent);
            HircProcessChildMap.Add(AkBkHircType.Action, ProcessAction);
            HircProcessChildMap.Add(AkBkHircType.SwitchContainer, ProcessSwitchContainer);
            HircProcessChildMap.Add(AkBkHircType.LayerContainer, ProcessBlendContainer);
            HircProcessChildMap.Add(AkBkHircType.RandomSequenceContainer, ProcessRandomSequenceContainer);
            HircProcessChildMap.Add(AkBkHircType.Sound, ProcessSound);
            HircProcessChildMap.Add(AkBkHircType.ActorMixer, ProcessActorMixer);
            HircProcessChildMap.Add(AkBkHircType.Dialogue_Event, ProcessDialogueEvent);
            HircProcessChildMap.Add(AkBkHircType.Music_Track, ProcessMusicTrack);
            HircProcessChildMap.Add(AkBkHircType.Music_Segment, ProcessMusicSegment);
            HircProcessChildMap.Add(AkBkHircType.Music_Switch, ProcessMusicSwitchContainer);
            HircProcessChildMap.Add(AkBkHircType.Music_Random_Sequence, ProcessMusicRandomSequenceContainer);
        }

        private void ProcessDialogueEvent(HircItem item, HircTreeNode parent)
        {
            var dialogueEvent = GetAsType<ICAkDialogueEvent>(item);
            var statePathParser = new StatePathParser(AudioRepository);
            var result = statePathParser.GetStatePaths(dialogueEvent);
            
            var dialogueEventNode = new HircTreeNode() { DisplayName = $"Dialogue Event - {AudioRepository.GetNameFromId(item.Id)}", Hirc = item };
            parent.Children.Add(dialogueEventNode);

            var argumentPathLookup = new Dictionary<ArgumentPathLookupKey, HircTreeNode>();
            foreach (var statePath in result.StatePaths)
            {
                var currentNode = dialogueEventNode;

                for (var depth = 0; depth < statePath.Items.Count; depth++)
                {
                    var statePathItem = statePath.Items[depth];
                    var lookupKey = new ArgumentPathLookupKey(currentNode, depth, statePathItem.Value);
                    if (!argumentPathLookup.TryGetValue(lookupKey, out var existingNode))
                    {
                        existingNode = new HircTreeNode()
                        {
                            DisplayName = $"State [{result.Header.Items[depth].DisplayName}] - {statePathItem.DisplayName}",
                            Hirc = dialogueEventNode.Hirc,
                            IsMetaNode = true
                        };

                        currentNode.Children.Add(existingNode);
                        argumentPathLookup.Add(lookupKey, existingNode);
                    }

                    currentNode = existingNode;
                }

                ProcessNext(statePath.ChildNodeId, currentNode);
            }
        }

        private void ProcessEvent(HircItem item, HircTreeNode parent)
        {
            var actionEvent = GetAsType<ICAkEvent>(item);
            var node = new HircTreeNode() { DisplayName = $"Action Event - {AudioRepository.GetNameFromId(item.Id)}", Hirc = item };
            parent.Children.Add(node);

            var actions = actionEvent.GetActionIds();
            ProcessNext(actions, node);
        }

        private void ProcessAction(HircItem item, HircTreeNode parent)
        {
            var action = GetAsType<ICAkAction>(item);
            var node = new HircTreeNode() { DisplayName = $"{action.GetActionType()} Action", Hirc = item, IsExpanded = true };
            parent.Children.Add(node);
            var childId = action.GetChildId();

            // Override child id if type is setState based on parameters 
            if (action.GetActionType() == AkActionType.SetState)
            {
                var stateGroupId = action.GetStateGroupId();
                var musicSwitches = AudioRepository.HircsById
                   .SelectMany(kvp => kvp.Value)
                   .Where(hirc => hirc.HircType == AkBkHircType.Music_Switch)
                   .DistinctBy(hirc => hirc.Id)
                   .Cast<CAkMusicSwitchCntr_V136>()
                   .ToList();

                foreach (var musicSwitch in musicSwitches)
                {
                    var allArgs = musicSwitch.Arguments.Select(x => x.GroupId).ToList();
                    if (allArgs.Contains(stateGroupId))
                        ProcessNext(musicSwitch.Id, node);
                }

                var normalSwitches = AudioRepository.HircsById
                   .SelectMany(kvp => kvp.Value)
                   .Where(hirc => hirc.HircType == AkBkHircType.SwitchContainer)
                   .DistinctBy(hirc => hirc.Id)
                   .Cast<CAkSwitchCntr_V136>()
                   .ToList();

                foreach (var normalSwitch in normalSwitches)
                {
                    if (normalSwitch.GroupId == stateGroupId)
                        ProcessNext(normalSwitch.Id, node);
                }
            }
            else 
                ProcessNext(childId, node);
        }

        private void ProcessSound(HircItem item, HircTreeNode parent)
        {
            var sound = GetAsType<ICAkSound>(item);

            var displayName = $"Sound - {sound.GetSourceId()}.wem";
            if (sound.GetStreamType() == AKBKSourceType.Data_BNK)
                displayName = $"Sound ({sound.GetStreamType()}) - {sound.GetSourceId()}.wem";

            var node = new HircTreeNode() { DisplayName = displayName, Hirc = item };
            parent.Children.Add(node);
        }

        private void ProcessSwitchContainer(HircItem item, HircTreeNode parent)
        {
            var switchContainer = GetAsType<ICAkSwitchCntr>(item);
            var switchGroup = AudioRepository.GetNameFromId(switchContainer.GroupId);

            var defaultSwitchValue = AudioRepository.GetNameFromId(switchContainer.DefaultSwitch);
            if (defaultSwitchValue == "0")
                defaultSwitchValue = "Any";

            var node = new HircTreeNode() { DisplayName = $"Switch Container (Default Value: {defaultSwitchValue})", Hirc = item };
            parent.Children.Add(node);

            foreach (var switchCase in switchContainer.SwitchList)
            {
                var switchValue = AudioRepository.GetNameFromId(switchCase.SwitchId);
                var switchValueNode = new HircTreeNode() { DisplayName = $"Switch [{switchGroup}] - {switchValue}", Hirc = item, IsMetaNode = true };
                node.Children.Add(switchValueNode);
                ProcessNext(switchCase.NodeIdList, switchValueNode);
            }
        }

        private void ProcessBlendContainer(HircItem item, HircTreeNode parent)
        {
            var blendContainer = GetAsType<ICAkLayerCntr>(item);
            var node = new HircTreeNode() { DisplayName = $"Blend Container", Hirc = item };
            parent.Children.Add(node);

            foreach (var layer in blendContainer.GetChildren())
                ProcessNext(layer, node);
        }

        private void ProcessRandomSequenceContainer(HircItem item, HircTreeNode parent)
        {
            var randomSequenceContainer = GetAsType<ICAkRanSeqCntr>(item);
            var node = new HircTreeNode() { DisplayName = $"Random / Sequence Container", Hirc = item, IsExpanded = true };
            parent.Children.Add(node);
            ProcessNext(randomSequenceContainer.GetChildren(), node);
        }

        private void ProcessMusicTrack(HircItem item, HircTreeNode parent)
        {
            var musicTrack = GetAsType<ICAkMusicTrack>(item);
            foreach (var sourceItem in musicTrack.GetChildren())
            {
                var node = new HircTreeNode() { DisplayName = $"Music Track - {sourceItem}.wem", Hirc = item };
                parent.Children.Add(node);
            }
        }

        private void ProcessMusicSegment(HircItem item, HircTreeNode parent)
        {
            var musicSegment = GetAsType<CAkMusicSegment_V136>(item);
            var node = new HircTreeNode() { DisplayName = $"Music Segment", Hirc = item };
            parent.Children.Add(node);

            foreach (var childId in musicSegment.MusicNodeParams.Children.ChildIds)
                ProcessNext(childId, node);
        }

        private void ProcessMusicSwitchContainer(HircItem item, HircTreeNode parent)
        {
            var musicSwitchContainer = GetAsType<CAkMusicSwitchCntr_V136>(item);
            var statePathParser = new StatePathParser(AudioRepository);
            var result = statePathParser.GetStatePaths(musicSwitchContainer);

            var musicSwitchContainerNode = new HircTreeNode() { DisplayName = $"Music Switch Container", Hirc = item };
            parent.Children.Add(musicSwitchContainerNode);

            var argumentPathLookup = new Dictionary<ArgumentPathLookupKey, HircTreeNode>();
            foreach (var statePath in result.StatePaths)
            {
                var currentNode = musicSwitchContainerNode;

                for (var depth = 0; depth < statePath.Items.Count; depth++)
                {
                    var statePathItem = statePath.Items[depth];
                    var lookupKey = new ArgumentPathLookupKey(currentNode, depth, statePathItem.Value);

                    if (!argumentPathLookup.TryGetValue(lookupKey, out var existingNode))
                    {
                        existingNode = new HircTreeNode()
                        {
                            DisplayName = $"Music Switch [{result.Header.Items[depth].DisplayName}] - {statePathItem.DisplayName}",
                            Hirc = musicSwitchContainerNode.Hirc,
                            IsMetaNode = true
                        };

                        currentNode.Children.Add(existingNode);
                        argumentPathLookup.Add(lookupKey, existingNode);
                    }

                    currentNode = existingNode;
                }

                ProcessNext(statePath.ChildNodeId, currentNode);
            }
        }

        private void ProcessMusicRandomSequenceContainer(HircItem item, HircTreeNode parent)
        {
            var musicRandomSequenceContainer = GetAsType<CAkMusicRanSeqCntr_V136>(item);
            var node = new HircTreeNode() { DisplayName = $"Music Random / Sequence Container", Hirc = item, IsExpanded = true };
            parent.Children.Add(node);

            if (musicRandomSequenceContainer.PlayList.Count != 0)
            {
                foreach (var playList in musicRandomSequenceContainer.PlayList.First().PlayList)
                    ProcessNext(playList.SegmentId, node);
            }
        }

        public void ProcessActorMixer(HircItem item, HircTreeNode parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var node = new HircTreeNode() { DisplayName = $"Actor Mixer", Hirc = item };
            parent.Children.Add(node);
            ProcessNext(actorMixer.GetChildren(), node);
        }
    }
}
