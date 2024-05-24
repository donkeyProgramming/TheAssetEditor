// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("SOUND_IMPACT", 2)]
    public class SoundImpact_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt1 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt2 { get; set; }
    }

    [MetaData("SOUND_TRIGGER", 2)]
    public class SoundTrigger_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt1 { get; set; }

        [MetaDataTag(5)]
        public int UnknownInt2 { get; set; }
    }

    [MetaData("SOUND_TRIGGER", 4)]
    public class SoundTrigger_v4 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public string SoundEvent { get; set; } = "";

        [MetaDataTag(5)]
        public int BoneIndex { get; set; }

        [MetaDataTag(6)]
        public Vector3 Position { get; set; }
    }

    [MetaData("SOUND_TRIGGER", 10)]
    public class SoundTrigger_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public string SoundEvent { get; set; } = "";

        [MetaDataTag(6)]
        public int BoneIndex { get; set; }

        [MetaDataTag(7)]
        public Vector3 Position { get; set; }
    }

    [MetaData("SOUND_TRIGGER", 11)]
    public class SoundTrigger_v11 : SoundTrigger_v10
    {
        [MetaDataTag(8)]
        public string Unknown { get; set; } = "";
    }


    [MetaData("SOUND_BUILDING", 2)]
    public class SoundBuilding_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public int UnknownInt1 { get; set; }

        [MetaDataTag(5)]
        public Vector3 Position { get; set; }

        [MetaDataTag(6)]
        public int UnknownInt2 { get; set; }
    }


    [MetaData("SOUND_SPHERE", 10)]
    public class SoundSphere_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public string UnknownString { get; set; } = "";

        [MetaDataTag(6)]
        public float UnknownFloat0_v10 { get; set; }

        [MetaDataTag(7)]
        public float UnknownFloat1_v10 { get; set; }
    }


    [MetaData("POSITION", 10)]
    public class Position_v10 : DecodedMetaEntryBase
    {
        //making up value types just to be able to parse it
        //the short one might str length

        [MetaDataTag(5)]
        public Vector3 Position { get; set; }
    }


    [MetaData("SOUND_POSITION", 10)]
    public class SoundPosition_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public string UnknownString_v10 { get; set; } = "";

        [MetaDataTag(6)]
        public Vector3 ProbablyPosition { get; set; }

        [MetaDataTag(7)]
        public Vector4 ProbablyOrientation { get; set; }

        [MetaDataTag(8)]
        public int UnknownInt_v10 { get; set; }
    }


    [MetaData("WOUNDED_POSE", 0)]
    public class WoundedPose_v0 : DecodedMetaEntryBase_v0
    {
        [MetaDataTag(2)]
        public string UnknownString { get; set; } = "";
    }

    [MetaData("WOUNDED_POSE", 2)]
    public class WoundedPose_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4)]
        public string UnknownString { get; set; } = "";
    }

    [MetaData("WOUNDED_POSE", 10)]
    public class WoundedPose_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public string UnknownString { get; set; } = "";
    }

    [MetaData("SOUND_SPHERE_LINK", 10)]
    public class SoundSphereLink_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("SYNC_MARKER", 10)]
    public class SyncMarker_v10 : DecodedMetaEntryBase
    {
    }
    [MetaData("PULL_ROPE", 2)]
    public class PullRope_v2 : DecodedMetaEntryBase_v2
    {
    }

    [MetaData("TURRET_ATTACHMENT", 14)]
    public class TurretAttachment_v14 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int UnknownInt0 { get; set; }
        [MetaDataTag(6)]
        public int UnknownInt1 { get; set; }
        [MetaDataTag(7)]
        public int UnknownInt2 { get; set; }
        [MetaDataTag(8)]
        public int UnknownInt3 { get; set; }
        [MetaDataTag(9)]
        public int UnknownInt4 { get; set; }
        [MetaDataTag(10)]
        public int UnknownInt5 { get; set; }
        [MetaDataTag(11)]
        public int UnknownInt6 { get; set; }
        [MetaDataTag(12)]
        public int UnknownInt7 { get; set; }

        [MetaDataTag(13)]
        public float UnknownFloat0 { get; set; }
        [MetaDataTag(14)]
        public float UnknownFloat1 { get; set; }

        [MetaDataTag(15)]
        public int UnknownInt8 { get; set; }
        [MetaDataTag(16)]
        public int UnknownInt9 { get; set; }
        [MetaDataTag(17)]
        public int UnknownInt10 { get; set; }
        [MetaDataTag(18)]
        public int UnknownInt11 { get; set; }
        [MetaDataTag(19)]
        public int UnknownInt12 { get; set; }
    }


    [MetaData("PARENT_CONSTRAINT", 10)]
    public class ParentConstraint_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int UnknownInt0 { get; set; }
        [MetaDataTag(6)]
        public int UnknownInt1 { get; set; }

        [MetaDataTag(7)]
        public float UnknownFloat0 { get; set; }

        //or floats
        [MetaDataTag(8)]
        public int UnknownInt2 { get; set; }
        [MetaDataTag(9)]
        public int UnknownInt3 { get; set; }
        [MetaDataTag(10)]
        public int UnknownInt4 { get; set; }
        [MetaDataTag(11)]
        public int UnknownInt5 { get; set; }
        [MetaDataTag(12)]
        public int UnknownInt6 { get; set; }

        [MetaDataTag(13)]
        public float UnknownFloat1 { get; set; }

        // or floats
        [MetaDataTag(14)]
        public int UnknownInt7 { get; set; }
        [MetaDataTag(15)]
        public int UnknownInt8 { get; set; }
    }


    [MetaData("CROP_MARKER", 10)]
    public class CropMarker_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("NOT_HERO_TARGET", 10)]
    public class NotHeroTarget_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("DIE_PERMANENTLY", 10)]
    public class DiePermamently_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("DISABLE_ENEMY_COLLISION", 10)]
    public class DisableEnemyCollision_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("MARK_RIGHT_FOOT_FRONT", 10)]
    public class MarkRightFootFront_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("NO CHAINING", 10)]
    public class NoChaining_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("NOT_REGULAR_TARGET", 10)]
    public class NotRegularTarget_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("START_CLIMB", 10)]
    public class StartClimb_v10 : DecodedMetaEntryBase
    {
    }

    [MetaData("MATERIAL_FLAG", 10)]
    public class MaterialFlag_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int UnknownInt0_v10 { get; set; }

        [MetaDataTag(6)]
        public float UnknownFloat0_v10 { get; set; }

        [MetaDataTag(7)]
        public int UnknownInt1_v10 { get; set; }
    }

    [MetaData("FREEZE_WEAPON", 10)]
    public class FreezeWeapon_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public int UnknownInt0 { get; set; }

        [MetaDataTag(6)]
        public string UnknownString { get; set; } = "";

        [MetaDataTag(7)]
        public Vector3 Unknown0_v10 { get; set; }
    }
}
