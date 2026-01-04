using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Editors.Audio.AudioEditor.Presentation.WaveformVisualiser
{
    public interface IWaveformVisualisationCacheService
    {
        bool GetWaveformVisualisation(string filePath, int targetWidth, out WaveformVisualisation visualisation);
        void Store(string filePath, WaveformVisualisation visualisation);
        void Remove(string filePath);
        Task PreloadWaveformVisualisationsAsync(IEnumerable<string> filePaths, int targetWidth, IWaveformRendererService renderService, CancellationToken cancellationToken);
    }

    public sealed class WaveformVisualisationCacheService : IWaveformVisualisationCacheService
    {
        private readonly ConcurrentDictionary<string, WaveformVisualisation> _visualisationByFilePath = new();
        private readonly ConcurrentDictionary<string, byte> _preloadInProgressByFilePath = new();
        private readonly ConcurrentDictionary<string, byte> _removedDuringPreloadByFilePath = new();

        public bool GetWaveformVisualisation(string filePath, int targetWidth, out WaveformVisualisation waveformVisualisation)
        {
            if (_visualisationByFilePath.TryGetValue(filePath, out var cached) && cached.PixelWidth == targetWidth)
            {
                waveformVisualisation = cached;
                return true;
            }

            waveformVisualisation = null;
            return false;
        }

        public void Store(string filePath, WaveformVisualisation visualisation)
        {
            _visualisationByFilePath[filePath] = visualisation;
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

                        var waveformVisualisation = await renderService.RenderAsync(filePath, targetWidth, cancellationToken).ConfigureAwait(false);

                        if (_removedDuringPreloadByFilePath.ContainsKey(filePath))
                            return;

                        _visualisationByFilePath[filePath] = waveformVisualisation;
                    }
                    finally
                    {
                        _preloadInProgressByFilePath.TryRemove(filePath, out _);
                    }
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }
}
