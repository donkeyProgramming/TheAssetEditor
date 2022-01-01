using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    [MetaData("DISMEMBER", 10)]
    public class Dismember_v10 : MetaEntryBase
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
}
