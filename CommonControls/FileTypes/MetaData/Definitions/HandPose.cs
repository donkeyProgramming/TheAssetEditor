using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    public class HandPose_v2 : BaseMetaEntry
    {
        [MetaDataTag(1, "Time in second when the Tag takes effect")]
        public float StartTime { get; set; }

        [MetaDataTag(2, "Time in second when the Tag stops taking effect")]
        public float EndTime { get; set; }

        [MetaDataTag(3)]
        public ushort Filter { get; set; }

        [MetaDataTag(4, "This should be an enum. (these are animation slots in .frg) 0=HAND_POSE_RELAX,1=HAND_POSE_FLAT,2=HAND_POSE_CLENCH,3=HAND_POSE_GRIP,4=HAND_POSE_HALF_OPEN,5=HAND_POSE_THUMB_GRIP,6=HAND_POSE_CUSTOM_1,7=HAND_POSE_CUSTOM_2,8=HAND_POSE_CUSTOM_3,9=HAND_POSE_CUSTOM_4,10=HAND_POSE_CUSTOM_5,11=HAND_POSE_CUSTOM_6,12=HAND_POSE_CUSTOM_7,13=HAND_POSE_CUSTOM_8,14=HAND_POSE_CUSTOM_9,15=HAND_POSE_CUSTOM_10,")]
        public int HandPoseId { get; set; }

        [MetaDataTag(5)]
        public float Weight { get; set; }
    }

    [MetaData("RHAND_POSE", 2)]
    public class RHandPose_v2 : HandPose_v2
    {
    }

    [MetaData("LHAND_POSE", 2)]
    public class LHandPose_v2 : HandPose_v2
    {
    }



    public class HandPose : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "This should be an enum. (these are animation slots in .frg) 0=HAND_POSE_RELAX,1=HAND_POSE_FLAT,2=HAND_POSE_CLENCH,3=HAND_POSE_GRIP,4=HAND_POSE_HALF_OPEN,5=HAND_POSE_THUMB_GRIP,6=HAND_POSE_CUSTOM_1,7=HAND_POSE_CUSTOM_2,8=HAND_POSE_CUSTOM_3,9=HAND_POSE_CUSTOM_4,10=HAND_POSE_CUSTOM_5,11=HAND_POSE_CUSTOM_6,12=HAND_POSE_CUSTOM_7,13=HAND_POSE_CUSTOM_8,14=HAND_POSE_CUSTOM_9,15=HAND_POSE_CUSTOM_10,")]
        public int HandPoseId { get; set; }

        [MetaDataTag(6)]
        public float Weight { get; set; }
    }

    [MetaData("RHAND_POSE", 10)]
    public class RHandPose : HandPose
    {
    }


    [MetaData("LHAND_POSE", 10)]
    public class LHandPose : HandPose
    {
    }
}
