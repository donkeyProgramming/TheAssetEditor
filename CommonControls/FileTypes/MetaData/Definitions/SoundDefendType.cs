using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SOUND_ATTACK_TYPE", 3)]
    public class SoundAttackType_v3 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt0_v3 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt1_v3 { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt2_v3 { get; set; }
    }
    
    [MetaData("SOUND_DEFEND_TYPE", 3)]
    public class SoundDefendType_v3 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt0_v3 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt1_v3 { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt2_v3 { get; set; }
    }
    
    
    [MetaData("SOUND_DEFEND_TYPE", 10)]
    public class SoundDefendType_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int UnknownInt0_v3 { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt1_v3 { get; set; }

        [MetaDataTag(7)]
        public int UnknownInt2_v3 { get; set; }
    }

    [MetaData("SOUND_DEFEND_TYPE", 11)]
    public class SoundDefendType_v11 : SoundDefendType_v10
    {
        [MetaDataTag(8)]
        public int UnknownInt0_v11 { get; set; }

    }
}
