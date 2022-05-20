using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    public interface IAnimatedPropMeta
    {
        public string ModelName { get; set; }
        public string AnimationName { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public int BoneId { get; set; }
        public Vector3 Position { get; set; }
        public Vector4 Orientation { get; set; }
        public float Scale{ get; }
    }

    [MetaData("ANIMATED_PROP", 3)]
    public class AnimatedProp_v3 : BaseMetaEntry
    {
        [MetaDataTag(1, "Time in second when the Tag takes effect")]
        public float StartTime { get; set; }

        [MetaDataTag(2, "Time in second when the Tag stops taking effect")]
        public float EndTime { get; set; }

        [MetaDataTag(3)]
        public ushort Filter { get; set; }


        [MetaDataTag(4)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(5)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(6)]
        public Vector3 Position { get; set; } = Vector3.Zero;

        [MetaDataTag(7, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(8)]
        public int Unkown1 { get; set; }

        [MetaDataTag(9)]
        public int Unkown0 { get; set; }
    }


    [MetaData("ANIMATED_PROP", 10)]
    public class AnimatedProp_v10 : DecodedMetaEntryBase, IAnimatedPropMeta
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

        public float Scale { get => 1; }
    }

    [MetaData("ANIMATED_PROP", 11)]
    public class AnimatedProp_v11 : AnimatedProp_v10
    {
        [MetaDataTag(13)]
        public float BlendInTime { get; set; }

        [MetaDataTag(14)]
        public float BlendOutTime { get; set; }
    }

    [MetaData("ANIMATED_PROP", 12)]
    public class AnimatedProp_v12 : DecodedMetaEntryBase, IAnimatedPropMeta
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

        public float Scale { get => 1; }
    }

    [MetaData("ANIMATED_PROP", 13)]
    public class AnimatedProp_v13 : DecodedMetaEntryBase, IAnimatedPropMeta
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

    [MetaData("ANIMATED_PROP", 14)]
   public class AnimatedProp_v14 : DecodedMetaEntryBase, IAnimatedPropMeta
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
