using Shared.Core.PackFiles;
using Shared.GameFormats.Bmd;

namespace Editors.BmdEditor.Exporting
{
    /// <summary>Resolves a BmdInfo reference's path to its parsed <see cref="BmdFile"/> via the pack
    /// file system, for recursively flattening nested BMDs during Terry export.</summary>
    public static class BmdReferenceResolver
    {
        public static Func<string, BmdFile?> Create(IPackFileService packFileService) => path =>
        {
            var packFile = packFileService.FindFile(path);
            if (packFile == null)
                return null;
            return BmdParser.Parse(packFile.DataSource.ReadData());
        };
    }
}
