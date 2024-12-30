namespace Shared.GameFormats.Wwise.Hirc
{
    public class HircFactory
    {
        private readonly Dictionary<HircType, Func<HircItem>> _itemList = [];

        public void RegisterHirc(HircType type, Func<HircItem> creator)
        {
            _itemList[type] = creator;
        }

        public HircItem CreateInstance(HircType type)
        {
            if (_itemList.TryGetValue(type, out var functor))
                return functor();

            return new CAkUnknown();
        }

        // Each major release of Wwise has a bank generator version.
        // CA sometimes use in-house compiled version of Wwise which is based on a public release with custom modifications to some Wwise objects.
        // The bank generator version of the closest public release (2019.2.15.7667) to that used in Wh3 (2147483784) is 135.
        // Wwiser adds 1 to that for internal use to create a pseudo version called 136 but really it's 2147483784.
        public static HircFactory CreateFactory(uint bankGeneratorVersion)
        {
            switch (bankGeneratorVersion)
            {
                case 112: return CreateFactory_v112();
                case 2147483784: return CreateFactory_v136();
            }

            throw new Exception($"Unknown Bank Generator Version: {bankGeneratorVersion}");
        }

        private static HircFactory CreateFactory_v112()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(HircType.ActorMixer, () => new V112.CAkActorMixer_v112());
            instance.RegisterHirc(HircType.Sound, () => new V112.CAkSound_V112());
            instance.RegisterHirc(HircType.Event, () => new V112.CAkEvent_v112());
            instance.RegisterHirc(HircType.Action, () => new V112.CAkAction_v112());
            instance.RegisterHirc(HircType.SwitchContainer, () => new V112.CAkSwitchCntr_V112());
            instance.RegisterHirc(HircType.SequenceContainer, () => new V112.CAkRanSeqCnt_V112());
            instance.RegisterHirc(HircType.LayerContainer, () => new V112.CAkLayerCntr_v112());
            instance.RegisterHirc(HircType.Dialogue_Event, () => new V112.CAkDialogueEvent_v112());
            return instance;
        }

        private static HircFactory CreateFactory_v136()
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
    }
}
