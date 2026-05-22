namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    // To find ActorMixer IDs click on a Sound in the Audio Explorer and refer to the lowest ActorMixer in the Graph (the top level Actor Mixer).

    // The way we play audio is a bit of a hack. We use ActorMixer IDs as the DirectParentId of target objects as it appears to make ActorMixers adopt the
    // target as a child and therefore function as if it really were its child. We expected this to cause the ActorMixer to no longer play some of its
    // actual children as the original Children list (and therefore the ActorMixer size) isn't updated for these new children, however surprisingly there
    // is no known bug caused by this. The expected issue could be something like some original children not playing, or even our audio not playing, but
    // none of that seems to happen, so, what Wwise doesn't know won't hurt it!

    // The alternative to this, and the original method tried before the DirectParentId hack discovery, is to make a new ActorMixer and match the settings
    // to that of the original top level Actor Mixer e.g. the same OverrideBusId, volume etc., or just give the target itself the OverrideBusId. We don't
    // do this because the DirectParentId hack is easier and requires making less stuff, and also because some ActorMixers seem to have some special
    // functionality, for example if you don't use CA's ActorMixer for movies, when you escape from the movie the audio continues to play so there must
    // be some game code muting the ActorMixer when escaping.
    public static class Wh3ActorMixerInformation
    {
        public const uint FrontendVO = 745637913;
        public const uint CampaignVO = 306196174;
        public const uint CampaignVOConversational = 652491101;
        public const uint BattleVOOrders = 1009314120;
        public const uint BattleVOConversational = 600762068;
        public const uint BattleAdvice = 142435894;
        public const uint CampaignAdvice = 517250292;
        public const uint BattleAbilities = 140075115;
        public const uint BattleMagic = 645285343;
        public const uint DiplomacyLines = 54848735;
        public const uint EventDilemmaNarration = 306196174;
        public const uint Movies = 573597124;
        public const uint QuestBattleSpeeches = 659413513;
    }
}
