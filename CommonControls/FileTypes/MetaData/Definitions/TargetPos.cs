using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    [MetaData("TARGET_POS", 10)]
    public class TargetPos_10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public short Unk{ get; set; }

        [MetaDataTag(6)]
        public Vector3 Position { get; set; }
    }
}
