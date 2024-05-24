// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
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
        public float Scale { get; }
    }

    [MetaData("ANIMATED_PROP", 0)]
    public class AnimatedProp_v0 : DecodedMetaEntryBase_v0
    {
        [MetaDataTag(2)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(3)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(4)]
        public Vector3 Position { get; set; } = Vector3.Zero;

        [MetaDataTag(5, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(6)]
        public int UnknownInt_v0 { get; set; }
    }

    [MetaData("ANIMATED_PROP", 2)]
    public class AnimatedProp_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(5)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(6)]
        public Vector3 Position { get; set; } = Vector3.Zero;

        [MetaDataTag(7, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(8)]
        public int UnknownInt_v0 { get; set; }
    }

    [MetaData("ANIMATED_PROP", 3)]
    public class AnimatedProp_v3 : AnimatedProp_v2
    {
        [MetaDataTag(9)]
        public int UnknownInt0_v3 { get; set; }
    }

    [MetaData("ANIMATED_PROP", 4)]
    public class AnimatedProp_v4 : AnimatedProp_v3
    {
        [MetaDataTag(10)]
        public int UnknownInt0_v4 { get; set; }
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

    [MetaData("ANIMATED_PROP", 15)]
    public class AnimatedProp_v15 : Prop_v15, IAnimatedPropMeta
    {
    }


    [MetaData("ANIMATED_PROP", 12, MetaDataAttributePriority.Low)]
    public class AnimatedProp_v12_3K : Prop_v12_3K, IAnimatedPropMeta
    {
    }

    [MetaData("ANIMATED_PROP", 13, MetaDataAttributePriority.Low)]
    public class AnimatedProp_v13_3K : Prop_v13_3K, IAnimatedPropMeta
    {
    }

}
