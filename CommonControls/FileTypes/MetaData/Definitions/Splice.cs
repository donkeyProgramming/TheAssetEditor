using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SPLICE", 5)]
    public class Splice_v5 :  DecodedMetaEntryBaseOld
    {
        [MetaDataTag(4)]
        public string Animation { get; set; } = "";

        [MetaDataTag(5)]
        public string ReferenceAnimation { get; set; } = "";

        [MetaDataTag(6, "-1 applies the whole animation? | The spliced animation applies to this bone and all of it's children. -1 seems to be the same as 0, ie. animroot, but there's probably something with the pose-type .anim files that makes it distinct.")]
        public int GenericBoneIndex { get; set; }

        [MetaDataTag(7)]
        public int GenericBoneDepth { get; set; }

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
        [MetaDataTag(17, "true or false as text")]
        public string ExcludeStripped { get; set; } = "false";
    }
    
    [MetaData("SPLICE", 6)]
    public class Splice_v6 :  Splice_v5
    {
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
