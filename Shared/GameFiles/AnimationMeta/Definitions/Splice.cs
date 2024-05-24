// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    //TODO: VERIFY OLD SPLICE META TAGS ...

    [MetaData("SPLICE", 0)]
    public class Splice_v0 : DecodedMetaEntryBase_v0
    {
        [MetaDataTag(2)]
        public string Animation { get; set; } = "";

        [MetaDataTag(3)]
        public float ProbablyNeckWeight { get; set; }
        [MetaDataTag(4)]
        public float ProbablyTorsoWeight { get; set; }
        [MetaDataTag(5)]
        public float ProbablyLeftArmWeight { get; set; }
        [MetaDataTag(6)]
        public float ProbablyRightArmWeight { get; set; }
        // [MetaDataTag(9)]
        // public float ProbablyLegsWeight { get; set; }

        [MetaDataTag(7, "true or false as text")]
        public string ProbablyAdditive { get; set; } = "false";
    }

    [MetaData("SPLICE", 2)]
    public class Splice_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public string Animation { get; set; } = "";

        [MetaDataTag(5)]
        public float ProbablyNeckWeight { get; set; }
        [MetaDataTag(6)]
        public float ProbablyTorsoWeight { get; set; }
        [MetaDataTag(7)]
        public float ProbablyLeftArmWeight { get; set; }
        [MetaDataTag(8)]
        public float ProbablyRightArmWeight { get; set; }
        // [MetaDataTag(9)]
        // public float ProbablyLegsWeight { get; set; }

        [MetaDataTag(9, "true or false as text")]
        public string ProbablyAdditive { get; set; } = "false";
    }

    [MetaData("SPLICE", 3)]
    public class Splice_v3 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public string Animation { get; set; } = "";

        // new field
        [MetaDataTag(5)]
        public string ReferenceAnimation { get; set; } = "";

        [MetaDataTag(6)]
        public float ProbablyGenericWeight { get; set; }

        [MetaDataTag(7)]
        public float ProbablyNeckWeight { get; set; }
        [MetaDataTag(8)]
        public float ProbablyTorsoWeight { get; set; }
        [MetaDataTag(9)]
        public float ProbablyLeftArmWeight { get; set; }
        [MetaDataTag(10)]
        public float ProbablyRightArmWeight { get; set; }
        [MetaDataTag(11)]
        public float ProbablyLegsWeight { get; set; }

        [MetaDataTag(12, "true or false as text")]
        public string ProbablyAdditive { get; set; } = "false";

        //new field
        [MetaDataTag(13, "true or false as text")]
        public string ProbablyWeaponPose { get; set; } = "false";
    }

    [MetaData("SPLICE", 4)]
    public class Splice_v4 : Splice_v3
    {
        [MetaDataTag(14, "true or false as text")]
        public string ProbablyStretch { get; set; } = "false";
    }

    [MetaData("SPLICE", 5)]
    public class Splice_v5 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public string Animation { get; set; } = "";

        [MetaDataTag(5)]
        public string ReferenceAnimation { get; set; } = "";

        // new field
        [MetaDataTag(6, "-1 applies the whole animation? | The spliced animation applies to this bone and all of it's children. -1 seems to be the same as 0, ie. animroot, but there's probably something with the pose-type .anim files that makes it distinct.")]
        public int GenericBoneIndex { get; set; }

        [MetaDataTag(7)]
        public float UnknownFloatOrGenericBoneDepth { get; set; }

        [MetaDataTag(8)]
        public float GenericWeight { get; set; }

        [MetaDataTag(9)]
        public float NeckWeight { get; set; }
        [MetaDataTag(10)]
        public float TorsoWeight { get; set; }
        [MetaDataTag(11)]
        public float LeftArmWeight { get; set; }
        [MetaDataTag(12)]
        public float RightArmWeight { get; set; }
        [MetaDataTag(13)]
        public float LegsWeight { get; set; }

        [MetaDataTag(14, "true or false as text")]
        public string Additive { get; set; } = "false";
        [MetaDataTag(15, "true or false as text")]
        public string WeaponPose { get; set; } = "false";
        [MetaDataTag(16, "true or false as text")]
        public string Stretch { get; set; } = "false";

        // new field
        [MetaDataTag(17, "true or false as text")]
        public string ExcludeStripped { get; set; } = "false";
    }

    [MetaData("SPLICE", 6)]
    public class Splice_v6 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public string Animation { get; set; } = "";

        [MetaDataTag(5)]
        public string ReferenceAnimation { get; set; } = "";

        [MetaDataTag(6, "-1 applies the whole animation? | The spliced animation applies to this bone and all of it's children. -1 seems to be the same as 0, ie. animroot, but there's probably something with the pose-type .anim files that makes it distinct.")]
        public int GenericBoneIndex { get; set; }

        // changed filetype from float to int
        [MetaDataTag(7)]
        public int GenericBoneDepth { get; set; }

        // As is
        [MetaDataTag(8)]
        public float GenericWeight { get; set; }

        [MetaDataTag(9)]
        public float NeckWeight { get; set; }
        [MetaDataTag(10)]
        public float TorsoWeight { get; set; }
        [MetaDataTag(11)]
        public float LeftArmWeight { get; set; }
        [MetaDataTag(12)]
        public float RightArmWeight { get; set; }
        [MetaDataTag(13)]
        public float LegsWeight { get; set; }


        // New field for v6
        [MetaDataTag(14)]
        public float BlendInTime { get; set; }

        // override MetaDataTag order
        [MetaDataTag(15, "true or false as text")]
        public string Additive { get; set; } = "false";
        [MetaDataTag(16, "true or false as text")]
        public string WeaponPose { get; set; } = "false";
        [MetaDataTag(17, "true or false as text")]
        public string Stretch { get; set; } = "false";
        [MetaDataTag(18, "true or false as text")]
        public string ExcludeStripped { get; set; } = "false";
    }

    [MetaData("SPLICE", 10)]
    public class Splice_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public string Animation { get; set; } = "";

        [MetaDataTag(6)]
        public string ReferenceAnimation { get; set; } = "";

        [MetaDataTag(7, "-1 applies the whole animation? | The spliced animation applies to this bone and all of it's children. -1 seems to be the same as 0, ie. animroot, but there's probably something with the pose-type .anim files that makes it distinct.")]
        public int GenericBoneIndex { get; set; }

        [MetaDataTag(8)]
        public int GenericBoneDepth { get; set; }

        [MetaDataTag(9)]
        public float GenericWeight { get; set; }

        [MetaDataTag(10)]
        public float NeckWeight { get; set; }
        [MetaDataTag(11)]
        public float TorsoWeight { get; set; }
        [MetaDataTag(12)]
        public float LeftArmWeight { get; set; }
        [MetaDataTag(13)]
        public float RightArmWeight { get; set; }
        [MetaDataTag(14)]
        public float LegsWeight { get; set; }
        [MetaDataTag(15)]
        public float BlendInTime { get; set; }

        [MetaDataTag(16, "true or false as text")]
        public string Additive { get; set; } = "false";
        [MetaDataTag(17, "true or false as text")]
        public string WeaponPose { get; set; } = "false";
        [MetaDataTag(18, "true or false as text")]
        public string Stretch { get; set; } = "false";
        [MetaDataTag(19, "true or false as text")]
        public string ExcludeStripped { get; set; } = "false";
    }

    [MetaData("SPLICE", 11)]
    public class Splice_v11 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public string Animation { get; set; } = "";

        [MetaDataTag(6)]
        public string ReferenceAnimation { get; set; } = "";

        [MetaDataTag(7, "-1 applies the whole animation? | The spliced animation applies to this bone and all of it's children. -1 seems to be the same as 0, ie. animroot, but there's probably something with the pose-type .anim files that makes it distinct.")]
        public int GenericBoneIndex { get; set; }

        [MetaDataTag(8)]
        public int GenericBoneDepth { get; set; }

        [MetaDataTag(9)]
        public float GenericWeight { get; set; }

        [MetaDataTag(10)]
        public float NeckWeight { get; set; }
        [MetaDataTag(11)]
        public float TorsoWeight { get; set; }
        [MetaDataTag(12)]
        public float LeftArmWeight { get; set; }
        [MetaDataTag(13)]
        public float RightArmWeight { get; set; }
        [MetaDataTag(14)]
        public float LegsWeight { get; set; }
        [MetaDataTag(15)]
        public float BlendInTime { get; set; }

        [MetaDataTag(16)]
        public float BlendOutTime { get; set; }

        [MetaDataTag(17, "true or false as text")]
        public string Additive { get; set; } = "false";
        [MetaDataTag(18, "true or false as text")]
        public string WeaponPose { get; set; } = "false";
        [MetaDataTag(19, "true or false as text")]
        public string Stretch { get; set; } = "false";
        [MetaDataTag(20, "true or false as text")]
        public string ExcludeStripped { get; set; } = "false";
    }


    [MetaData("SPLICE", 12)]
    public class Splice_v12 : Splice_v11
    {
        [MetaDataTag(21)]
        public string PrimaryPersistent { get; set; } = "";
    }
}
