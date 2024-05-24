// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    public class HandPose_v0 : DecodedMetaEntryBase_v0
    {
        [MetaDataTag(2, "This should be an enum. (these are animation slots in .frg) 0=HAND_POSE_RELAX,1=HAND_POSE_FLAT,2=HAND_POSE_CLENCH,3=HAND_POSE_GRIP,4=HAND_POSE_HALF_OPEN,5=HAND_POSE_THUMB_GRIP,6=HAND_POSE_CUSTOM_1,7=HAND_POSE_CUSTOM_2,8=HAND_POSE_CUSTOM_3,9=HAND_POSE_CUSTOM_4,10=HAND_POSE_CUSTOM_5,11=HAND_POSE_CUSTOM_6,12=HAND_POSE_CUSTOM_7,13=HAND_POSE_CUSTOM_8,14=HAND_POSE_CUSTOM_9,15=HAND_POSE_CUSTOM_10,")]
        public int HandPoseId { get; set; }

        [MetaDataTag(3)]
        public float Weight { get; set; }
    }

    public class HandPose_v1 : DecodedMetaEntryBase_v1
    {
        [MetaDataTag(3, "This should be an enum. (these are animation slots in .frg) 0=HAND_POSE_RELAX,1=HAND_POSE_FLAT,2=HAND_POSE_CLENCH,3=HAND_POSE_GRIP,4=HAND_POSE_HALF_OPEN,5=HAND_POSE_THUMB_GRIP,6=HAND_POSE_CUSTOM_1,7=HAND_POSE_CUSTOM_2,8=HAND_POSE_CUSTOM_3,9=HAND_POSE_CUSTOM_4,10=HAND_POSE_CUSTOM_5,11=HAND_POSE_CUSTOM_6,12=HAND_POSE_CUSTOM_7,13=HAND_POSE_CUSTOM_8,14=HAND_POSE_CUSTOM_9,15=HAND_POSE_CUSTOM_10,")]
        public int HandPoseId { get; set; }

        [MetaDataTag(4)]
        public float Weight { get; set; }
    }

    public class HandPose_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4, "This should be an enum. (these are animation slots in .frg) 0=HAND_POSE_RELAX,1=HAND_POSE_FLAT,2=HAND_POSE_CLENCH,3=HAND_POSE_GRIP,4=HAND_POSE_HALF_OPEN,5=HAND_POSE_THUMB_GRIP,6=HAND_POSE_CUSTOM_1,7=HAND_POSE_CUSTOM_2,8=HAND_POSE_CUSTOM_3,9=HAND_POSE_CUSTOM_4,10=HAND_POSE_CUSTOM_5,11=HAND_POSE_CUSTOM_6,12=HAND_POSE_CUSTOM_7,13=HAND_POSE_CUSTOM_8,14=HAND_POSE_CUSTOM_9,15=HAND_POSE_CUSTOM_10,")]
        public int HandPoseId { get; set; }

        [MetaDataTag(5)]
        public float Weight { get; set; }
    }



    [MetaData("RHAND_POSE", 0)]
    public class RHandPose_v0 : HandPose_v0
    {
    }

    [MetaData("LHAND_POSE", 0)]
    public class LHandPose_v0 : HandPose_v0
    {
    }

    [MetaData("RHAND_POSE", 1)]
    public class RHandPose_v1 : HandPose_v1
    {
    }

    [MetaData("LHAND_POSE", 1)]
    public class LHandPose_v1 : HandPose_v1
    {
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
