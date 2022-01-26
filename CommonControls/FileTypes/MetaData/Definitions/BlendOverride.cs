using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("BLEND_OVERRIDE", 10)]
    public class BlendOverride_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float BlendOutTime { get; set; }

        [MetaDataTag(6)]
        public float BlendInTime { get; set; }

        [MetaDataTag(7, "blend_in_default, blend_out_default, blend_in_crossfade, blend_out_crossfade ,blend_in_time,blend_out_time")]
        public int BlendMethod { get; set; }
    }
}
