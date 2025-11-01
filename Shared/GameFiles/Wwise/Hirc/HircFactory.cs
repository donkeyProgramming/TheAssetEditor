using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc
{
    public class HircFactory
    {
        private readonly Dictionary<AkBkHircType, Func<HircItem>> _itemList = [];

        public void RegisterHirc(AkBkHircType type, Func<HircItem> creator)
        {
            _itemList[type] = creator;
        }

        public HircItem CreateInstance(AkBkHircType type)
        {
            if (_itemList.TryGetValue(type, out var functor))
                return functor();

            return new UnknownHircItem();
        }

        // Each major release of Wwise has a bank generator version
        // CA sometimes use an in-house compiled version of Wwise which is based on a public release with custom modifications to some Wwise objects
        // The bank generator version of the closest public release (2019.2.15.7667) to that used in Wh3 (2147483784) is 135
        // Wwiser adds 1 to that for internal use to create a pseudo version called 136 but really it's 2147483784
        public static HircFactory CreateFactory(uint bankGeneratorVersion)
        {
            return bankGeneratorVersion switch
            {
                112 => CreateFactory_v112(),
                135 => CreateFactory_v136(),
                2147483784 => CreateFactory_v136(),
                _ => throw new Exception($"Unknown Bank Generator Version: {bankGeneratorVersion}"),
            };
        }

        private static HircFactory CreateFactory_v112()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(AkBkHircType.ActorMixer, () => new V112.CAkActorMixer_V112());
            instance.RegisterHirc(AkBkHircType.Sound, () => new V112.CAkSound_V112());
            instance.RegisterHirc(AkBkHircType.Event, () => new V112.CAkEvent_V112());
            instance.RegisterHirc(AkBkHircType.Action, () => new V112.CAkAction_V112());
            instance.RegisterHirc(AkBkHircType.SwitchContainer, () => new V112.CAkSwitchCntr_V112());
            instance.RegisterHirc(AkBkHircType.RandomSequenceContainer, () => new V112.CAkRanSeqCntr_V112());
            instance.RegisterHirc(AkBkHircType.LayerContainer, () => new V112.CAkLayerCntr_V112());
            instance.RegisterHirc(AkBkHircType.Dialogue_Event, () => new V112.CAkDialogueEvent_V112());
            return instance;
        }

        private static HircFactory CreateFactory_v136()
        {
            var instance = new HircFactory();
            instance.RegisterHirc(AkBkHircType.ActorMixer, () => new V136.CAkActorMixer_V136());
            instance.RegisterHirc(AkBkHircType.Sound, () => new V136.CAkSound_V136());
            instance.RegisterHirc(AkBkHircType.Event, () => new V136.CAkEvent_V136());
            instance.RegisterHirc(AkBkHircType.Action, () => new V136.CAkAction_V136());
            instance.RegisterHirc(AkBkHircType.SwitchContainer, () => new V136.CAkSwitchCntr_V136());
            instance.RegisterHirc(AkBkHircType.RandomSequenceContainer, () => new V136.CAkRanSeqCntr_V136());
            instance.RegisterHirc(AkBkHircType.LayerContainer, () => new V136.CAkLayerCntr_V136());
            instance.RegisterHirc(AkBkHircType.Dialogue_Event, () => new V136.CAkDialogueEvent_V136());
            instance.RegisterHirc(AkBkHircType.Music_Track, () => new V136.CAkMusicTrack_V136());
            instance.RegisterHirc(AkBkHircType.Music_Segment, () => new V136.CAkMusicSegment_V136());
            instance.RegisterHirc(AkBkHircType.Music_Random_Sequence, () => new V136.CAkMusicRanSeqCntr_V136());
            instance.RegisterHirc(AkBkHircType.Music_Switch, () => new V136.CAkMusicSwitchCntr_V136());
            instance.RegisterHirc(AkBkHircType.FxCustom, () => new V136.CAkFxCustom_V136());
            instance.RegisterHirc(AkBkHircType.FxShareSet, () => new V136.CAkFxShareSet_V136());
            instance.RegisterHirc(AkBkHircType.Audio_Bus, () => new V136.CAkBus_V136());
            return instance;
        }
    }
}
