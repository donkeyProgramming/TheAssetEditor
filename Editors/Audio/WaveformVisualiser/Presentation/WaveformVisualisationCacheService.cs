using System.Collections.Concurrent;

namespace Editors.Audio.WaveformVisualiser.Presentation
{
    public sealed record WemWaveformSource(string CacheKey, Func<byte[]> LoadBytes);

    public interface IWaveformVisualisationCacheService
    {
        WaveformRenderResult GetWaveformVisualisation(string filePath, int targetWidth);
        void Store(string filePath, WaveformRenderResult waveformRenderResult);
        void Remove(string filePath);
        Task PreloadWaveformVisualisationsAsync(IEnumerable<string> filePaths, int targetWidth, IWaveformRendererService renderService, CancellationToken cancellationToken);
        Task<WaveformRenderResult> GetOrRenderWemAsync(WemWaveformSource source, int targetWidth, IWaveformRendererService renderService, CancellationToken cancellationToken);
        Task PreloadWemWaveformVisualisationsAsync(IEnumerable<WemWaveformSource> sources, int targetWidth, IWaveformRendererService renderService, CancellationToken cancellationToken);
    }

    public sealed class WaveformVisualisationCacheService : IWaveformVisualisationCacheService
    {
        private readonly ConcurrentDictionary<string, WaveformRenderResult> _visualisationByFilePath = new();
        private readonly ConcurrentDictionary<string, byte> _preloadInProgressByFilePath = new();
        private readonly ConcurrentDictionary<string, byte> _removedDuringPreloadByFilePath = new();
        private readonly ConcurrentDictionary<string, Task<WaveformRenderResult>> _wemRenderInProgressByKey = new();

        public WaveformRenderResult GetWaveformVisualisation(string filePath, int targetWidth)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            if (_visualisationByFilePath.TryGetValue(filePath, out var cached) && cached.PixelWidth == targetWidth)
                return cached;

            return null;
        }

        public void Store(string filePath, WaveformRenderResult waveformRenderResult)
        {
            _visualisationByFilePath[filePath] = waveformRenderResult;
        }

        public void Remove(string filePath)
        {
            _removedDuringPreloadByFilePath[filePath] = 0;
            _visualisationByFilePath.TryRemove(filePath, out _);
            _preloadInProgressByFilePath.TryRemove(filePath, out _);
        }

        public async Task PreloadWaveformVisualisationsAsync(IEnumerable<string> filePaths, int targetWidth, IWaveformRendererService renderService, CancellationToken cancellationToken)
        {
            var uniqueFilePaths = (filePaths ?? [])
                .Where(filePath => !string.IsNullOrWhiteSpace(filePath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(filePath => !_visualisationByFilePath.TryGetValue(filePath, out var existingfilePath) || existingfilePath.PixelWidth != targetWidth)
                .Where(filePath => _preloadInProgressByFilePath.TryAdd(filePath, 0))
                .ToArray();

            if (uniqueFilePaths.Length == 0)
                return;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
                CancellationToken = cancellationToken
            };

            try
            {
                await Parallel.ForEachAsync(uniqueFilePaths, options, async (filePath, cancellationToken) =>
                {
                    try
                    {
                        _removedDuringPreloadByFilePath.TryRemove(filePath, out _);

                        var waveformRenderResult = await renderService.RenderAsync(filePath, targetWidth, cancellationToken).ConfigureAwait(false);

                        if (_removedDuringPreloadByFilePath.ContainsKey(filePath))
                            return;

                        _visualisationByFilePath[filePath] = waveformRenderResult;
                    }
                    finally
                    {
                        _preloadInProgressByFilePath.TryRemove(filePath, out _);
                    }
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        public async Task<WaveformRenderResult> GetOrRenderWemAsync(WemWaveformSource source, int targetWidth, IWaveformRendererService renderService, CancellationToken cancellationToken)
        {
            var cached = GetWaveformVisualisation(source.CacheKey, targetWidth);
            if (cached != null)
                return cached;

            var renderKey = $"{source.CacheKey}|{targetWidth}";
            var renderTask = _wemRenderInProgressByKey.GetOrAdd(
                renderKey,
                _ => RenderAndStoreWemAsync(source, targetWidth, renderService));

            try
            {
                return await renderTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (renderTask.IsCompleted)
                    _wemRenderInProgressByKey.TryRemove(new KeyValuePair<string, Task<WaveformRenderResult>>(renderKey, renderTask));
            }
        }

        public async Task PreloadWemWaveformVisualisationsAsync(IEnumerable<WemWaveformSource> sources, int targetWidth, IWaveformRendererService renderService, CancellationToken cancellationToken)
        {
            var uniqueSources = (sources ?? [])
                .Where(source => source != null && !string.IsNullOrWhiteSpace(source.CacheKey))
                .DistinctBy(source => source.CacheKey, StringComparer.OrdinalIgnoreCase)
                .Where(source => GetWaveformVisualisation(source.CacheKey, targetWidth) == null)
                .ToArray();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
                CancellationToken = cancellationToken
            };

            try
            {
                await Parallel.ForEachAsync(uniqueSources, options, async (source, token) =>
                {
                    await GetOrRenderWemAsync(source, targetWidth, renderService, token).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        private async Task<WaveformRenderResult> RenderAndStoreWemAsync(WemWaveformSource source, int targetWidth, IWaveformRendererService renderService)
        {
            var wemBytes = await Task.Run(source.LoadBytes).ConfigureAwait(false);
            var result = await renderService.RenderFromWemBytesAsync(wemBytes, targetWidth, CancellationToken.None).ConfigureAwait(false);
            Store(source.CacheKey, result);
            return result;
        }
    }
}
