using System.Linq;
using Editors.Audio.AudioExplorer;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Utilities;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.Shared.Wwise
{
    public class WwiseTreeParserChildren : WwiseTreeParserBase
    {
        public WwiseTreeParserChildren(IAudioRepository audioRepository) : base(audioRepository)
        {
            _hircProcessChildMap.Add(AkBkHircType.Event, ProcessEvent);
            _hircProcessChildMap.Add(AkBkHircType.Action, ProcessAction);
            _hircProcessChildMap.Add(AkBkHircType.SwitchContainer, ProcessSwitchControl);
            _hircProcessChildMap.Add(AkBkHircType.LayerContainer, ProcessLayerContainer);
            _hircProcessChildMap.Add(AkBkHircType.RandomSequenceContainer, ProcessSequenceContainer);
            _hircProcessChildMap.Add(AkBkHircType.Sound, ProcessSound);
            _hircProcessChildMap.Add(AkBkHircType.ActorMixer, ProcessActorMixer);
            _hircProcessChildMap.Add(AkBkHircType.Dialogue_Event, ProcessDialogueEvent);
            _hircProcessChildMap.Add(AkBkHircType.Music_Track, ProcessMusicTrack);
            _hircProcessChildMap.Add(AkBkHircType.Music_Segment, ProcessMusicSegment);
            _hircProcessChildMap.Add(AkBkHircType.Music_Switch, ProcessMusicSwitch);
            _hircProcessChildMap.Add(AkBkHircType.Music_Random_Sequence, ProcessRandMusicContainer);
        }

        private void ProcessDialogueEvent(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<ICAkDialogueEvent>(item);

            var helper = new DecisionPathHelper(_audioRepository);
            var paths = helper.GetDecisionPaths(hirc);

            var dialogueEventNode = new HircTreeItem() { DisplayName = $"Dialogue Event {_audioRepository.GetNameFromId(item.Id)} - [{paths.Header.GetAsString()}]", Item = item };
            parent.Children.Add(dialogueEventNode);

            foreach (var path in paths.Paths)
            {
                var pathNode = new HircTreeItem() { DisplayName = path.GetAsString(), Item = item, IsExpanded = false };
                dialogueEventNode.Children.Add(pathNode);
                ProcessNext(path.ChildNodeId, pathNode);
            }
        }

        private void ProcessEvent(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkEvent>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Action Event {_audioRepository.GetNameFromId(item.Id)}", Item = item };
            parent.Children.Add(actionTreeNode);

            var actions = actionHirc.GetActionIds();
            ProcessNext(actions, actionTreeNode);
        }

        private void ProcessAction(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkAction>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Action {actionHirc.GetActionType()}", Item = item };
            parent.Children.Add(actionTreeNode);
            var childId = actionHirc.GetChildId();

            // Override child id if type is setState based on parameters 
            if (actionHirc.GetActionType() == AkActionType.SetState)
            {
                var stateGroupId = actionHirc.GetStateGroupId();
                var musicSwitches = _audioRepository.HircsById
                   .SelectMany(x => x.Value)
                   .Where(X => X.HircType == AkBkHircType.Music_Switch)
                   .DistinctBy(x => x.Id)
                   .Cast<CAkMusicSwitchCntr_V136>()
                   .ToList();

                foreach (var musicSwitch in musicSwitches)
                {
                    var allArgs = musicSwitch.Arguments.Select(x => x.GroupId).ToList();
                    if (allArgs.Contains(stateGroupId))
                        ProcessNext(musicSwitch.Id, actionTreeNode);
                }

                var normalSwitches = _audioRepository.HircsById
                   .SelectMany(x => x.Value)
                   .Where(X => X.HircType == AkBkHircType.SwitchContainer)
                   .DistinctBy(x => x.Id)
                   .Cast<CAkSwitchCntr_V136>()
                   .ToList();

                foreach (var normalSwitch in normalSwitches)
                    if (normalSwitch.GroupId == stateGroupId)
                        ProcessNext(normalSwitch.Id, actionTreeNode);
            }
            else ProcessNext(childId, actionTreeNode);
        }

        private void ProcessSound(HircItem item, HircTreeItem parent)
        {
            var soundHirc = GetAsType<ICAkSound>(item);

            string displayName = soundHirc.GetStreamType() == AKBKSourceType.Data_BNK
                ? $"Sound {soundHirc.GetSourceId()}.wem (stream type: {soundHirc.GetStreamType()})"
                : $"Sound {soundHirc.GetSourceId()}.wem";

            var soundTreeNode = new HircTreeItem() { DisplayName = displayName, Item = item };
            parent.Children.Add(soundTreeNode);
        }

        public void ProcessActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var actorMixerNode = new HircTreeItem() { DisplayName = $"ActorMixer {_audioRepository.GetNameFromId(item.Id)}", Item = item };
            parent.Children.Add(actorMixerNode);

            ProcessNext(actorMixer.GetChildren(), actorMixerNode);
        }

        private void ProcessSwitchControl(HircItem item, HircTreeItem parent)
        {
            var switchControl = GetAsType<ICAkSwitchCntr>(item);
            var switchType = _audioRepository.GetNameFromId(switchControl.GroupId);
            var defaultValue = _audioRepository.GetNameFromId(switchControl.DefaultSwitch);
            var switchControlNode = new HircTreeItem() { DisplayName = $"Switch {switchType} DefaultValue: {defaultValue}", Item = item };
            parent.Children.Add(switchControlNode);

            foreach (var switchCase in switchControl.SwitchList)
            {
                var switchValue = _audioRepository.GetNameFromId(switchCase.SwitchId);
                var switchValueNode = new HircTreeItem() { DisplayName = $"SwitchValue: {switchValue}", Item = item, IsMetaNode = true };
                switchControlNode.Children.Add(switchValueNode);

                ProcessNext(switchCase.NodeIdList, switchValueNode);
            }
        }

        private void ProcessLayerContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<ICAkLayerCntr>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container", Item = item };
            parent.Children.Add(layerNode);

            foreach (var layer in layerContainer.GetChildren())
                ProcessNext(layer, layerNode);
        }

        private void ProcessSequenceContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<ICAkRanSeqCntr>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Rand Container", Item = item };
            parent.Children.Add(layerNode);

            ProcessNext(layerContainer.GetChildren(), layerNode);
        }

        private void ProcessMusicTrack(HircItem item, HircTreeItem parent)
        {
            var musicTrackHirc = GetAsType<ICAkMusicTrack>(item);

            foreach (var sourceItem in musicTrackHirc.GetChildren())
            {
                var musicTrackTreeNode = new HircTreeItem() { DisplayName = $"Music Track {sourceItem}.wem", Item = item };
                parent.Children.Add(musicTrackTreeNode);
            }
        }

        private void ProcessMusicSegment(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<CAkMusicSegment_V136>(item);
            var node = new HircTreeItem() { DisplayName = $"Music Segment", Item = item };
            parent.Children.Add(node);

            foreach (var childId in hirc.MusicNodeParams.Children.ChildIds)
                ProcessNext(childId, node);
        }

        private void ProcessMusicSwitch(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<CAkMusicSwitchCntr_V136>(item);

            var helper = new DecisionPathHelper(_audioRepository);
            var paths = helper.GetDecisionPaths(hirc);

            var dialogueEventNode = new HircTreeItem() { DisplayName = $"Music Switch {_audioRepository.GetNameFromId(item.Id)} - [{paths.Header.GetAsString()}]", Item = item };
            parent.Children.Add(dialogueEventNode);

            foreach (var path in paths.Paths)
            {
                var pathNode = new HircTreeItem() { DisplayName = path.GetAsString(), Item = hirc, IsExpanded = false };
                dialogueEventNode.Children.Add(pathNode);
                ProcessNext(path.ChildNodeId, pathNode);
            }
        }

        private void ProcessRandMusicContainer(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<CAkMusicRanSeqCntr_V136>(item);
            var node = new HircTreeItem() { DisplayName = $"Music Rand Container", Item = item };
            parent.Children.Add(node);

            if (hirc.PlayList.Count != 0)
                foreach (var playList in hirc.PlayList.First().PlayList)
                    ProcessNext(playList.SegmentId, node);
        }
    }
}
