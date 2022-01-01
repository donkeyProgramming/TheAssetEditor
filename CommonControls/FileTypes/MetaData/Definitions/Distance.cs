using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("DISTANCE", 10)]
    public class Distance_v10 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }
}
