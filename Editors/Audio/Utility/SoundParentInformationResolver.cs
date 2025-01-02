using CommunityToolkit.Diagnostics;
using Editors.Audio.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.Utility
{
    public class FindAudioParentStructureHelper
    {
        public class ParentStructure
        {
            public string Description { get; set; }
            public List<GraphItem> GraphItems { get; set; } = [];

            public class GraphItem
            {
                public string Description { get; set; }
                public AkBkHircType Type { get; set; }
                public uint Id { get; set; }
            }
        }

        private class BusItem
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

        private ParentStructure GetAudioParentStructure(HircItem sound, IAudioRepository audioRepository, out List<BusItem> busses)
        {
            busses = new List<BusItem>();
            var output = new ParentStructure()
            {
                Description = "Graph structure:"
            };

            var parser = new WwiseTreeParserParent(audioRepository, true, true, true);
            var nodes = parser.BuildHierarchyAsFlatList(sound);
            nodes.Reverse();

            foreach (var node in nodes)
            {
                var busInfo = "";

                if (node.Item is CAkActorMixer_V136TEMP mixerInstance && mixerInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With AudioBus [{mixerInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = mixerInstance.NodeBaseParams.OverrideBusId });
                }
                else if (node.Item is CAkSound_V136TEMP soundInstance && soundInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With AudioBus [{soundInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = soundInstance.NodeBaseParams.OverrideBusId });
                }
                else if (node.Item is CAkRanSeqCntr_V136TEMP randInstance && randInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With AudioBus [{randInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = randInstance.NodeBaseParams.OverrideBusId });
                }
                else if (node.Item is CAkLayerCntr_V136TEMP layerInstance && layerInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With AudioBus [{layerInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = layerInstance.NodeBaseParams.OverrideBusId });
                }
                else if (node.Item is CAkSwitchCntr_V136TEMP switchInstance && switchInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With AudioBus [{switchInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = switchInstance.NodeBaseParams.OverrideBusId });
                }

                var graphItem = new ParentStructure.GraphItem()
                {
                    Description = $"{node.Item.HircType}[{node.Item.Id}]{busInfo}",
                    Type = node.Item.HircType,
                    Id = node.Item.Id,
                };
                output.GraphItems.Add(graphItem);
            }

            return output;
        }

        private static List<ParentStructure> GetBusParentStructure(IAudioRepository audioRepository, List<BusItem> busItems)
        {
            var output = new List<ParentStructure>();

            foreach (var currentBusItem in busItems)
            {
                output.Add(new ParentStructure() { Description = $"AudioBus graph for {currentBusItem.SourceDescription}:" });

                var firstBus = audioRepository.GetHircObject(currentBusItem.BusId)
                    .Where(x => x.HircType == AkBkHircType.Audio_Bus)
                    .Cast<CAkBus_V136TEMP>()
                    .First();

                var item = firstBus;
                while (item.OverrideBusId != 0)
                {
                    item = audioRepository.GetHircObject(item.OverrideBusId)
                            .Where(x => x.HircType == AkBkHircType.Audio_Bus)
                            .Cast<CAkBus_V136TEMP>()
                            .First();

                    var name = audioRepository.GetNameFromHash(item.Id, out var found);
                    if (found == false)
                        name = "";

                    var graphItem = new ParentStructure.GraphItem()
                    {
                        Description = $"{name}[{item.Id}]",
                        Type = item.HircType,
                        Id = item.Id,
                    };

                    output.Last().GraphItems.Add(graphItem);
                }
            }
            return output;
        }
    }
}
