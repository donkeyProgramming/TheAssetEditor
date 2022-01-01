using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("PROP", 10)]
    public class Prop_v10 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(6, "Path to model. Probably has to be of the \"rigid\" vertex type, so no animated models")]
        public string ModelName { get; set; }

        [MetaDataTag(7, "Always empty?")]
        public string AnimationName { get; set; }

        [MetaDataTag(8)]
        public Vector3 Position { get; set; }

        [MetaDataTag(9)]
        public Vector4 Orientation { get; set; }

        [MetaDataTag(10)]
        public int BoneId { get; set; }

        [MetaDataTag(11, "bind_pose,animation,zeroed,skinned")]
        public int AttachMethod { get; set; }

        [MetaDataTag(12, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6), weapon_6 (7)")]
        public int OverrideProp { get; set; }
    }

    [MetaData("PROP", 11)]
    public class Prop_v11 : Prop_v10
    {
        [MetaDataTag(13)]
        public float BlendInTime { get; set; }

        [MetaDataTag(14)]
        public float BlendOutTime { get; set; }
    }
}
