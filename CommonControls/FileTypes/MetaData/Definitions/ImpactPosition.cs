using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("IMPACT_POS", 2)]
    public class ImpactPosition_v2 : DecodedMetaEntryBaseOld
    {
        [MetaDataTag(4)]
        public Vector3 Position { get; set; }
    }
    
    [MetaData("IMPACT_POS", 10)]
    public class ImpactPosition : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public Vector3 Position { get; set; }
    }
}
