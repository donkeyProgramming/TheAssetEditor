using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    [MetaData("IMPACT_SPEED", 10)]
    public class ImpactSpeed_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Speed { get; set; }
    }
}
