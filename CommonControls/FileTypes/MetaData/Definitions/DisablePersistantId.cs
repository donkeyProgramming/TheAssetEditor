using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{


    [MetaData("DISABLE_PERSISTENT_ID", 10)]
    public class DisablePersistantId_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value{ get; set; }

        [MetaDataTag(6)]
        public float BlendInTime { get; set; }

        [MetaDataTag(7)]
        public float BlendOutTime { get; set; }
    }

    //[MetaData("DISABLE_PERSISTENT_ID", 11)]
    //public class DisablePersistantId_v11 : DecodedMetaEntryBase
    //{
    //    [MetaDataTag(5)]
    //    public float Value { get; set; }
    //
    //    [MetaDataTag(6)]
    //    public float BlendInTime { get; set; }
    //
    //    [MetaDataTag(7)]
    //    public float BlendOutTime { get; set; }
    //
    //    [MetaDataTag(8)]
    //    public bool PositionOnly { get; set; }
    //
    //    [MetaDataTag(9)]
    //    public bool Unk0 { get; set; }
    //    [MetaDataTag(10)]
    //    public bool Unk1 { get; set; }
    //    [MetaDataTag(11)]
    //    public bool Unk2 { get; set; }
    //}
}
