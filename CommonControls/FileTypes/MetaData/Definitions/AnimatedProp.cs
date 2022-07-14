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
    public class AnimatedProp_v10 : Prop_v10, IAnimatedPropMeta
    {
        public float Scale { get => 1; }
    }

    [MetaData("ANIMATED_PROP", 11)]
    public class AnimatedProp_v11 : Prop_v11, IAnimatedPropMeta
    {
        public float Scale { get => 1; }
    }

    [MetaData("ANIMATED_PROP", 12)]
    public class AnimatedProp_v12 : Prop_v12, IAnimatedPropMeta
    {
        public float Scale { get => 1; }
    }

    [MetaData("ANIMATED_PROP", 13)]
    public class AnimatedProp_v13 : Prop_v13, IAnimatedPropMeta
    {
    }

    [MetaData("ANIMATED_PROP", 14)]
   public class AnimatedProp_v14 : Prop_v14, IAnimatedPropMeta
   {
   }

}
