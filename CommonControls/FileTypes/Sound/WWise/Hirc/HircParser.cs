using CommonControls.FileTypes.Sound.WWise;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonControls.FileTypes.Sound.WWise.Hirc
{
    public class HircFactory
    {
        Dictionary<HircType, Func<HircItem>> _itemList = new Dictionary<HircType, Func<HircItem>>();
        public void RegisterHirc(HircType type, Func<HircItem> creator)
        {
            _itemList[type] = creator;
        }

        public HircItem CreateInstance(HircType type)
        {
            if (_itemList.ContainsKey(type))
                return _itemList[type]();
            return new CAkUnknown();
        }

        public static HircFactory CreateFactory(uint version)
        {
            switch (version)
            {
                case 112: return CreateFactory_v112();
                case 122: return CreateFactory_v122();
                case 136: return CreateFactory_v136();
            }

            throw new Exception($"Unkown Version {version}");
        }

        public static HircFactory CreateFactory_v122()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(HircType.Sound, () => new V122.CAkSound_V122());
            instance.RegisterHirc(HircType.Event, () => new V122.CAkEvent_v122());
            instance.RegisterHirc(HircType.Action, () => new V122.CAkAction_V122());
            instance.RegisterHirc(HircType.SwitchContainer, () => new V122.CAkSwitchCntr_v122());
            instance.RegisterHirc(HircType.SequenceContainer, () => new V122.CAkRanSeqCnt_V122());
            instance.RegisterHirc(HircType.LayerContainer, () => new V122.CAkLayerCntr_v122());
            instance.RegisterHirc(HircType.Dialogue_Event, () => new V122.CAkDialogueEvent_v122());
            return instance;
        }

        public static HircFactory CreateFactory_v112()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(HircType.Sound, () => new V112.CAkSound_V112());
            instance.RegisterHirc(HircType.Event, () => new V112.CAkEvent_v112());
            instance.RegisterHirc(HircType.Action, () => new V112.CAkAction_v112());
            instance.RegisterHirc(HircType.SwitchContainer, () => new V112.CAkSwitchCntr_V112());
            instance.RegisterHirc(HircType.SequenceContainer, () => new V112.CAkRanSeqCnt_V112());
            instance.RegisterHirc(HircType.LayerContainer, () => new V112.CAkLayerCntr_v112());
            instance.RegisterHirc(HircType.Dialogue_Event, () => new V112.CAkDialogueEvent_v112());
            return instance;
        }

        public static HircFactory CreateFactory_v136()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(HircType.Sound, () => new V136.CAkSound_v136());
            instance.RegisterHirc(HircType.Event, () => new V136.CAkEvent_v136());
            instance.RegisterHirc(HircType.Action, () => new V136.CAkAction_v136());
            instance.RegisterHirc(HircType.SwitchContainer, () => new V136.CAkSwitchCntr_v136());
            instance.RegisterHirc(HircType.SequenceContainer, () => new V136.CAkRanSeqCntr_v136());
            instance.RegisterHirc(HircType.LayerContainer, () => new V136.CAkLayerCntr_v136());
            instance.RegisterHirc(HircType.Dialogue_Event, () => new V136.CAkDialogueEvent_v136());
            return instance;
        }

    }

    public class HircParser : IParser
    {

        public void Parse(string fileName, ByteChunk chunk, SoundDataBase soundDb)
        {
            var chunkSize = chunk.ReadUInt32();
            var numItems = chunk.ReadUInt32();
            var failedItems = new List<uint>();

            var factory = HircFactory.CreateFactory(soundDb.Header.dwBankGeneratorVersion);

            for (uint itemIndex = 0; itemIndex < numItems; itemIndex++)
            {
                var hircType = (HircType)chunk.PeakByte();

                var start = chunk.Index;
                try
                {
                    var hircItem = factory.CreateInstance(hircType);
                    hircItem.IndexInFile = itemIndex;
                    hircItem.OwnerFile = fileName;
                    hircItem.Parse(chunk);
                    soundDb.Hircs.Add(hircItem);
                }
                catch (Exception e)
                {
                    failedItems.Add(itemIndex);
                    chunk.Index = start;

                    var unkInstance = new CAkUnknown() { ErrorMsg = e.Message, IndexInFile = itemIndex, OwnerFile = fileName };
                    unkInstance.Parse(chunk);
                    soundDb.Hircs.Add(unkInstance);
                }
            }
        }
    }
}
