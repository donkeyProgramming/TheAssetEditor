using Audio.AudioEditor;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using CommunityToolkit.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Audio.Utility
{
    public class FindAudioParentStructureHelper
    {
        public class ParentStructure
        {
            public string Description { get; set; }
            public List<GraphItem> GraphItems { get; set; } = new List<GraphItem>();

            public class GraphItem
            {
                public string Description { get; set; }
                public HircType Type { get; set; }
                public uint Id { get; set; }
            }
        }

        class BusItem
        {
            public string SourceDescription { get; set; }
            public uint BusId { get; set; }
        }

        public List<ParentStructure> Compute(HircItem sound, IAudioRepository audioRepository)
        {
            Guard.IsNotNull(sound);
            Guard.IsNotNull(sound as ICAkSound);
            Guard.IsNotNull(audioRepository);

            var output = new List<ParentStructure>();
            output.Add(GetAudioParentStructure(sound, audioRepository, out var overrideBusIds));
            output.AddRange(GetBusParentStructure(audioRepository, overrideBusIds));
            return output;
        }


        ParentStructure GetAudioParentStructure(HircItem sound, IAudioRepository audioRepository, out List<BusItem> busses)
        {
            busses = new List<BusItem>();
            var output = new ParentStructure()
            {
                Description = "Graph structure:"
            };

            var parser = new WWiseTreeParserParent(audioRepository, true, true, true);
            var nodes = parser.BuildHierarchyAsFlatList(sound);
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

                var graphItem = new ParentStructure.GraphItem()
                {
                    Description = $"{node.Item.Type}[{node.Item.Id}]{busInfo}",
                    Type = node.Item.Type,
                    Id = node.Item.Id,
                };

                output.GraphItems.Add(graphItem);
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

                    var graphItem = new ParentStructure.GraphItem()
                    {
                        Description = $"{name}[{item.Id}]",
                        Type = item.Type,
                        Id = item.Id,
                    };

                    output.Last().GraphItems.Add(graphItem);
                }
            }

            return output;
        }




    }

}
