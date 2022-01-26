using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("ALLOWED_DELTA_SCALE", 10)]
    public class AllowDeltaScale_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Value { get; set; }
    }
}
