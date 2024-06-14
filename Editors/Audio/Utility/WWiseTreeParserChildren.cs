using Editors.Audio.Presentation.AudioExplorer;
using Editors.Audio.Storage;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Hirc;
using Shared.GameFormats.WWise.Hirc.V136;
using System;
using System.IO;
using System.Linq;

namespace Editors.Audio.Utility
{

    public class DialogEventInfoPrinter
    {
        private readonly IAudioRepository _repository;

        public DialogEventInfoPrinter(IAudioRepository repository)
        {
            _repository = repository;
        }

        public void PrintDialogEventInfos()
        {
            // Retrieve all HircItem instances from the repository.
            var allHircItems = _repository.GetAllOfType<HircItem>();

            // Filter those that are ICADialogEvent.
            var dialogEvents = allHircItems.OfType<ICADialogEvent>();

            foreach (var dialogEvent in dialogEvents)
            {
                PrintDialogEventInfo(dialogEvent);
            }
        }

        private void PrintDialogEventInfo(ICADialogEvent dialogEvent)
        {
            var hircItem = dialogEvent as HircItem; // Assuming HircItem is the base type with an Id
            if (hircItem == null)
            {
                throw new InvalidCastException("dialogEvent is not a HircItem.");
            }

            var helper = new DecisionPathHelper(_repository);
            var paths = helper.GetDecisionPaths(dialogEvent);

            // Splitting the string by '.' and enclosing each part in quotes
            var splitPaths = paths.Header.GetAsString().Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(part => $"\"{part}\"")
                              .ToArray();

            // Joining the quoted strings with a comma and a space, and enclosing the result in brackets
            var formattedPaths = "[" + string.Join(", ", splitPaths) + "]";

            // Format the information with quotes around the dialog event and the modified path string
            var info = $"\"{_repository.GetNameFromHash(hircItem.Id)}\" : {formattedPaths}";

            Console.WriteLine(info);
            var filePath = @"C:\Users\george\Desktop\dialogue_events_state_groups.txt";
            File.AppendAllText(filePath, info + Environment.NewLine);

        }
    }

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
            _hircProcessChildMap.Add(HircType.Music_Switch, ProcessMusicSwitch);
            _hircProcessChildMap.Add(HircType.Music_Random_Sequence, ProcessRandMusicContainer);
        }

        private void ProcessDialogEvent(HircItem item, HircTreeItem parent)
        {

            // Get all the dialogue event info
            //var printer = new DialogEventInfoPrinter(_repository);
            //printer.PrintDialogEventInfos();

            var hirc = GetAsType<ICADialogEvent>(item);

            var helper = new DecisionPathHelper(_repository);
            var paths = helper.GetDecisionPaths(hirc);

            var dialogEventNode = new HircTreeItem() { DisplayName = $"Dialog_Event {_repository.GetNameFromHash(item.Id)} - [{paths.Header.GetAsString()}]", Item = item };
            parent.Children.Add(dialogEventNode);

            foreach (var path in paths.Paths)
            {
                var pathNode = new HircTreeItem() { DisplayName = path.GetAsString(), Item = item, IsExpanded = false };
                dialogEventNode.Children.Add(pathNode);
                ProcessNext(path.ChildNodeId, pathNode);
            }
        }

        void ProcessEvent(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkEvent>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Event {_repository.GetNameFromHash(item.Id)}", Item = item };
            parent.Children.Add(actionTreeNode);

            var actions = actionHirc.GetActionIds();
            ProcessNext(actions, actionTreeNode);

            /*
            // Generate CSV of strings (triggered when an event is searched)
            var lines = File.ReadLines("C:\\Users\\george\\Desktop\\hirc_ids.csv");
            using (var file = File.CreateText("C:\\Users\\george\\Desktop\\hirc_names.csv"))
            foreach (var line in lines)
            {
                var name = _repository.GetNameFromHash(Convert.ToUInt32(line));
                file.WriteLine(string.Join(",", name));
            }
            */

        }

        void ProcessAction(HircItem item, HircTreeItem parent)
        {
            var actionHirc = GetAsType<ICAkAction>(item);
            var actionTreeNode = new HircTreeItem() { DisplayName = $"Action {actionHirc.GetActionType()}", Item = item };
            parent.Children.Add(actionTreeNode);
            var childId = actionHirc.GetChildId();

            // Override child id if type is setState based on parameters 
            if (actionHirc.GetActionType() == ActionType.SetState)
            {
                var stateGroupId = actionHirc.GetStateGroupId();
                var musicSwitches = _repository.HircObjects
                   .SelectMany(x => x.Value)
                   .Where(X => X.Type == HircType.Music_Switch)
                   .DistinctBy(x => x.Id)
                   .Cast<CAkMusicSwitchCntr_v136>()
                   .ToList();

                foreach (var musicSwitch in musicSwitches)
                {
                    var allArgs = musicSwitch.ArgumentList.Arguments.Select(x => x.ulGroupId).ToList();
                    if (allArgs.Contains(stateGroupId))
                        ProcessNext(musicSwitch.Id, actionTreeNode);
                }

                var normalSwitches = _repository.HircObjects
                   .SelectMany(x => x.Value)
                   .Where(X => X.Type == HircType.SwitchContainer)
                   .DistinctBy(x => x.Id)
                   .Cast<CAkSwitchCntr_v136>()
                   .ToList();

                foreach (var normalSwitch in normalSwitches)
                {
                    if (normalSwitch.ulGroupID == stateGroupId)
                        ProcessNext(normalSwitch.Id, actionTreeNode);
                }
            }
            else
            {

                ProcessNext(childId, actionTreeNode);
            }
        }

        private void ProcessSound(HircItem item, HircTreeItem parent)
        {
            var soundHirc = GetAsType<ICAkSound>(item);
            var soundTreeNode = new HircTreeItem() { DisplayName = $"Sound {soundHirc.GetSourceId()}.wem", Item = item };
            parent.Children.Add(soundTreeNode);
        }

        public void ProcessActorMixer(HircItem item, HircTreeItem parent)
        {
            var actorMixer = GetAsType<ICAkActorMixer>(item);
            var actorMixerNode = new HircTreeItem() { DisplayName = $"ActorMixer {_repository.GetNameFromHash(item.Id)}", Item = item };
            parent.Children.Add(actorMixerNode);

            ProcessNext(actorMixer.GetChildren(), actorMixerNode);
        }

        void ProcessSwitchControl(HircItem item, HircTreeItem parent)
        {
            var switchControl = GetAsType<ICAkSwitchCntr>(item);
            var switchType = _repository.GetNameFromHash(switchControl.GroupId);
            var defaultValue = _repository.GetNameFromHash(switchControl.DefaultSwitch);
            var switchControlNode = new HircTreeItem() { DisplayName = $"Switch {switchType} DefaultValue: {defaultValue}", Item = item };
            parent.Children.Add(switchControlNode);

            foreach (var switchCase in switchControl.SwitchList)
            {
                var switchValue = _repository.GetNameFromHash(switchCase.SwitchId);
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
            var layerContainer = GetAsType<CAkRanSeqCnt>(item);
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
            var hirc = GetAsType<CAkMusicSegment_v136>(item);
            var node = new HircTreeItem() { DisplayName = $"Music Segment", Item = item };
            parent.Children.Add(node);

            foreach (var childId in hirc.MusicNodeParams.Children.ChildIdList)
                ProcessNext(childId, node);
        }

        private void ProcessMusicSwitch(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<CAkMusicSwitchCntr_v136>(item);

            var helper = new DecisionPathHelper(_repository);
            var paths = helper.GetDecisionPaths(hirc);

            var dialogEventNode = new HircTreeItem() { DisplayName = $"Music Switch {_repository.GetNameFromHash(item.Id)} - [{paths.Header.GetAsString()}]", Item = item };
            parent.Children.Add(dialogEventNode);

            foreach (var path in paths.Paths)
            {
                var pathNode = new HircTreeItem() { DisplayName = path.GetAsString(), Item = hirc, IsExpanded = false };
                dialogEventNode.Children.Add(pathNode);
                ProcessNext(path.ChildNodeId, pathNode);
            }
        }

        private void ProcessRandMusicContainer(HircItem item, HircTreeItem parent)
        {
            var hirc = GetAsType<CAkMusicRanSeqCntr_v136>(item);
            var node = new HircTreeItem() { DisplayName = $"Music Rand Container", Item = item };
            parent.Children.Add(node);

            if (hirc.pPlayList.Any())
            {
                foreach (var playList in hirc.pPlayList.First().pPlayList)
                    ProcessNext(playList.SegmentID, node);
            }
        }
    }
}
