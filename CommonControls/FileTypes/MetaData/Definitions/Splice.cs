using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SPLICE", 12)]
    public class Splice : MetaEntryBase
    {
        [MetaDataTag(5)]
        public string Animation{ get; set; }

        [MetaDataTag(6)]
        public string ReferenceAnimation { get; set; }

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
        
        [MetaDataTag(17)]
        public string Additive { get; set; }
        [MetaDataTag(18)]
        public string WeaponPose { get; set; }
        [MetaDataTag(19)]
        public string Stretch { get; set; }
        [MetaDataTag(20)]
        public string ExcludeStripped { get; set; }
        [MetaDataTag(21)]
        public string PrimaryPersistent { get; set; }
    }
}
