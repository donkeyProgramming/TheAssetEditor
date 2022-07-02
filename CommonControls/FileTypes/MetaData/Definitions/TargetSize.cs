using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    public class TargetSize : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "Likely an enum related to unit size. Maybe 0=large, 1=medium, 2=small, 3=very_large, 4=very_small")]
        public int MaxTargetSize { get; set; }

        [MetaDataTag(6)]
        public int Unk0 { get; set; }

        [MetaDataTag(7)]
        public int Unk1 { get; set; }
    }

    [MetaData("MAX_TARGET_SIZE", 10)]
    public class MaxTargetSize_v10 : TargetSize { }

    [MetaData("MIN_TARGET_SIZE", 10)]
    public class MinTargetSize_v10 : TargetSize { }


    [MetaData("MAX_TARGET_SIZE", 11)]
    public class MaxTargetSize_v11 : TargetSize
    {
        [MetaDataTag(8)]
        public int Unk2 { get; set; }
    }

    [MetaData("MIN_TARGET_SIZE", 11)]
    public class MinTargetSize_v11 : TargetSize 
    {
        [MetaDataTag(8)]
        public int Unk2 { get; set; }
    }

}
