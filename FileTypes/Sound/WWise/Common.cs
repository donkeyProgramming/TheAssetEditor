using Filetypes.ByteParsing;
using System;
using System.Text;

namespace FileTypes.Sound.WWise
{

    interface IParser
    {
        void Parse(ByteChunk chunk, SoundDataBase soundDb);
    }

    public enum HircType : byte
    {
        Sound = 0x02,
        Action = 0x03,
        Event = 0x04,
        SwitchContainer = 0x06,
        ActorMixer = 0x07
    }

    public enum ActionType : ushort
    {
        Play = 0x0403
    };

    public enum SourceType : ushort
    {
        Data_BNK = 0x00,
        PrefetchStreaming = 0x01,
        Straming = 0x02,
    }
}
