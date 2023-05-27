using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("PROP", 4)]
    public class Prop_v4 : DecodedMetaEntryBaseOld
    {
        [MetaDataTag(4)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(5)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(6)]
        public Vector3 Position { get; set; }

        [MetaDataTag(7, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(8)]
        public int BoneId { get; set; }

        [MetaDataTag(9, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public int AttachMethod { get; set; }

        [MetaDataTag(10, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
        public int OverrideProp { get; set; }
    }
    
    [MetaData("PROP", 10)]
    public class Prop_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(6)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(7)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(8)]
        public Vector3 Position { get; set; }

        [MetaDataTag(9, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(10)]
        public int BoneId { get; set; }

        [MetaDataTag(11, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public int AttachMethod { get; set; }

        [MetaDataTag(12, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
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

    [MetaData("PROP", 12)]
    public class Prop_v12 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(6)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(7)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(8)]
        public string MetaData { get; set; } = "";

        [MetaDataTag(9)]
        public Vector3 Position { get; set; }

        [MetaDataTag(10, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(11)]
        public int BoneId { get; set; }

        [MetaDataTag(12, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public int AttachMethod { get; set; }

        [MetaDataTag(13, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
        public int OverrideProp { get; set; }

        [MetaDataTag(14)]
        public float BlendInTime { get; set; }

        [MetaDataTag(15)]
        public float BlendOutTime { get; set; }
    }

    [MetaData("PROP", 13)]
    public class Prop_v13 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(6)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(7)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(8)]
        public string MetaData { get; set; } = "";

        [MetaDataTag(9)]
        public Vector3 Position { get; set; }

        [MetaDataTag(10, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(11)]
        public float Scale { get; set; } = 1;

        [MetaDataTag(12)]
        public int BoneId { get; set; }

        [MetaDataTag(13, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public int AttachMethod { get; set; }

        [MetaDataTag(14, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
        public int OverrideProp { get; set; }

        [MetaDataTag(15)]
        public float BlendInTime { get; set; }

        [MetaDataTag(16)]
        public float BlendOutTime { get; set; }
    }

    [MetaData("PROP", 14)]
    public class Prop_v14 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "")]
        public bool Unknownbool { get; set; }

        [MetaDataTag(6)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(7)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(8)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(9)]
        public string MetaData { get; set; } = "";

        [MetaDataTag(10)]
        public Vector3 Position { get; set; }

        [MetaDataTag(11, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(12)]
        public float Scale { get; set; } = 1;

        [MetaDataTag(13)]
        public int BoneId { get; set; }

        [MetaDataTag(14, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public int AttachMethod { get; set; }

        [MetaDataTag(15, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
        public int OverrideProp { get; set; }


        [MetaDataTag(16)]
        public float BlendInTime { get; set; }

        [MetaDataTag(17)]
        public float BlendOutTime { get; set; }
    }
}
