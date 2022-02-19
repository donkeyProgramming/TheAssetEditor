using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SOUND_DEFEND_TYPE", 10)]
    public class SoundDefendType_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int Unk0 { get; set; }

        [MetaDataTag(6)]
        public int Unk1 { get; set; }

        [MetaDataTag(7)]
        public int Unk2 { get; set; }

    }

    [MetaData("SOUND_DEFEND_TYPE", 11)]
    public class SoundDefendType_v11 : SoundDefendType_v10
    {
        [MetaDataTag(8)]
        public int Unk3 { get; set; }

    }
}
