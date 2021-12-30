using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("FACE_POSE", 10)]
    public class FacePose : MetaEntryBase
    {
        [MetaDataTag(5, "This should probably be an enum. (these are animation slots available in .frg, and in the order they appear there) 0=FACE_POSE_RELAX,1=FACE_POSE_RELAX_BLINK,2=FACE_POSE_ANGRY,3=FACE_POSE_ANGRY_BLINK,4=FACE_POSE_ANGRY_SCREAM,5=FACE_POSE_DEAD,6=FACE_POSE_DEAD2,7=FACE_POSE_DEATH_SCREAM,8=FACE_POSE_WORRIED,9=FACE_POSE_WORRIED_BLINK")]
        public int FacePoseId { get; set; }

        public float Weight { get; set; }
    }
}
