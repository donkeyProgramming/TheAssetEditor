using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace AssetEditor.Services.Ipc
{
    public class ExternalPackLoader : IExternalPackLoader
    {
        private readonly ILogger _logger = Logging.Create<ExternalPackLoader>();
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;

        public ExternalPackLoader(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
        }

        public Task<PackLoadResult> EnsureLoadedAsync(string packPathOnDisk, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(packPathOnDisk))
                return Task.FromResult(PackLoadResult.Ok());

            var normalizedDiskPath = NormalizeDiskPath(packPathOnDisk);
            if (string.IsNullOrWhiteSpace(normalizedDiskPath))
                return Task.FromResult(PackLoadResult.Fail("Pack path is empty"));

            var alreadyLoaded = _packFileService
                .GetAllPackfileContainers()
                .Any(x =>
                    PathsEqual(x.SystemFilePath, normalizedDiskPath)
                    || x.SourcePackFilePaths.Any(sourcePath => PathsEqual(sourcePath, normalizedDiskPath)));

            if (alreadyLoaded)
                return Task.FromResult(PackLoadResult.Ok());

            try
            {
                var container = _packFileContainerLoader.Load(normalizedDiskPath);
                if (container == null)
                    return Task.FromResult(PackLoadResult.Fail("Pack file could not be loaded"));

                var added = AddContainerOnUiThread(container);
                if (added == null)
                    return Task.FromResult(PackLoadResult.Fail("Pack file could not be added"));

                _logger.Here().Information($"Externally loaded pack file {normalizedDiskPath}");
                return Task.FromResult(PackLoadResult.Ok());
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, $"Failed loading external pack file {normalizedDiskPath}");
                return Task.FromResult(PackLoadResult.Fail("Pack file load failed"));
            }
        }

        private PackFileContainer AddContainerOnUiThread(PackFileContainer container)
        {
            var app = Application.Current;
            if (app?.Dispatcher == null || app.Dispatcher.CheckAccess())
                return _packFileService.AddContainer(container, false);

            return app.Dispatcher.Invoke(() => _packFileService.AddContainer(container, false));
        }

        private static string NormalizeDiskPath(string input)
        {
            var path = input.Trim();

            if (path.Length >= 2)
            {
                var first = path[0];
                var last = path[path.Length - 1];
                var hasMatchingQuotes = (first == '"' && last == '"') || (first == '\'' && last == '\'');
                if (hasMatchingQuotes)
                    path = path.Substring(1, path.Length - 2);
            }

            path = path.Replace('/', '\\');

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            var normalizedLeft = left.Replace('/', '\\').Trim();
            var normalizedRight = right.Replace('/', '\\').Trim();
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }
    }
}
