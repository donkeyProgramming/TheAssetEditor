using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{


    public class TargetSize : MetaEntryBase
    {
        [MetaDataTag(5)]
        public int MaxTargetSize { get; set; }

        [MetaDataTag(6)]
        public int Unk0 { get; set; }

        [MetaDataTag(7)]
        public bool Unk1 { get; set; }
    }

    [MetaData("MAX_TARGET_SIZE", 10)]
    public class MaxTargetSize_v10 : TargetSize { }

    [MetaData("Min_TARGET_SIZE", 10)]
    public class MinTargetSize_v10 : TargetSize { }

}
