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
        public HircTreeChildrenParser(IAudioRepository audioRepository) : base(audioRepository)
        {
            HircProcessChildMap.Add(AkBkHircType.Event, ProcessEvent);
            HircProcessChildMap.Add(AkBkHircType.Action, ProcessAction);
            HircProcessChildMap.Add(AkBkHircType.SwitchContainer, ProcessSwitchControl);
            HircProcessChildMap.Add(AkBkHircType.LayerContainer, ProcessLayerContainer);
            HircProcessChildMap.Add(AkBkHircType.RandomSequenceContainer, ProcessSequenceContainer);
            HircProcessChildMap.Add(AkBkHircType.Sound, ProcessSound);
            HircProcessChildMap.Add(AkBkHircType.ActorMixer, ProcessActorMixer);
            HircProcessChildMap.Add(AkBkHircType.Dialogue_Event, ProcessDialogueEvent);
            HircProcessChildMap.Add(AkBkHircType.Music_Track, ProcessMusicTrack);
            HircProcessChildMap.Add(AkBkHircType.Music_Segment, ProcessMusicSegment);
            HircProcessChildMap.Add(AkBkHircType.Music_Switch, ProcessMusicSwitch);
            HircProcessChildMap.Add(AkBkHircType.Music_Random_Sequence, ProcessRandMusicContainer);
        }

        private void ProcessDialogueEvent(HircItem item, HircTreeNode parent)
        {
            var hirc = GetAsType<ICAkDialogueEvent>(item);

            var statePathParser = new StatePathParser(AudioRepository);
            var result = statePathParser.GetStatePaths(hirc);

            var dialogueEventNode = new HircTreeNode() { DisplayName = $"Dialogue Event {AudioRepository.GetNameFromId(item.Id)} - [{result.Header.GetAsString()}]", Item = item };
            parent.Children.Add(dialogueEventNode);

            foreach (var path in result.StatePaths)
            {
                var pathNode = new HircTreeNode() { DisplayName = path.GetAsString(), Item = item, IsExpanded = false };
                dialogueEventNode.Children.Add(pathNode);
                ProcessNext(path.ChildNodeId, pathNode);
            }
        }

        private void ProcessEvent(HircItem item, HircTreeNode parent)
        {
            var actionHirc = GetAsType<ICAkEvent>(item);
            var actionTreeNode = new HircTreeNode() { DisplayName = $"Action Event {AudioRepository.GetNameFromId(item.Id)}", Item = item };
            parent.Children.Add(actionTreeNode);

            var actions = actionHirc.GetActionIds();
            ProcessNext(actions, actionTreeNode);
        }

        private void ProcessAction(HircItem item, HircTreeNode parent)
        {
            var actionHirc = GetAsType<ICAkAction>(item);
            var actionTreeNode = new HircTreeNode() { DisplayName = $"Action {actionHirc.GetActionType()}", Item = item };
            parent.Children.Add(actionTreeNode);
            var childId = actionHirc.GetChildId();

            // Override child id if type is setState based on parameters 
            if (actionHirc.GetActionType() == AkActionType.SetState)
            {
                var stateGroupId = actionHirc.GetStateGroupId();
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
                        ProcessNext(musicSwitch.Id, actionTreeNode);
                }

                var normalSwitches = AudioRepository.HircsById
                   .SelectMany(kvp => kvp.Value)
                   .Where(hirc => hirc.HircType == AkBkHircType.SwitchContainer)
                   .DistinctBy(hirc => hirc.Id)
                   .Cast<CAkSwitchCntr_V136>()
                   .ToList();

                foreach (var normalSwitch in normalSwitches)
                    if (normalSwitch.GroupId == stateGroupId)
                        ProcessNext(normalSwitch.Id, actionTreeNode);
            }
            else ProcessNext(childId, actionTreeNode);
        }

        private void ProcessSound(HircItem item, HircTreeNode parent)
        {
            var soundHirc = GetAsType<ICAkSound>(item);

            var displayName = soundHirc.GetStreamType() == AKBKSourceType.Data_BNK
                ? $"Sound {soundHirc.GetSourceId()}.wem (stream type: {soundHirc.GetStreamType()})"
                : $"Sound {soundHirc.GetSourceId()}.wem";

            var soundTreeNode = new HircTreeNode() { DisplayName = displayName, Item = item };
            parent.Children.Add(soundTreeNode);
        }

        public void ProcessActorMixer(HircItem item, HircTreeNode parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var actorMixerNode = new HircTreeNode() { DisplayName = $"Actor Mixer {AudioRepository.GetNameFromId(item.Id)}", Item = item };
            parent.Children.Add(actorMixerNode);

            ProcessNext(actorMixer.GetChildren(), actorMixerNode);
        }

        private void ProcessSwitchControl(HircItem item, HircTreeNode parent)
        {
            var switchControl = GetAsType<ICAkSwitchCntr>(item);
            var switchType = AudioRepository.GetNameFromId(switchControl.GroupId);
            var defaultValue = AudioRepository.GetNameFromId(switchControl.DefaultSwitch);
            var switchControlNode = new HircTreeNode() { DisplayName = $"Switch {switchType} Default Value: {defaultValue}", Item = item };
            parent.Children.Add(switchControlNode);

            foreach (var switchCase in switchControl.SwitchList)
            {
                var switchValue = AudioRepository.GetNameFromId(switchCase.SwitchId);
                var switchValueNode = new HircTreeNode() { DisplayName = $"Switch Value: {switchValue}", Item = item, IsMetaNode = true };
                switchControlNode.Children.Add(switchValueNode);

                ProcessNext(switchCase.NodeIdList, switchValueNode);
            }
        }

        private void ProcessLayerContainer(HircItem item, HircTreeNode parent)
        {
            var layerContainer = GetAsType<ICAkLayerCntr>(item);
            var layerNode = new HircTreeNode() { DisplayName = $"Layer Container", Item = item };
            parent.Children.Add(layerNode);

            foreach (var layer in layerContainer.GetChildren())
                ProcessNext(layer, layerNode);
        }

        private void ProcessSequenceContainer(HircItem item, HircTreeNode parent)
        {
            var layerContainer = GetAsType<ICAkRanSeqCntr>(item);
            var layerNode = new HircTreeNode() { DisplayName = $"Random Sequence Container", Item = item };
            parent.Children.Add(layerNode);

            ProcessNext(layerContainer.GetChildren(), layerNode);
        }

        private void ProcessMusicTrack(HircItem item, HircTreeNode parent)
        {
            var musicTrackHirc = GetAsType<ICAkMusicTrack>(item);

            foreach (var sourceItem in musicTrackHirc.GetChildren())
            {
                var musicTrackTreeNode = new HircTreeNode() { DisplayName = $"Music Track {sourceItem}.wem", Item = item };
                parent.Children.Add(musicTrackTreeNode);
            }
        }

        private void ProcessMusicSegment(HircItem item, HircTreeNode parent)
        {
            var hirc = GetAsType<CAkMusicSegment_V136>(item);
            var node = new HircTreeNode() { DisplayName = $"Music Segment", Item = item };
            parent.Children.Add(node);

            foreach (var childId in hirc.MusicNodeParams.Children.ChildIds)
                ProcessNext(childId, node);
        }

        private void ProcessMusicSwitch(HircItem item, HircTreeNode parent)
        {
            var hirc = GetAsType<CAkMusicSwitchCntr_V136>(item);

            var statePathParser = new StatePathParser(AudioRepository);
            var result = statePathParser.GetStatePaths(hirc);

            var dialogueEventNode = new HircTreeNode() { DisplayName = $"Music Switch {AudioRepository.GetNameFromId(item.Id)} - [{result.Header.GetAsString()}]", Item = item };
            parent.Children.Add(dialogueEventNode);

            foreach (var path in result.StatePaths)
            {
                var pathNode = new HircTreeNode() { DisplayName = path.GetAsString(), Item = hirc, IsExpanded = false };
                dialogueEventNode.Children.Add(pathNode);
                ProcessNext(path.ChildNodeId, pathNode);
            }
        }

        private void ProcessRandMusicContainer(HircItem item, HircTreeNode parent)
        {
            var hirc = GetAsType<CAkMusicRanSeqCntr_V136>(item);
            var node = new HircTreeNode() { DisplayName = $"Music Random Container", Item = item };
            parent.Children.Add(node);

            if (hirc.PlayList.Count != 0)
                foreach (var playList in hirc.PlayList.First().PlayList)
                    ProcessNext(playList.SegmentId, node);
        }
    }
}
