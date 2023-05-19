using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using Audio.Utility;

namespace Audio.AudioEditor
{
    public class WWiseTreeParserChildren : WWiseTreeParserBase
    {
        public WWiseTreeParserChildren(IAudioRepository repository, bool showId, bool showOwningBnkFile, bool filterByBnkName)
            : base(repository, showId, showOwningBnkFile, filterByBnkName)
        {
            _hircProcessChildMap.Add(HircType.Event, ProcessEvent);
            _hircProcessChildMap.Add(HircType.Action, ProcessAction);
            _hircProcessChildMap.Add(HircType.SwitchContainer, ProcessSwitchControl);
            _hircProcessChildMap.Add(HircType.LayerContainer, ProcessLayerContainer);
            _hircProcessChildMap.Add(HircType.SequenceContainer, ProcessSequenceContainer);
            _hircProcessChildMap.Add(HircType.Sound, ProcessSound);
            _hircProcessChildMap.Add(HircType.ActorMixer, ProcessActorMixer);
            _hircProcessChildMap.Add(HircType.Dialogue_Event, ProcessDialogEvent);
            _hircProcessChildMap.Add(HircType.Music_Track, ProcessMusicTrack);
            _hircProcessChildMap.Add(HircType.Music_Segment, ProcessMusicSegment);
            //_hircProcessChildMap.Add(HircType.Music_Switch, ProcessDialogEvent);
            //_hircProcessChildMap.Add(HircType.Music_Random_Sequence, ProcessDialogEvent);
        }

        private void ProcessDialogEvent(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<CAkDialogueEvent_v136>(item);
            var dialogEventNode = new HircTreeItem() { DisplayName = $"Dialog_Event {_repository.GetNameFromHash(item.Id)}", Item = item };
            parent.Children.Add(dialogEventNode);

            ProcessNode(hirc.AkDecisionTree.Root, dialogEventNode, item, 0);
        }

        void ProcessNode(AkDecisionTree.Node node, HircTreeItem parent, HircItem owner, uint depdth)
        {
            if (node.IsAudioNode())
            {
                if (node.Content.Key != 0)
                {
                    var dialogEventNode = new HircTreeItem() { DisplayName = $"{_repository.GetNameFromHash(node.Content.Key)}", Item = owner };
                    parent.Children.Add(dialogEventNode);
                    ProcessNext(node.AudioNodeId, dialogEventNode);
                }
                else
                {
                    var dialogEventNode = new HircTreeItem() { DisplayName = $"Default", Item = owner };
                    parent.Children.Add(dialogEventNode);
                    ProcessNext(node.AudioNodeId, dialogEventNode);
                }
            }
            else
            {
                var nextNode = parent;
                depdth += 1;
                if (depdth > 2) // For some reason we can always skip the first 2 nodes
                {
                    var dialogEventNode = new HircTreeItem() { DisplayName = $"{_repository.GetNameFromHash(node.Content.Key)}", Item = owner };
                    parent.Children.Add(dialogEventNode);
                    nextNode = dialogEventNode;
                }

                foreach (var child in node.Children)
                    ProcessNode(child, nextNode, owner, depdth);
            }
        }

        void ProcessEvent(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkEvent>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Event {_repository.GetNameFromHash(item.Id)}", Item = item };
            parent.Children.Add(actionTreeNode);

            var actions = actionHirc.GetActionIds();
            ProcessNext(actions, actionTreeNode);
        }

        void ProcessAction(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkAction>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Action {actionHirc.GetActionType()}", Item = item };
            parent.Children.Add(actionTreeNode);

            var childId = actionHirc.GetChildId();
            ProcessNext(childId, actionTreeNode);
        }

        private void ProcessSound(HircItem item, HircTreeItem parent)
        {
            var soundHirc = GetAsType<ICAkSound>(item);
            var soundTreeNode = new HircTreeItem() { DisplayName = $"Sound {soundHirc.GetSourceId()}.wem", Item = item };
            parent.Children.Add(soundTreeNode);
        }

        private void ProcessActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<CAkActorMixer_v136>(item);
            var actorMixerNode = new HircTreeItem() { DisplayName = $"ActorMixer {_repository.GetNameFromHash(item.Id)}", Item = item };
            parent.Children.Add(actorMixerNode);

            ProcessNext(actorMixer.Children.ChildIdList, actorMixerNode);
        }

        void ProcessSwitchControl(HircItem item, HircTreeItem parent)
        {
            var switchControl = GetAsType<CAkSwitchCntr_v136>(item);
            var switchType = _repository.GetNameFromHash(switchControl.ulGroupID);
            var defaultValue = _repository.GetNameFromHash(switchControl.ulDefaultSwitch);
            var switchControlNode = new HircTreeItem() { DisplayName = $"Switch {switchType} DefaultValue: {defaultValue}", Item = item };
            parent.Children.Add(switchControlNode);

            foreach (var switchCase in switchControl.SwitchList)
            {
                var switchValue = _repository.GetNameFromHash(switchCase.SwitchId);
                var switchValueNode = new HircTreeItem() { DisplayName = $"SwitchValue: {switchValue}", Item = item, IsMetaNode = true};
                switchControlNode.Children.Add(switchValueNode);

                ProcessNext(switchCase.NodeIdList, switchValueNode);
            }
        }

        private void ProcessLayerContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<CAkLayerCntr_v136>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Layer Container", Item = item };
            parent.Children.Add(layerNode);

            foreach (var layer in layerContainer.Children.ChildIdList)
                ProcessNext(layer, layerNode);           
        }

        private void ProcessSequenceContainer(HircItem item, HircTreeItem parent)
        {
            var layerContainer = GetAsType<CAkRanSeqCnt>(item);
            var layerNode = new HircTreeItem() { DisplayName = $"Rand Container", Item = item };
            parent.Children.Add(layerNode);

            ProcessNext(layerContainer.GetChildren(), layerNode);
        }

        private void ProcessMusicTrack(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<CAkMusicTrack_v136>(item);
            var node = new HircTreeItem() { DisplayName = $"Music Track", Item = item };
            parent.Children.Add(node);

            foreach (var sourceItem in hirc.pSourceList)
                ProcessNext(sourceItem.akMediaInformation.SourceId, node);
        }

        private void ProcessMusicSegment(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<CAkMusicSegment_v136>(item);
            var node = new HircTreeItem() { DisplayName = $"Music Segment", Item = item };
            parent.Children.Add(node);

            foreach (var childId in hirc.MusicNodeParams.Children.ChildIdList)
                ProcessNext(childId, node);
        }
    }
}
