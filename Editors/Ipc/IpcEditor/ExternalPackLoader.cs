using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;

namespace Editors.Ipc
{
    public class ExternalPackLoader : IExternalPackLoader
    {
        private readonly ILogger _logger = Logging.Create<ExternalPackLoader>();
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IUiDispatcher _uiDispatcher;

        public ExternalPackLoader(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader, IUiDispatcher uiDispatcher)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _uiDispatcher = uiDispatcher;
        }

        public async Task<PackLoadResult> EnsureLoadedAsync(string packPathOnDisk, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(packPathOnDisk))
                return PackLoadResult.Ok();

            var normalizedDiskPath = NormalizeDiskPath(packPathOnDisk);
            if (string.IsNullOrWhiteSpace(normalizedDiskPath))
                return PackLoadResult.Fail("Pack path is empty");

            try
            {
                // The pack loader owns WPF wait-cursor handling, so both loading and
                // publishing the new container must run on the application dispatcher.
                return await _uiDispatcher.InvokeAsync(
                    () => EnsureLoadedOnUiThread(normalizedDiskPath),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Here().Error(ex, $"Failed loading external pack file {normalizedDiskPath}");
                return PackLoadResult.Fail("Pack file load failed");
            }
        }

        private PackLoadResult EnsureLoadedOnUiThread(string normalizedDiskPath)
        {
            if (_packFileService.IsPackFileLoaded(normalizedDiskPath))
                return PackLoadResult.Ok();

            var container = _packFileContainerLoader.CreateFromPackFile(PackFileContainerType.Normal, normalizedDiskPath, true);
            if (container == null)
                return PackLoadResult.Fail("Pack file could not be loaded");

            var added = _packFileService.AddContainer(container, false);
            if (added == null)
                return PackLoadResult.Fail("Pack file could not be added");

            _logger.Here().Information($"Externally loaded pack file {normalizedDiskPath}");
            return PackLoadResult.Ok();
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
    }
}
