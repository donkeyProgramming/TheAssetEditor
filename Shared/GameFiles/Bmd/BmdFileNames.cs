namespace Shared.GameFormats.Bmd
{
    /// <summary>
    /// Which files are BMDs: <c>.bmd</c> prefabs, and battle maps' <c>.bin</c> files under terrain/tiles/battle.
    /// <c>.bin</c> is shared with other formats, so a <c>.bin</c> only counts when its name matches <see cref="BinNameParts"/>.
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
