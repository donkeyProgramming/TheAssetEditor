using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileTypes.Sound.WWise.Hirc
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
            if(_itemList.ContainsKey(type))
                return _itemList[type]();
            return new CAkUnknown();
        }

        public static HircFactory CreateFactory(uint version)
        {
            switch (version)
            {
                case 112: return CreateFactory_v112();
                case 122: return CreateFactory_v122();
            }

            throw new Exception("Unkown Version");
        }

        public static HircFactory CreateFactory_v112()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(HircType.Sound, () => new V122.CAkSound());
            instance.RegisterHirc(HircType.Event, () => new V122.CAkEvent_v122());
            instance.RegisterHirc(HircType.Action, () => new V122.CAkAction());
            instance.RegisterHirc(HircType.SwitchContainer, () => new V122.CAkSwitchCntr());
            instance.RegisterHirc(HircType.SequenceContainer, () => new V122.CAkRanSeqCnt());
            instance.RegisterHirc(HircType.LayerContainer, () => new V122.CAkLayerCntr());
            instance.RegisterHirc(HircType.Dialogue_Event, () => new V122.CAkDialogueEvent());
            return instance;
        }

        public static HircFactory CreateFactory_v122()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(HircType.Sound, () => new V122.CAkSound());
            instance.RegisterHirc(HircType.Event, () => new V122.CAkEvent_v122());
            instance.RegisterHirc(HircType.Action, () => new V122.CAkAction());
            instance.RegisterHirc(HircType.SwitchContainer, () => new V122.CAkSwitchCntr());
            instance.RegisterHirc(HircType.SequenceContainer, () => new V122.CAkRanSeqCnt());
            instance.RegisterHirc(HircType.LayerContainer, () => new V122.CAkLayerCntr());
            instance.RegisterHirc(HircType.Dialogue_Event, () => new V122.CAkDialogueEvent());
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

            for (uint i = 0; i < numItems; i++)
            {
                var hircType = (HircType)chunk.PeakByte();

                var start = chunk.Index;
                try
                {
                    var hircItem = factory.CreateInstance(hircType);
                    hircItem.Parse(chunk);
                    soundDb.Hircs.Add(hircItem);
                }
                catch (Exception e)
                {
                    failedItems.Add(i);
                    chunk.Index = start;

                    var unkInstance = new CAkUnknown() { ErrorMsg = e.Message};
                    unkInstance.Parse(chunk);
                    soundDb.Hircs.Add(unkInstance);
                }

                soundDb.Hircs.Last().IndexInFile = i;
                soundDb.Hircs.Last().OwnerFile = fileName;
            }
        }
    }
}
