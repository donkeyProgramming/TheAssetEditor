using System.Windows;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;

/// <summary>
/// Writes all import outputs in one call, on the UI thread. Unless overwriting, it writes only
/// when none of the target paths exist yet, and returns the conflicting paths when it writes nothing.
/// </summary>
internal static class PackFileDispatcherWriter
{
    public static IReadOnlyList<string> AddFilesToPackIfNoConflicts(
        IPackFileService packFileService,
        IPackFileContainer container,
        List<NewPackFileEntry> files,
        IReadOnlyList<string> targetPaths,
        bool overwriteExisting = false)
    {
        IReadOnlyList<string> AddFiles()
        {
            var conflicts = overwriteExisting ? [] : targetPaths
                .Where(container.ContainsFile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (conflicts.Count != 0)
                return conflicts;

            packFileService.AddFilesToPack(container, files);
            return [];
        }

        // AddFilesToPack raises events the pack tree handles, so it has to run on the UI thread.
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            return AddFiles();

        return dispatcher.Invoke(AddFiles);
    }
}
