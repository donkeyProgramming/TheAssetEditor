using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaEntry("ANIMATED_PROP", 10)]
    public class AnimatedProp_v10 : MetaEntryBase
    {
        [MetaDataTag(5)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(6)]
        public string ModelName { get; set; }

        [MetaDataTag(7)]
        public string AnimationName { get; set; }

        [MetaDataTag(8)]
        public Vector3 Position { get; set; }

        [MetaDataTag(9)]
        public Vector4 Orientation { get; set; }

        [MetaDataTag(10)]
        public int BoneId { get; set; }

        [MetaDataTag(11)]
        public int AttachMethod { get; set; }

        [MetaDataTag(12)]
        public int OverrideProp { get; set; }
    }

    [MetaEntry("ANIMATED_PROP", 11)]
    public class AnimatedProp_v11 : AnimatedProp_v10
    {
        [MetaDataTag(13)]
        public float BlendInTime { get; set; }

        [MetaDataTag(14)]
        public float BlendOutTime { get; set; }
    }
}
