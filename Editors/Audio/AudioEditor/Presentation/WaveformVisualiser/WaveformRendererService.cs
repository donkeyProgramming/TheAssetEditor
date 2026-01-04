using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
using Shared.Core.PackFiles;
using Color = System.Drawing.Color;
using DrawingImage = System.Drawing.Image;

namespace Editors.Audio.AudioEditor.Presentation.WaveformVisualiser
{
    public record WaveformRenderResult(WaveformVisualisation Visualisation, TimeSpan TotalTime, int PixelWidth);

    public interface IWaveformRendererService
    {
        Task<WaveformRenderResult> RenderAsync(string filePathKey, int targetWidth, CancellationToken cancellationToken);
    }

    public sealed class WaveformRendererService(IPackFileService packFileService) : IWaveformRendererService
    {
        private readonly IPackFileService _packFileService = packFileService;

        public static int DefaultPixelsPerPeak { get; set; } = 2;
        public static int DefaultSpacerPixels { get; set; } = 1;

        public async Task<WaveformRenderResult> RenderAsync(string filePathKey, int targetWidth, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(filePathKey))
                throw new ArgumentNullException(nameof(filePathKey));

            var packFile = _packFileService.FindFile(filePathKey);
            var data = packFile.DataSource.ReadData();

            var baseSettings = CreateBaseWaveformSettings(targetWidth);
            var overlaySettings = CreateOverlayWaveformSettings(targetWidth);

            return await Task.Run(() => RenderWaveformFromBytes(data, packFile.Extension, baseSettings, overlaySettings), cancellationToken).ConfigureAwait(false);
        }

        private static WaveformRenderResult RenderWaveformFromBytes(byte[] data, string extension, WaveFormRendererSettings baseSettings, WaveFormRendererSettings overlaySettings)
        {
            using var memoryStream = new MemoryStream(data, writable: false);
            using var waveStream = new WaveFileReader(memoryStream);
            using var alignedWaveStream = new BlockAlignReductionStream(waveStream);

            var waveFormRenderer = new WaveFormRenderer();

            using var baseImageDrawing = waveFormRenderer.Render(alignedWaveStream, baseSettings);
            alignedWaveStream.Position = 0;
            using var overlayImageDrawing = waveFormRenderer.Render(alignedWaveStream, overlaySettings);

            var baseBitmap = ToBitmapImage(baseImageDrawing);
            var overlayBitmap = ToBitmapImage(overlayImageDrawing);

            var waveformVisualisation = WaveformVisualisation.Create(baseBitmap, overlayBitmap);
            var totalTime = waveStream.TotalTime;
            var pixelWidth = baseBitmap.PixelWidth;

            return new WaveformRenderResult(waveformVisualisation, totalTime, pixelWidth);
        }

        private static SoundCloudBlockWaveFormSettings CreateBaseWaveformSettings(int width)
        {
            return new SoundCloudBlockWaveFormSettings(
                Color.FromArgb(196, 230, 230, 230), // top peak
                Color.FromArgb(64, 220, 220, 220), // top spacer
                Color.FromArgb(196, 210, 210, 210), // bottom peak
                Color.FromArgb(64, 190, 190, 190)) // bottom spacer
            {
                Width = width,
                PixelsPerPeak = DefaultPixelsPerPeak,
                SpacerPixels = DefaultSpacerPixels,
                TopSpacerGradientStartColor = Color.FromArgb(64, 220, 220, 220),
                BackgroundColor = Color.Transparent
            };
        }

        private static SoundCloudBlockWaveFormSettings CreateOverlayWaveformSettings(int width)
        {
            return new SoundCloudBlockWaveFormSettings(
                Color.FromArgb(255, 255, 68, 0), // top peak
                Color.FromArgb(64, 255, 68, 0), // top spacer
                Color.FromArgb(255, 255, 191, 153), // bottom peak
                Color.FromArgb(128, 255, 191, 153)) // bottom spacer
            {
                Width = width,
                PixelsPerPeak = DefaultPixelsPerPeak,
                SpacerPixels = DefaultSpacerPixels,
                TopSpacerGradientStartColor = Color.FromArgb(64, 255, 68, 0),
                BackgroundColor = Color.Transparent
            };
        }

        private static BitmapImage ToBitmapImage(DrawingImage drawingImage)
        {
            using var memoryStream = new MemoryStream();

            try
            {
                drawingImage.Save(memoryStream, ImageFormat.Png);
            }
            catch (ArgumentNullException)
            {
                // Sometimes the encoder isn't initialised at the start so we delay then retry.
                Thread.Sleep(50);
                drawingImage.Save(memoryStream, ImageFormat.Png);
            }

            memoryStream.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memoryStream;
            image.EndInit();
            image.Freeze();
            return image;
        }

    }
}
