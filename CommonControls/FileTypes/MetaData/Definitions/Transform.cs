using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("TRANSFORM", 10)]
    public class Transform_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "ID of the bone which you are moving or rotating.")]
        public int TargetNode{ get; set; }

        [MetaDataTag(6)]
        public int SourceNode { get; set; }
        [MetaDataTag(7)]
        public Vector3 Position { get; set; }
        [MetaDataTag(8)]
        public Vector4 Orientation { get; set; }
        [MetaDataTag(9)]
        public float BlendInTime { get; set; }
        [MetaDataTag(10)]
        public float BlendOutTime { get; set; }
    }
}
