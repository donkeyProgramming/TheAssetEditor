using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("DISTANCE", 2)]
    public class Distance_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public float Value { get; set; }
    }
    
    [MetaData("DISTANCE", 10)]
    public class Distance_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }
}
