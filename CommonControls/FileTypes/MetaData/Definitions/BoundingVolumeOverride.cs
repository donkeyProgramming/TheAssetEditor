using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    [MetaData("BOUNDING_VOLUME_OVERRIDE", 10)]
    public class BoundingVolumeOverride_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Unk0 { get; set; }
    }
}
