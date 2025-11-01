using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.Shared.Wwise.HircExploration
{
    public class SoundParentStructureParser
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

        public static List<ParentStructure> Compute(HircItem sound, IAudioRepository audioRepository)
        {
            Guard.IsNotNull(sound);
            Guard.IsNotNull(sound as ICAkSound);
            Guard.IsNotNull(audioRepository);

            var output = new List<ParentStructure>();
            output.Add(GetAudioParentStructure(sound, audioRepository, out var overrideBusIds));
            output.AddRange(GetBusParentStructure(audioRepository, overrideBusIds));
            return output;
        }

        private static ParentStructure GetAudioParentStructure(HircItem sound, IAudioRepository audioRepository, out List<BusItem> busses)
        {
            busses = new List<BusItem>();
            var output = new ParentStructure()
            {
                Description = "Graph structure:"
            };

            var parser = new HircTreeParentParser(audioRepository);
            var nodes = parser.BuildHierarchyAsFlatList(sound);
            nodes.Reverse();

            foreach (var node in nodes)
            {
                var busInfo = "";

                if (node.Item is CAkActorMixer_V136 mixerInstance && mixerInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With Audio Bus [{mixerInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = mixerInstance.NodeBaseParams.OverrideBusId });
                }
                else if (node.Item is CAkSound_V136 soundInstance && soundInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With Audio Bus [{soundInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = soundInstance.NodeBaseParams.OverrideBusId });
                }
                else if (node.Item is CAkRanSeqCntr_V136 randInstance && randInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With Audio Bus [{randInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = randInstance.NodeBaseParams.OverrideBusId });
                }
                else if (node.Item is CAkLayerCntr_V136 layerInstance && layerInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With Audio Bus [{layerInstance.NodeBaseParams.OverrideBusId}]";
                    busses.Add(new BusItem() { SourceDescription = $"{node.Item.HircType}[{node.Item.Id}]", BusId = layerInstance.NodeBaseParams.OverrideBusId });
                }
                else if (node.Item is CAkSwitchCntr_V136 switchInstance && switchInstance.NodeBaseParams.OverrideBusId != 0)
                {
                    busInfo = $" - With Audio Bus [{switchInstance.NodeBaseParams.OverrideBusId}]";
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
                output.Add(new ParentStructure() { Description = $"Audio Bus graph for {currentBusItem.SourceDescription}:" });

                var firstBus = audioRepository.GetHircs(currentBusItem.BusId)
                    .Where(x => x.HircType == AkBkHircType.Audio_Bus)
                    .Cast<CAkBus_V136>()
                    .First();

                var item = firstBus;
                while (item.OverrideBusId != 0)
                {
                    item = audioRepository.GetHircs(item.OverrideBusId)
                            .Where(x => x.HircType == AkBkHircType.Audio_Bus)
                            .Cast<CAkBus_V136>()
                            .First();

                    var name = audioRepository.GetNameFromId(item.Id, out var found);
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
