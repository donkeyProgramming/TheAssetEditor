// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{

    [MetaData("PROP", 2)]
    public class Prop_v2 : DecodedMetaEntryBase_v2
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
    }

    [MetaData("PROP", 3)]
    public class Prop_v3 : Prop_v2
    {
        [MetaDataTag(9, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public int AttachMethod { get; set; }
    }

    [MetaData("PROP", 4)]
    public class Prop_v4 : Prop_v3
    {
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
    public class Prop_v12 : Prop_v11
    {
        // new field
        [MetaDataTag(8)]
        public string MetaData { get; set; } = "";

        //override order
        [MetaDataTag(9)]
        public new Vector3 Position { get; set; }

        [MetaDataTag(10, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public new Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(11)]
        public new int BoneId { get; set; }

        [MetaDataTag(12, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public new int AttachMethod { get; set; }

        [MetaDataTag(13, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
        public new int OverrideProp { get; set; }

        [MetaDataTag(14)]
        public new float BlendInTime { get; set; }

        [MetaDataTag(15)]
        public new float BlendOutTime { get; set; }
    }

    [MetaData("PROP", 13)]
    public class Prop_v13 : Prop_v12
    {
        // new field
        [MetaDataTag(11)]
        public float Scale { get; set; } = 1;

        // override order
        [MetaDataTag(12)]
        public new int BoneId { get; set; }

        [MetaDataTag(13, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public new int AttachMethod { get; set; }

        [MetaDataTag(14, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
        public new int OverrideProp { get; set; }

        [MetaDataTag(15)]
        public new float BlendInTime { get; set; }

        [MetaDataTag(16)]
        public new float BlendOutTime { get; set; }
    }

    [MetaData("PROP", 14)]
    public class Prop_v14 : DecodedMetaEntryBase
    {
        // new field
        [MetaDataTag(5, "")]
        public bool Unknownbool { get; set; }

        // the rest from previous versions
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

    [MetaData("PROP", 15)]
    public class Prop_v15 : DecodedMetaEntryBase
    {
        // new field
        [MetaDataTag(5, "")]
        public bool Unknownbool_v15 { get; set; }

        // the rest from previous versions
        [MetaDataTag(6, "")]
        public bool Unknownbool { get; set; }

        [MetaDataTag(7)]
        public bool DistanceCulled { get; set; }

        [MetaDataTag(8)]
        public string ModelName { get; set; } = "";

        [MetaDataTag(9)]
        public string AnimationName { get; set; } = "";

        [MetaDataTag(10)]
        public string MetaData { get; set; } = "";

        [MetaDataTag(11)]
        public Vector3 Position { get; set; }

        [MetaDataTag(12, "", MetaDataTagAttribute.DisplayType.EulerVector)]
        public Vector4 Orientation { get; set; } = new Vector4(0, 0, 0, 1);

        [MetaDataTag(13)]
        public float Scale { get; set; } = 1;

        [MetaDataTag(14)]
        public int BoneId { get; set; }

        [MetaDataTag(15, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public int AttachMethod { get; set; }

        [MetaDataTag(16, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
        public int OverrideProp { get; set; }

        [MetaDataTag(17)]
        public float BlendInTime { get; set; }

        [MetaDataTag(18)]
        public float BlendOutTime { get; set; }
    }



    // YOBANNYI VROT ETO KASINO

    [MetaData("PROP", 12, MetaDataAttributePriority.Low)]
    public class Prop_v12_3K : DecodedMetaEntryBase
    {
        public override string Description { get; } = "_3KOnly";

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

        // TODO Verify order of the following 3 fields:
        [MetaDataTag(10)]
        public int BoneId { get; set; }

        [MetaDataTag(11, "bind_pose (1), animation (2), zeroed (3), skinned (4)")]
        public int AttachMethod { get; set; }

        [MetaDataTag(12, "projectile (1) ,weapon_1 (2) ,weapon_2 (3) ,weapon_3 (4) ,weapon_4 (5) ,weapon_5 (6),weapon_6 (7)")]
        public int OverrideProp { get; set; }

        [MetaDataTag(13)]
        public float BlendInTime { get; set; }

        [MetaDataTag(14)]
        public float BlendOutTime { get; set; }

        [MetaDataTag(15)]
        public float Scale { get; set; } = 1;
    }


    [MetaData("PROP", 13, MetaDataAttributePriority.Low)]
    public class Prop_v13_3K : Prop_v12_3K
    {
        public override string Description { get; } = "_3KOnly";

        [MetaDataTag(16)]
        public string MetaData { get; set; } = "";

        [MetaDataTag(17, "This works in pair with MetaData")]
        public string PathToAnotherAnim { get; set; } = "";
    }
}
