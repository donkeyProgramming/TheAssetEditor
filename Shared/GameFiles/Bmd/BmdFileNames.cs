namespace Shared.GameFormats.Bmd
{
    /// <summary>
    /// Which files are BMDs. Prefabs use <c>.bmd</c>, but battle maps keep theirs as <c>.bin</c> files under
    /// terrain/tiles/battle (bmd_data.bin, catchment_NN_layer_bmd_data.bin, *_procedural_bmd_data.bin, ...).
    /// <c>.bin</c> is shared with other formats (even other FASTBIN0 ones such as battle_locations_map.bin and
    /// tile_list.bin), so a <c>.bin</c> only counts when its name matches one of <see cref="BinNameParts"/>.
    /// In vanilla WH3 these cover all 15,871 BMD <c>.bin</c> files and nothing else.
    /// </summary>
    public static class BmdFileNames
    {
        public const string BmdExtension = ".bmd";
        public const string BinExtension = ".bin";

        public static readonly string[] BinNameParts = ["bmd_data", "bmd_nogo_data", "bmd_objects_sound", "_bmd.bin"];

        public static bool IsPrefab(string fileName) => fileName.EndsWith(BmdExtension, StringComparison.OrdinalIgnoreCase);

        public static bool IsBmd(string fileName)
        {
            if (IsPrefab(fileName))
                return true;
            var name = Path.GetFileName(fileName).ToLowerInvariant();
            return name.EndsWith(BinExtension) && BinNameParts.Any(name.Contains);
        }
    }
}
