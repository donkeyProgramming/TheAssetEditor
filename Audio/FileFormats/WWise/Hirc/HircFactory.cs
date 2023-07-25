using System;
using System.Collections.Generic;

namespace Audio.FileFormats.WWise.Hirc
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
            if (_itemList.TryGetValue(type, out var functor))
                return functor();

            else if (type == HircType.Audio_Bus)
            { }
            else if (type == HircType.FxCustom)
            { }

            return new CAkUnknown();
        }

        public static HircFactory CreateFactory(uint version)
        {
            switch (version)
            {
                case 112: return CreateFactory_v112();  // Atilla
                case 122: return CreateFactory_v122();
                case 136: return CreateFactory_v136();  // Wh3
            }

            throw new Exception($"Unknown Version {version}");
        }

        static HircFactory CreateFactory_v122()
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

        static HircFactory CreateFactory_v112()
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

        static HircFactory CreateFactory_v136()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(HircType.ActorMixer, () => new V136.CAkActorMixer_v136());
            instance.RegisterHirc(HircType.Sound, () => new V136.CAkSound_v136());
            instance.RegisterHirc(HircType.Event, () => new V136.CAkEvent_v136());
            instance.RegisterHirc(HircType.Action, () => new V136.CAkAction_v136());
            instance.RegisterHirc(HircType.SwitchContainer, () => new V136.CAkSwitchCntr_v136());
            instance.RegisterHirc(HircType.SequenceContainer, () => new V136.CAkRanSeqCntr_v136());
            instance.RegisterHirc(HircType.LayerContainer, () => new V136.CAkLayerCntr_v136());
            instance.RegisterHirc(HircType.Dialogue_Event, () => new V136.CAkDialogueEvent_v136());
            instance.RegisterHirc(HircType.Music_Track, () => new V136.CAkMusicTrack_v136());
            instance.RegisterHirc(HircType.Music_Segment, () => new V136.CAkMusicSegment_v136());
            instance.RegisterHirc(HircType.Music_Random_Sequence, () => new V136.CAkMusicRanSeqCntr_v136());
            instance.RegisterHirc(HircType.Music_Switch, () => new V136.CAkMusicSwitchCntr_v136());
            instance.RegisterHirc(HircType.FxCustom, () => new V136.CAkFxCustom_v136());
            instance.RegisterHirc(HircType.FxShareSet, () => new V136.CAkFxShareSet_v136());
            instance.RegisterHirc(HircType.Audio_Bus, () => new V136.CAkBus_v136());
            instance.RegisterHirc(HircType.AuxiliaryBus, () => new V136.CAkAuxBus_v136());
            return instance;
        }

        public static HircFactory CreateByteHircFactory()
        {
            var instance = new HircFactory();
            var hircTypes = Enum.GetValues(typeof(HircType)) as HircType[];
            foreach (var hircType in hircTypes)
                instance.RegisterHirc(hircType, () => new ByteHirc());
            return instance;
        }
    }
}
