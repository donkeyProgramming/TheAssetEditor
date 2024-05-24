// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{

    public abstract class DockEquipment_v0 : DecodedMetaEntryBase_v0
    {
        //TODO verify
        [MetaDataTag(2, "(Might be not true for old  meta version) 0=\"Weapon Bone 1\" in .frg but \"be_prop_0\" in a typical VMD. 1=\"Weapon Bone 2\" in .frg but \"be_prop_1\" in VMD. etc.")]
        public int PropBoneId { get; set; }

        public virtual string AnimationSlotName { get; } = "";
        public virtual string[] SkeletonNameAlternatives { get; } = new string[] { "" };
    }

    public abstract class DockEquipment_v1 : DecodedMetaEntryBase_v1
    {
        //TODO verify
        [MetaDataTag(3, "(Might be not true for old  meta version) 0=\"Weapon Bone 1\" in .frg but \"be_prop_0\" in a typical VMD. 1=\"Weapon Bone 2\" in .frg but \"be_prop_1\" in VMD. etc.")]
        public int PropBoneId { get; set; }

        public virtual string AnimationSlotName { get; } = "";
        public virtual string[] SkeletonNameAlternatives { get; } = new string[] { "" };
    }

    public abstract class DockEquipment_v2 : DecodedMetaEntryBase_v2
    {
        //TODO verify
        [MetaDataTag(4, "(Might be not true for old  meta version) 0=\"Weapon Bone 1\" in .frg but \"be_prop_0\" in a typical VMD. 1=\"Weapon Bone 2\" in .frg but \"be_prop_1\" in VMD. etc.")]
        public int PropBoneId { get; set; }

        public virtual string AnimationSlotName { get; } = "";
        public virtual string[] SkeletonNameAlternatives { get; } = new string[] { "" };
    }

    public abstract class DockEquipment_v3 : DockEquipment_v2
    {
        [MetaDataTag(5)]
        public float BlendInTime { get; set; }

        [MetaDataTag(6)]
        public float BlendOutTime { get; set; }
    }

    public abstract class DockEquipment : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "0=\"Weapon Bone 1\" in .frg but \"be_prop_0\" in a typical VMD. 1=\"Weapon Bone 2\" in .frg but \"be_prop_1\" in VMD. etc.")]
        public int PropBoneId { get; set; }

        [MetaDataTag(6)]
        public float BlendInTime { get; set; }

        [MetaDataTag(7)]
        public float BlendOutTime { get; set; }

        public virtual string AnimationSlotName { get; } = "";
        public virtual string[] SkeletonNameAlternatives { get; } = new string[] { "" };
    }

    /// <summary>
    /// Dock right hand
    /// </summary>
    [MetaData("DOCK_EQPT_RHAND", 3)]
    public class DockEquipmentRHand_v3 : DockEquipment_v3
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_RIGHT_HAND";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "hand_right" };
    }

    [MetaData("DOCK_EQPT_RHAND", 10)]
    public class DockEquipmentRHand_v10 : DockEquipment
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_RIGHT_HAND";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "hand_right" };
    }

    [MetaData("DOCK_EQPT_RHAND", 11)]
    public class DockEquipmentRHand_v11 : DockEquipmentRHand_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    // TODO: proper inheritance
    [MetaData("DOCK_EQPT_RHAND_2", 11)]
    public class DockEquipmentRHand2_v11 : DockEquipmentRHand_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("DOCK_EQPT_RHAND", 11, MetaDataAttributePriority.Low)]
    public class DockEquipmentRHand_v11_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownFloat { get; set; }
    }


    [MetaData("DOCK_EQPT_RHAND", 14, MetaDataAttributePriority.Low)]
    public class DockEquipmentRHand_v14_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownByte0 { get; set; }

        [MetaDataTag(9)]
        public byte UnknownByte1 { get; set; }

        [MetaDataTag(10)]
        public byte UnknownByte4 { get; set; }

        [MetaDataTag(11)]
        public float UnknownFloat0 { get; set; }

        [MetaDataTag(12)]
        public float UnknownFloat1 { get; set; }

        [MetaDataTag(13)]
        public float UnknownFloat2 { get; set; }

        [MetaDataTag(14)]
        public float UnknownFloat3 { get; set; }

        [MetaDataTag(15)]
        public float UnknownFloat4 { get; set; }
    }


    /// <summary>
    /// Dock left hand
    /// </summary>
    [MetaData("DOCK_EQPT_LHAND", 3)]
    public class DockEquipmentLHand_v3 : DockEquipment_v3
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_LEFT_HAND";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "hand_left" };
    }

    [MetaData("DOCK_EQPT_LHAND", 10)]
    public class DockEquipmentLHand_v10 : DockEquipment
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_LEFT_HAND";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "hand_left" };
    }

    [MetaData("DOCK_EQPT_LHAND", 11)]
    public class DockEquipmentLHand_v11 : DockEquipmentLHand_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    // TODO: proper inheritance
    [MetaData("DOCK_EQPT_LHAND_2", 11)]
    public class DockEquipmentLHand2_v11 : DockEquipmentLHand_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("DOCK_EQPT_LHAND", 11, MetaDataAttributePriority.Low)]
    public class DockEquipmentLHand_v11_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownFloat { get; set; }
    }


    [MetaData("DOCK_EQPT_LHAND", 14, MetaDataAttributePriority.Low)]
    public class DockEquipmentLHand_v14_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownByte0 { get; set; }

        [MetaDataTag(9)]
        public byte UnknownByte1 { get; set; }

        [MetaDataTag(10)]
        public byte UnknownByte4 { get; set; }

        [MetaDataTag(11)]
        public float UnknownFloat0 { get; set; }

        [MetaDataTag(12)]
        public float UnknownFloat1 { get; set; }

        [MetaDataTag(13)]
        public float UnknownFloat2 { get; set; }

        [MetaDataTag(14)]
        public float UnknownFloat3 { get; set; }

        [MetaDataTag(15)]
        public float UnknownFloat4 { get; set; }
    }

    /// <summary>
    /// Dock right waits
    /// </summary>
    [MetaData("DOCK_EQPT_RWAIST", 10)]
    public class DockEquipmentRWaist_v10 : DockEquipment
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_RIGHT_WAIST";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "root" };
    }

    [MetaData("DOCK_EQPT_RWAIST", 11)]
    public class DockEquipmentRWaist_v11 : DockEquipmentRWaist_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("DOCK_EQPT_RWAIST", 11, MetaDataAttributePriority.Low)]
    public class DockEquipmentRWaist_v11_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownFloat { get; set; }
    }

    [MetaData("DOCK_EQPT_RWAIST", 14, MetaDataAttributePriority.Low)]
    public class DockEquipmentRWaist_v14_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownByte0 { get; set; }

        [MetaDataTag(9)]
        public byte UnknownByte1 { get; set; }

        [MetaDataTag(10)]
        public byte UnknownByte4 { get; set; }

        [MetaDataTag(11)]
        public float UnknownFloat0 { get; set; }

        [MetaDataTag(12)]
        public float UnknownFloat1 { get; set; }

        [MetaDataTag(13)]
        public float UnknownFloat2 { get; set; }

        [MetaDataTag(14)]
        public float UnknownFloat3 { get; set; }

        [MetaDataTag(15)]
        public float UnknownFloat4 { get; set; }
    }

    /// <summary>
    /// Dock left waist
    /// </summary>
    [MetaData("DOCK_EQPT_LWAIST", 3)]
    public class DockEquipmentLWaist_v3 : DockEquipment_v3
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_LEFT_WAIST";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "root" };
    }
    [MetaData("DOCK_EQPT_LWAIST", 10)]
    public class DockEquipmentLWaist_v10 : DockEquipment
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_LEFT_WAIST";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "root" };
    }

    [MetaData("DOCK_EQPT_LWAIST", 11)]
    public class DockEquipmentLWaist_v11 : DockEquipmentLWaist_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("DOCK_EQPT_LWAIST", 11, MetaDataAttributePriority.Low)]
    public class DockEquipmentLWaist_v11_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownFloat { get; set; }
    }

    [MetaData("DOCK_EQPT_LWAIST", 14, MetaDataAttributePriority.Low)]
    public class DockEquipmentLWaist_v14_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownByte0 { get; set; }

        [MetaDataTag(9)]
        public byte UnknownByte1 { get; set; }

        [MetaDataTag(10)]
        public byte UnknownByte4 { get; set; }

        [MetaDataTag(11)]
        public float UnknownFloat0 { get; set; }

        [MetaDataTag(12)]
        public float UnknownFloat1 { get; set; }

        [MetaDataTag(13)]
        public float UnknownFloat2 { get; set; }

        [MetaDataTag(14)]
        public float UnknownFloat3 { get; set; }

        [MetaDataTag(15)]
        public float UnknownFloat4 { get; set; }
    }

    /// <summary>
    /// Dock back
    /// </summary>
    [MetaData("DOCK_EQPT_BACK", 3)]
    public class DockEquipmentBack_v3 : DockEquipment_v3
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_BACK";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "spine_2" };
    }

    [MetaData("DOCK_EQPT_BACK", 10)]
    public class DockEquipmentBack_v10 : DockEquipment
    {
        public override string AnimationSlotName => "DOCK_EQUIPMENT_BACK";
        public override string[] SkeletonNameAlternatives { get; } = new string[] { "spine_2" };
    }

    [MetaData("DOCK_EQPT_BACK", 11)]
    public class DockEquipmentBack_v11 : DockEquipmentBack_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("DOCK_EQPT_BACK", 11, MetaDataAttributePriority.Low)]
    public class DockEquipmentBack_v11_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownFloat { get; set; }
    }

    [MetaData("DOCK_EQPT_BACK", 14, MetaDataAttributePriority.Low)]
    public class DockEquipmentBack_v14_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownByte0 { get; set; }

        [MetaDataTag(9)]
        public byte UnknownByte1 { get; set; }

        [MetaDataTag(10)]
        public byte UnknownByte4 { get; set; }

        [MetaDataTag(11)]
        public float UnknownFloat0 { get; set; }

        [MetaDataTag(12)]
        public float UnknownFloat1 { get; set; }

        [MetaDataTag(13)]
        public float UnknownFloat2 { get; set; }

        [MetaDataTag(14)]
        public float UnknownFloat3 { get; set; }

        [MetaDataTag(15)]
        public float UnknownFloat4 { get; set; }
    }

    [MetaData("WEAPON_HIP", 2)]
    public class WeaponHip_v2 : DockEquipment_v2
    {
    }

    [MetaData("WEAPON_HIP", 3)]
    public class WeaponHip_v3 : DockEquipment_v3
    {
    }

    [MetaData("WEAPON_HIP", 10)]
    public class WeaponHip_v10 : DockEquipment
    {
    }

    [MetaData("WEAPON_HIP", 11)]
    public class WeaponHip_v11 : WeaponHip_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("WEAPON_RHAND", 0)]
    public class WeaponRHand_v0 : DockEquipment_v0
    {
    }

    [MetaData("WEAPON_RHAND", 1)]
    public class WeaponRHand_v1 : DockEquipment_v1
    {
    }

    [MetaData("WEAPON_RHAND", 2)]
    public class WeaponRHand_v2 : DockEquipment_v2
    {
    }

    [MetaData("WEAPON_RHAND", 3)]
    public class WeaponRHand_v3 : DockEquipment_v3
    {
    }

    [MetaData("WEAPON_RHAND", 10)]
    public class WeaponRHand_v10 : DockEquipment
    {
    }

    [MetaData("WEAPON_RHAND", 11)]
    public class WeaponRHand_v11 : WeaponRHand_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("WEAPON_RHAND", 11, MetaDataAttributePriority.Low)]
    public class WeaponRHand_v11_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownFloat { get; set; }
    }




    [MetaData("WEAPON_LHAND", 0)]
    public class WeaponLHand_v0 : DockEquipment_v0
    {
    }


    [MetaData("WEAPON_LHAND", 1)]
    public class WeaponLHand_v1 : DockEquipment_v1
    {
    }


    [MetaData("WEAPON_LHAND", 2)]
    public class WeaponLHand_v2 : DockEquipment_v2
    {
    }

    [MetaData("WEAPON_LHAND", 3)]
    public class WeaponLHand_v3 : DockEquipment_v3
    {
    }

    [MetaData("WEAPON_LHAND", 10)]
    public class WeaponLHand_v10 : DockEquipment
    {
    }


    [MetaData("WEAPON_LHAND", 11)]
    public class WeaponLHand_v11 : WeaponLHand_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("WEAPON_LHAND", 11, MetaDataAttributePriority.Low)]
    public class WeaponLHand_v11_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownFloat { get; set; }
    }


    [MetaData("WEAPON_ON", 2)]
    public class WeaponOn_v2 : DockEquipment_v2
    {
    }

    [MetaData("WEAPON_ON", 3)]
    public class WeaponOn_v3 : DockEquipment_v3
    {
    }

    [MetaData("WEAPON_ON", 10)]
    public class WeaponOn_v10 : DockEquipment
    {
    }

    [MetaData("WEAPON_ON", 11)]
    public class WeaponOn_v11 : WeaponOn_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
    }

    [MetaData("WEAPON_ON", 11, MetaDataAttributePriority.Low)]
    public class WeaponOn_v11_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownFloat { get; set; }
    }

    [MetaData("WEAPON_ON", 14, MetaDataAttributePriority.Low)]
    public class WeaponOn_v14_Troy : DockEquipmentLHand_v10
    {
        public override string Description { get; } = "_TroyOnly";

        [MetaDataTag(8)]
        public byte UnknownByte0 { get; set; }

        [MetaDataTag(9)]
        public byte UnknownByte1 { get; set; }

        [MetaDataTag(10)]
        public byte UnknownByte4 { get; set; }

        [MetaDataTag(11)]
        public float UnknownFloat0 { get; set; }

        [MetaDataTag(12)]
        public float UnknownFloat1 { get; set; }

        [MetaDataTag(13)]
        public float UnknownFloat2 { get; set; }

        [MetaDataTag(14)]
        public float UnknownFloat3 { get; set; }

        [MetaDataTag(15)]
        public float UnknownFloat4 { get; set; }
    }
}
