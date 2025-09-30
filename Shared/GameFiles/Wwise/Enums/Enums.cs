namespace Shared.GameFormats.Wwise.Enums
{
    public static class BankChunkTypes
    {
        // There are other chunk types but these are the ones we care about. 
        // For more info on chunk types see: https://github.com/bnnm/wwiser/blob/master/doc/WWISER.md
        public const string BKHD = "BKHD"; // The BKHD (Bank Header) section contains the Bank version and some global config stuff like Bank name.
        public const string HIRC = "HIRC"; // The HIRC (Hierarchy) section contains most Wwise objects.
        public const string STID = "STID"; // The STID (String Table ID) section contains a list of all project Banks referenced in the HIRC section (including the current SoundBank) with info on their IDs and file names. 
        public const string DIDX = "DIDX"; // The DIDX (Data Index) section contains the references to the .wem files embedded in the SoundBank.
        public const string DATA = "DATA"; // The DATA section contains the .wem files data, not encoded, and immediately following each other or alternatively pre-fetch data for streaming.
        public const string INIT = "INIT"; // The INIT (Initialisation) section contains the game's global config such as busses, states, plugins etc.
    }

    // TODO: need to account for the different hirc types in different versions, hmm
    public enum AkBkHircType : byte
    {
        None = 0x00,
        State = 0x01,
        Sound = 0x02,
        Action = 0x03,
        Event = 0x04,
        RandomSequenceContainer = 0x05,
        SwitchContainer = 0x06,
        ActorMixer = 0x07,
        Audio_Bus = 0x08,
        LayerContainer = 0x09,
        Music_Segment = 0x0a,
        Music_Track = 0x0b,
        Music_Switch = 0x0c,
        Music_Random_Sequence = 0x0d,
        Attenuation = 0x0e,
        Dialogue_Event = 0x0f,
        FxShareSet = 0x10,
        FxCustom = 0x11,
        AuxiliaryBus = 0x12,
        LFO = 0x13,
        Envelope = 0x14,
        AudioDevice = 0x15,
        TimeMod = 0x16,
        Didx_Audio = 0x17
    }

    public enum AkActionType : ushort
    {
        None = 0x0000,
        SetState = 0x1204,
        BypassFX_M = 0x1A02,
        BypassFX_O = 0x1A03,
        ResetBypassFX_M = 0x1B02,
        ResetBypassFX_O = 0x1B03,
        ResetBypassFX_ALL = 0x1B04,
        ResetBypassFX_ALL_O = 0x1B05,
        ResetBypassFX_AE = 0x1B08,
        ResetBypassFX_AE_O = 0x1B09,
        SetSwitch = 0x1901,
        UseState_E = 0x1002,
        UnuseState_E = 0x1102,
        Play = 0x0403,
        PlayAndContinue = 0x0503,
        Stop_E = 0x0102,
        Stop_E_O = 0x0103,
        Stop_ALL = 0x0104,
        Stop_ALL_O = 0x0105,
        Stop_AE = 0x0108,
        Stop_AE_O = 0x0109,
        Pause_E = 0x0202,
        Pause_E_O = 0x0203,
        Pause_ALL = 0x0204,
        Pause_ALL_O = 0x0205,
        Pause_AE = 0x0208,
        Pause_AE_O = 0x0209,
        Resume_E = 0x0302,
        Resume_E_O = 0x0303,
        Resume_ALL = 0x0304,
        Resume_ALL_O = 0x0305,
        Resume_AE = 0x0308,
        Resume_AE_O = 0x0309,
        Break_E = 0x1C02,
        Break_E_O = 0x1C03,
        Mute_M = 0x0602,
        Mute_O = 0x0603,
        Unmute_M = 0x0702,
        Unmute_O = 0x0703,
        Unmute_ALL = 0x0704,
        Unmute_ALL_O = 0x0705,
        Unmute_AE = 0x0708,
        Unmute_AE_O = 0x0709,
        SetVolume_M = 0x0A02,
        SetVolume_O = 0x0A03,
        ResetVolume_M = 0x0B02,
        ResetVolume_O = 0x0B03,
        ResetVolume_ALL = 0x0B04,
        ResetVolume_ALL_O = 0x0B05,
        ResetVolume_AE = 0x0B08,
        ResetVolume_AE_O = 0x0B09,
        SetPitch_M = 0x0802,
        SetPitch_O = 0x0803,
        ResetPitch_M = 0x0902,
        ResetPitch_O = 0x0903,
        ResetPitch_ALL = 0x0904,
        ResetPitch_ALL_O = 0x0905,
        ResetPitch_AE = 0x0908,
        ResetPitch_AE_O = 0x0909,
        SetLPF_M = 0x0E02,
        SetLPF_O = 0x0E03,
        ResetLPF_M = 0x0F02,
        ResetLPF_O = 0x0F03,
        ResetLPF_ALL = 0x0F04,
        ResetLPF_ALL_O = 0x0F05,
        ResetLPF_AE = 0x0F08,
        ResetLPF_AE_O = 0x0F09,
        SetHPF_M = 0x2002,
        SetHPF_O = 0x2003,
        ResetHPF_M = 0x3002,
        ResetHPF_O = 0x3003,
        ResetHPF_ALL = 0x3004,
        ResetHPF_ALL_O = 0x3005,
        ResetHPF_AE = 0x3008,
        ResetHPF_AE_O = 0x3009,
        SetBusVolume_M = 0x0C02,
        SetBusVolume_O = 0x0C03,
        ResetBusVolume_M = 0x0D02,
        ResetBusVolume_O = 0x0D03,
        ResetBusVolume_ALL = 0x0D04,
        ResetBusVolume_AE = 0x0D08,
        PlayEvent = 0x2103,
        StopEvent = 0x1511,
        PauseEvent = 0x1611,
        ResumeEvent = 0x1711,
        Duck = 0x1820,
        Trigger = 0x1D00,
        Trigger_O = 0x1D01,
        Trigger_E = 0x1D02,
        Trigger_E_O = 0x1D03,
        Seek_E = 0x1E02,
        Seek_E_O = 0x1E03,
        Seek_ALL = 0x1E04,
        Seek_ALL_O = 0x1E05,
        Seek_AE = 0x1E08,
        Seek_AE_O = 0x1E09,
        ResetPlaylist_E = 0x2202,
        ResetPlaylist_E_O = 0x2203,
        SetGameParameter = 0x1302,
        SetGameParameter_O = 0x1303,
        ResetGameParameter = 0x1402,
        ResetGameParameter_O = 0x1403,
        Release = 0x1F02,
        Release_O = 0x1F03,
    };

    public enum AKBKSourceType : ushort
    {
        Data_BNK = 0x00,
        PrefetchStreaming = 0x01,
        Streaming = 0x02,
    }

    public enum AkGroupType : byte
    {
        Switch = 0x00,
        State = 0x01,
    }

    public enum AkMode : byte
    {
        BestMatch = 0x00
    }

    public enum AkRtpcType : byte
    {
        GameParameter = 0x00,
        Modulator = 0x01,
        MIDIParameter = 0x01,
    }

    public enum AkTransitionMode : byte
    {
        Disabled = 0x00,
        CrossFadeAmp = 0x01,
        CrossFadePower = 0x02,
        Delay = 0x03,
        SampleAccurate = 0x04,
        TriggerRate = 0x05,
    }

    public enum AkRandomMode : byte
    {
        Normal = 0x00,
        Shuffle = 0x01,
    }

    public enum AkContainerMode : byte
    {
        Random = 0x00,
        Sequence = 0x01,
    }
}
