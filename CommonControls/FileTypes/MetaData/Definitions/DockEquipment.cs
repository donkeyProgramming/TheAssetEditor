using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{

    public abstract class DockEquipment : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "0=\"Weapon Bone 1\" in .frg but \"be_prop_0\" in a typical VMD. 1=\"Weapon Bone 2\" in .frg but \"be_prop_1\" in VMD. etc.")]
        public int PropBoneId { get; set; }

        [MetaDataTag(6)]
        public float BlendInTime { get; set; }

        [MetaDataTag(7)]
        public float BlendOutTime { get; set; }

        public virtual string AnimationSlotName { get; }
        public virtual string[] SkeletonNameAlternatives { get; } = new string[] { "" };
    }

    /// <summary>
    /// Dock right hand
    /// </summary>
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

    /// <summary>
    /// Dock left hand
    /// </summary>
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

    /// <summary>
    /// Dock left waist
    /// </summary>
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

    /// <summary>
    /// Dock back
    /// </summary>
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



    [MetaData("WEAPON_HIP", 10)]
    public class WeaponHip_v10 : DockEquipment
    {
    }

    [MetaData("WEAPON_RHAND", 10)]
    public class WeaponRHand_v10 : DockEquipment
    {
    }

    [MetaData("WEAPON_RHAND", 11)]
    public class WeaponRHand_v11: WeaponRHand_v10
    {
        [MetaDataTag(8)]
        public float UnknownFloat { get; set; }
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
}
