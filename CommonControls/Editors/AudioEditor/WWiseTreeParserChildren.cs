using CommonControls.Editors.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.FileTypes.Sound.WWise.Hirc.V136;

namespace CommonControls.Editors.AudioEditor
{
    public class WWiseTreeParserChildren : WWiseTreeParserBase
    {
        public WWiseTreeParserChildren(ExtenededSoundDataBase globalSoundDb, WWiseNameLookUpHelper nameLookUpHelper, bool showId, bool showOwningBnkFile, bool filterByBnkName)
            : base(globalSoundDb, nameLookUpHelper, showId, showOwningBnkFile, filterByBnkName)
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
            var dialogEventNode = new HircTreeItem() { DisplayName = $"Dialog_Event {_nameLookUpHelper.GetName(item.Id)}", Item = item };
            parent.Children.Add(dialogEventNode);

            ProcessNode(hirc.AkDecisionTree.Root, dialogEventNode, item);
        }

        void ProcessNode(AkDecisionTree.Node node, HircTreeItem parent, HircItem owner)
        {
            if (node.IsAudioNode)
            {
                var dialogEventNode = new HircTreeItem() { DisplayName = $"Node {_nameLookUpHelper.GetName(node.Key)}", Item = owner };
                parent.Children.Add(dialogEventNode);
                ProcessNext(node.AudioNodeId, dialogEventNode);
            }
            else
            {
                var dialogEventNode = new HircTreeItem() { DisplayName = $"{_nameLookUpHelper.GetName(node.Key)}", Item = owner };
                parent.Children.Add(dialogEventNode);

                foreach (var child in node.Children)
                    ProcessNode(child, dialogEventNode, owner);
            }
        }

        void ProcessEvent(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkEvent>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Event {_nameLookUpHelper.GetName(item.Id)}", Item = item };
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
            var soundTreeNode = new HircTreeItem() { DisplayName = $"Sound {soundHirc.GetSourceId()}.wav", Item = item };
            parent.Children.Add(soundTreeNode);
        }

        private void ProcessActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<CAkActorMixer_v136>(item);
            var actorMixerNode = new HircTreeItem() { DisplayName = $"ActorMixer {_nameLookUpHelper.GetName(item.Id)}", Item = item };
            parent.Children.Add(actorMixerNode);

            ProcessNext(actorMixer.Children.ChildIdList, actorMixerNode);
        }

        void ProcessSwitchControl(HircItem item, HircTreeItem parent)
        {
            var switchControl = GetAsType<CAkSwitchCntr_v136>(item);
            var switchType = _nameLookUpHelper.GetName(switchControl.ulGroupID);
            var defaultValue = _nameLookUpHelper.GetName(switchControl.ulDefaultSwitch);
            var switchControlNode = new HircTreeItem() { DisplayName = $"Switch {switchType} DefaultValue: {defaultValue}", Item = item };
            parent.Children.Add(switchControlNode);

            foreach (var switchCase in switchControl.SwitchList)
            {
                var switchValue = _nameLookUpHelper.GetName(switchCase.SwitchId);
                var switchValueNode = new HircTreeItem() { DisplayName = $"SwitchValue: {switchValue}", Item = item };
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
