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
                public uint ID { get; set; }
            }
        }

        private class BusItem
        {
            public string SourceDescription { get; set; }
            public uint BusID { get; set; }
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

                if (node.Item is CAkActorMixer_V136 mixerInstance && mixerInstance.NodeBaseParams.OverrideBusID != 0)
                {
                    busInfo = $" - With AudioBus [{mixerInstance.NodeBaseParams.OverrideBusID}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.ID}]", BusID = mixerInstance.NodeBaseParams.OverrideBusID });
                }
                else if (node.Item is CAkSound_V136 soundInstance && soundInstance.NodeBaseParams.OverrideBusID != 0)
                {
                    busInfo = $" - With AudioBus [{soundInstance.NodeBaseParams.OverrideBusID}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.ID}]", BusID = soundInstance.NodeBaseParams.OverrideBusID });
                }
                else if (node.Item is CAkRanSeqCntr_V136 randInstance && randInstance.NodeBaseParams.OverrideBusID != 0)
                {
                    busInfo = $" - With AudioBus [{randInstance.NodeBaseParams.OverrideBusID}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.ID}]", BusID = randInstance.NodeBaseParams.OverrideBusID });
                }
                else if (node.Item is CAkLayerCntr_V136 layerInstance && layerInstance.NodeBaseParams.OverrideBusID != 0)
                {
                    busInfo = $" - With AudioBus [{layerInstance.NodeBaseParams.OverrideBusID}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.ID}]", BusID = layerInstance.NodeBaseParams.OverrideBusID });
                }
                else if (node.Item is CAkSwitchCntr_V136 switchInstance && switchInstance.NodeBaseParams.OverrideBusID != 0)
                {
                    busInfo = $" - With AudioBus [{switchInstance.NodeBaseParams.OverrideBusID}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.ID}]", BusID = switchInstance.NodeBaseParams.OverrideBusID });
                }

                var graphItem = new ParentStructure.GraphItem()
                {
                    Description = $"{node.Item.HircType}[{node.Item.ID}]{busInfo}",
                    Type = node.Item.HircType,
                    ID = node.Item.ID,
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

                var firstBus = audioRepository.GetHircObject(currentBusItem.BusID)
                    .Where(x => x.HircType == AkBkHircType.Audio_Bus)
                    .Cast<CAkBus_V136>()
                    .First();

                var item = firstBus;
                while (item.OverrideBusID != 0)
                {
                    item = audioRepository.GetHircObject(item.OverrideBusID)
                            .Where(x => x.HircType == AkBkHircType.Audio_Bus)
                            .Cast<CAkBus_V136>()
                            .First();

                    var name = audioRepository.GetNameFromID(item.ID, out var found);
                    if (found == false)
                        name = "";

                    var graphItem = new ParentStructure.GraphItem()
                    {
                        Description = $"{name}[{item.ID}]",
                        Type = item.HircType,
                        ID = item.ID,
                    };

                    output.Last().GraphItems.Add(graphItem);
                }
            }
            return output;
        }
    }
}
