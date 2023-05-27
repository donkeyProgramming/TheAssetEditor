using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    [MetaData("DISMEMBER", 10)]
    public class Dismember_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int BoneIndex { get; set; }

        [MetaDataTag(6)]
        public float BonePosition { get; set; }

        [MetaDataTag(7)]
        public Vector3 Direction { get; set; }

        [MetaDataTag(8)]
        public Vector3 Rotation { get; set; }
    }
    
    
    [MetaData("ALLOW_LEG_DISMEMBER", 10)]
    public class AllowLegDismember_v10 : DecodedMetaEntryBase
    {
    }
    [MetaData("ALLOW_FRONT_LEG_DISMEMBER", 10)]
    public class AllowFrontLegDismember_v10 : DecodedMetaEntryBase
    {
    }
}
