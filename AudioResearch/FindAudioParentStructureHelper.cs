using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using Audio.FileFormats.WWise;
using Audio.AudioEditor;

namespace AudioResearch
{
    partial class Program
    {
        public class FindAudioParentStructureHelper
        {
            List<CAkAction_v136> _allActions;
            List<CAkEvent_v136> _AllEvents;
            public FindAudioParentStructureHelper(IAudioRepository audioRepository)
            {
                _allActions = audioRepository.HircObjects
                    .SelectMany(x => x.Value)
                    .DistinctBy(x => x.Id)
                    .Where(x => x.Type == HircType.Action)
                    .Cast<CAkAction_v136>()
                    .Where(x => x.ActionType == ActionType.Play)
                    .DistinctBy(x => x.idExt)
                    .ToList();


                _AllEvents = audioRepository.HircObjects
                    .SelectMany(x => x.Value)
                    .DistinctBy(x => x.Id)
                    .Where(x => x.Type == HircType.Event)
                    .Cast<CAkEvent_v136>()
                    .ToList();
            }

            public class ParentStructure
            { 
                public string Description { get; set; }
                public List<string> GraphItems { get; set; } = new List<string>();
            }

            class BusItem
            { 
                public string SourceDescription { get; set; }
                public uint BusId { get; set; }
            }

            public void DebugPrint(List<ParentStructure> parentStructures)
            {
                foreach(var  parentStructure in parentStructures) 
                {
                    Console.WriteLine(parentStructure.Description);
                    foreach(var item in  parentStructure.GraphItems)
                        Console.WriteLine("\t"+ item);
                    Console.WriteLine();
                }
            }


            string GetEventNameForSound(List<HircTreeItem> soundHierarchyAsFlatList, IAudioRepository audioRepository)
            {
                foreach (var node in soundHierarchyAsFlatList)
                {
                    uint itemId = 0;
                    if (node.Item is CAkSound_v136)
                        itemId = node.Item.Id;

                    else if (node.Item is CAkRanSeqCntr_v136)
                        itemId = node.Item.Id;

                    else if (node.Item is CAkLayerCntr_v136)
                        itemId = node.Item.Id;

                    else if (node.Item is CAkSwitchCntr_v136 )
                        itemId = node.Item.Id;






                    // Handle music,

                    if (itemId != 0)
                    {
                        var eventName = GetEventName(itemId, audioRepository, out var found);
                        if (found == true)
                            return eventName;
                        else
                        { 
                        
                        }

                    }
                }

                return "test";
            }

            string GetEventName(uint idBeforeAction, IAudioRepository audioRepository, out bool found)
            {
                found = false;
                var actionParents = _allActions
                    .Where(x => x.idExt == idBeforeAction)
                    .ToList();

                if (actionParents.Count() != 1)
                {
                    Console.WriteLine("actionParents.Count() != 1 : " + actionParents.Count());

                    //var act = audioRepository.HircObjects
                    //    .SelectMany(x => x.Value)
                    //    .DistinctBy(x => x.Id)
                    //    .Where(x => x.Type == HircType.Action)
                    //    .Cast<CAkAction_v136>()
                    //    .Where(x => x.idExt == idBeforeAction)
                    //    .ToList();

                }


                if (actionParents.Count() == 1)
                {
                    var parentEvent = _AllEvents
                        .Where(x => x.Actions.Select(a => a.ActionId).Contains(actionParents.First().Id))
                        .ToList();

                    if (parentEvent.Count() != 1)
                    {
                        Console.WriteLine("parentEvent.Count() != 1 : " + parentEvent.Count());
                    }

                    found = true;
                    return audioRepository.GetNameFromHash(parentEvent.First().Id, out var nameFound);
                }

                Console.WriteLine("Not Found");
                return "not found";
            }

            ParentStructure GetAudioParentStructure(CAkSound_v136 sound, IAudioRepository audioRepository, out List<BusItem> busses)
            {
                busses = new List<BusItem>();
                var output = new ParentStructure()
                {
                    Description = "Graph structure:"
                };

                var parser = new WWiseTreeParserParent(audioRepository, true, true, true);
                var nodes = parser.BuildHierarchyAsFlatList(sound);

                GetEventNameForSound(nodes, audioRepository);
                return output;
                nodes.Reverse();

                foreach (var node in nodes)
                {
                    var busInfo = "";
                    if (node.Item is CAkActorMixer_v136 mixerInstance && mixerInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{mixerInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = mixerInstance.NodeBaseParams.OverrideBusId });
                    }

                    else if (node.Item is CAkSound_v136 soundInstance && soundInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{soundInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = soundInstance.NodeBaseParams.OverrideBusId });
                    }

                    else if (node.Item is CAkRanSeqCntr_v136 randInstance && randInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{randInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = randInstance.NodeBaseParams.OverrideBusId });
                    }

                    else if (node.Item is CAkLayerCntr_v136 layerInstance && layerInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{layerInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = layerInstance.NodeBaseParams.OverrideBusId });
                    }

                    else if (node.Item is CAkSwitchCntr_v136 switchInstance && switchInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{switchInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = switchInstance.NodeBaseParams.OverrideBusId });
                    }

                    var str = $"{node.Item.Type}[{node.Item.Id}]{busInfo}";
                    output.GraphItems.Add(str);
                }

                return output;


            }

            List<ParentStructure> GetBusParentStructure(IAudioRepository audioRepository, List<BusItem> busItems)
            {
                var output = new List<ParentStructure>();

                foreach (var currentBusItem in busItems)
                {
                    output.Add(new ParentStructure() { Description = $"AudioBus graph for {currentBusItem.SourceDescription}:" });

                    var firstBus = audioRepository.GetHircObject(currentBusItem.BusId)
                        .Where(x => x.Type == HircType.Audio_Bus)
                        .Cast<CAkBus_v136>()
                        .First();

                    var item = firstBus;
                    while (item.OverrideBusId != 0)
                    {
                        item = audioRepository.GetHircObject(item.OverrideBusId)
                                .Where(x => x.Type == HircType.Audio_Bus)
                                .Cast<CAkBus_v136>()
                                .First();

                        var name = audioRepository.GetNameFromHash(item.Id, out var found);
                        if (found == false)
                            name = "";
                        var str = $"{name}[{item.Id}]";
                        output.Last().GraphItems.Add(str);
                    }
                }
            
                return output;
            }



            public List<ParentStructure> Compute(CAkSound_v136 sound, IAudioRepository audioRepository)
            {
                var output = new List<ParentStructure>();
                output.Add(GetAudioParentStructure(sound, audioRepository, out var overrideBusIds));
                //output.AddRange(GetBusParentStructure(audioRepository, overrideBusIds));

                //DebugPrint(output);
                return output;
             }
        }
    }
}
