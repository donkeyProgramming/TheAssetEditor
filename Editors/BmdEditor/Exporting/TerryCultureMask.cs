using Shared.GameFormats.Bmd;

namespace Editors.BmdEditor.Exporting
{
    /// <summary>
    /// Formats BMD culture masks into Terry's comma-separated culture_mask attribute text
    /// (e.g. "wh3_main_cth_cathay,wh3_main_ksl_kislev"), and resolves how an entity's own mask
    /// combines with the mask inherited from an enclosing BmdInfo reference. The per-type
    /// combination rules (prefer own vs. prefer inherited vs. own-only) mirror what the reference
    /// Terry-export scripts do for each BMD component type.
    /// </summary>
    public static class TerryCultureMask
    {
        // Ideally these bit->culture names would be looked up from the game DB (e.g. a
        // "prefab_types_tables"-style table) instead of hardcoded here. AssetEditor has no live
        // DB-table-reading path to do that with, though: Shared/GameFiles/DB (SchemaManager,
        // SimpleSchema) is schema-driven table-decoding scaffolding, but every class in it is
        // commented out/dead, so there's nothing to hook into short of building that
        // infrastructure from scratch. Hardcoded here instead, matching the reference scripts.
        private static readonly Dictionary<int, string> BitNames = new()
        {
            [6] = "wh_dlc03_bst_beastmen",
            [7] = "wh_main_brt_bretonnia",
            [8] = "wh_main_chs_chaos",
            [9] = "wh_main_dwf_dwarfs",
            [10] = "wh_main_emp_empire",
            [11] = "wh_main_grn_greenskins",
            [12] = "wh_main_vmp_vampire_counts",
            [13] = "wh_dlc05_wef_wood_elves",
            [17] = "wh2_main_def_dark_elves",
            [18] = "wh2_main_hef_high_elves",
            [19] = "wh2_main_lzd_lizardmen",
            [20] = "wh2_main_skv_skaven",
            [21] = "wh2_dlc09_tmb_tomb_kings",
            [22] = "wh2_main_rogue",
            [23] = "wh3_main_ksl_kislev",
            [24] = "wh3_main_ogr_ogre_kingdoms",
            [25] = "wh2_dlc11_cst_vampire_coast",
            [27] = "wh3_main_kho_khorne",
            [28] = "wh3_main_tze_tzeentch",
            [29] = "wh3_main_nur_nurgle",
            [30] = "wh3_main_sla_slaanesh",
            [31] = "wh3_main_dae_daemons",
            [32] = "wh3_main_cth_cathay",
            [33] = "wh_dlc08_nor_norsca",
            [34] = "wh3_dlc23_chd_chaos_dwarfs",
            [63] = "*",
        };

        public static string Format(ulong bits)
        {
            if (bits == 0)
                return "";

            var names = new List<string>();
            for (var i = 0; i < 64; i++)
            {
                if ((bits & (1UL << i)) != 0 && BitNames.TryGetValue(i, out var name))
                    names.Add(name);
            }
            return string.Join(",", names);
        }

        public static string Format(byte[]? raw8)
        {
            if (raw8 == null || raw8.Length < 8)
                return "";
            return Format(BitConverter.ToUInt64(raw8, 0));
        }

        public static string Format(CultureMask mask)
        {
            var bits = 0UL;
            if (mask.CultMaskBst) bits |= 1UL << 6;
            if (mask.CultMaskBrt) bits |= 1UL << 7;
            if (mask.CultMaskChs) bits |= 1UL << 8;
            if (mask.CultMaskDwf) bits |= 1UL << 9;
            if (mask.CultMaskEmp) bits |= 1UL << 10;
            if (mask.CultMaskGrn) bits |= 1UL << 11;
            if (mask.CultMaskVmp) bits |= 1UL << 12;
            if (mask.CultMaskWef) bits |= 1UL << 13;
            if (mask.CultMaskDef) bits |= 1UL << 17;
            if (mask.CultMaskHef) bits |= 1UL << 18;
            if (mask.CultMaskLzd) bits |= 1UL << 19;
            if (mask.CultMaskSkv) bits |= 1UL << 20;
            if (mask.CultMaskTmb) bits |= 1UL << 21;
            if (mask.CultMaskRogue) bits |= 1UL << 22;
            if (mask.CultMaskKsl) bits |= 1UL << 23;
            if (mask.CultMaskOgr) bits |= 1UL << 24;
            if (mask.CultMaskCst) bits |= 1UL << 25;
            if (mask.CultMaskKho) bits |= 1UL << 27;
            if (mask.CultMaskTze) bits |= 1UL << 28;
            if (mask.CultMaskNur) bits |= 1UL << 29;
            if (mask.CultMaskSla) bits |= 1UL << 30;
            if (mask.CultMaskDae) bits |= 1UL << 31;
            if (mask.CultMaskCth) bits |= 1UL << 32;
            if (mask.CultMaskNor) bits |= 1UL << 33;
            if (mask.CultMaskChd) bits |= 1UL << 34;
            return Format(bits);
        }

        /// <summary>Props: keep the prop's own mask; only fall back to the inherited (BmdInfo)
        /// mask when the prop itself carries no restriction.</summary>
        public static string PreferOwn(string own, string inherited) => own.Length > 0 ? own : inherited;

        /// <summary>VFX: the inherited (BmdInfo) mask wins whenever it's non-empty, regardless of
        /// the VFX's own mask.</summary>
        public static string PreferInherited(string own, string inherited) => inherited.Length > 0 ? inherited : own;
    }
}
