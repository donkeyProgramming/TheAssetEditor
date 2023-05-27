using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SOUND_TRIGGER", 2)]
    public class SoundTrigger_v2 : DecodedMetaEntryBaseOld
    {
        [MetaDataTag(4)]
        public int UnknownInt1 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt2 { get; set; }
    }
    
    [MetaData("SOUND_TRIGGER", 4)]
    public class SoundTrigger_v4 : DecodedMetaEntryBaseOld
    {
        [MetaDataTag(4)]
        public string SoundEvent { get; set; } = "";

        [MetaDataTag(5)]
        public int BoneIndex { get; set; }

        [MetaDataTag(6)]
        public Vector3 Position { get; set; }
    }

    [MetaData("SOUND_TRIGGER", 10)]
    public class SoundTrigger_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public string SoundEvent { get; set; } = "";

        [MetaDataTag(6)]
        public int BoneIndex { get; set; }

        [MetaDataTag(7)]
        public Vector3 Position { get; set; }
    }

    [MetaData("SOUND_TRIGGER", 11)]
    public  class SoundTrigger_v11 : SoundTrigger_v10
    {
        [MetaDataTag(8)]
        public string Unknown { get; set; } = "";
    }
    
    
    [MetaData("SOUND_BUILDING", 2)]
    public class SoundBuilding_v2 : DecodedMetaEntryBaseOld
    {
        [MetaDataTag(4)]
        public int UnknownInt1 { get; set; }

        [MetaDataTag(5)]
        public Vector3 Position { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt2 { get; set; }
    }
}
