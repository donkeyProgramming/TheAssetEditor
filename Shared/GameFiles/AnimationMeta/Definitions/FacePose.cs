// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("FACE_POSE", 2)]
    public class FacePose_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4, "This should probably be an enum. (these are animation slots available in .frg, and in the order they appear there) 0=FACE_POSE_RELAX,1=FACE_POSE_RELAX_BLINK,2=FACE_POSE_ANGRY,3=FACE_POSE_ANGRY_BLINK,4=FACE_POSE_ANGRY_SCREAM,5=FACE_POSE_DEAD,6=FACE_POSE_DEAD2,7=FACE_POSE_DEATH_SCREAM,8=FACE_POSE_WORRIED,9=FACE_POSE_WORRIED_BLINK")]
        public int FacePoseId { get; set; }

        [MetaDataTag(5)]
        public float Weight { get; set; }
    }

    [MetaData("FACEFX", 2)]
    public class FaceFX_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public string FbxString { get; set; } = "";
        [MetaDataTag(5)]
        public string CaString { get; set; } = "";
        [MetaDataTag(6)]
        public string UnknownString { get; set; } = "";
        [MetaDataTag(7)]
        public Vector3 Unknown0_v10 { get; set; }
    }

    [MetaData("FACE_POSE", 10)]
    public class FacePose : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "This should probably be an enum. (these are animation slots available in .frg, and in the order they appear there) 0=FACE_POSE_RELAX,1=FACE_POSE_RELAX_BLINK,2=FACE_POSE_ANGRY,3=FACE_POSE_ANGRY_BLINK,4=FACE_POSE_ANGRY_SCREAM,5=FACE_POSE_DEAD,6=FACE_POSE_DEAD2,7=FACE_POSE_DEATH_SCREAM,8=FACE_POSE_WORRIED,9=FACE_POSE_WORRIED_BLINK")]
        public int FacePoseId { get; set; }

        [MetaDataTag(6)]
        public float Weight { get; set; }
    }
}
