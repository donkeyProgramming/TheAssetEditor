using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
using Shared.Core.PackFiles;
using Shared.GameFormats.Wwise.Wem.V132;
using Shared.GameFormats.Wwise.Wem.V132.Decoding;
using Shared.GameFormats.Wwise.Wem.V132.Encoding;
using Color = System.Drawing.Color;
using DrawingImage = System.Drawing.Image;

namespace Editors.Audio.WaveformVisualiser.Presentation
{
    public record WaveformRenderResult(WaveformVisualisation Visualisation, TimeSpan TotalTime, int PixelWidth);

    public interface IWaveformRendererService
    {
        Task<WaveformRenderResult> RenderAsync(string filePathKey, int targetWidth, CancellationToken cancellationToken);
        Task<WaveformRenderResult> RenderFromWemBytesAsync(byte[] wemBytes, int targetWidth, CancellationToken cancellationToken);
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

            return await Task.Run(() => RenderWaveformFromWavBytes(data, baseSettings, overlaySettings), cancellationToken).ConfigureAwait(false);
        }

        public async Task<WaveformRenderResult> RenderFromWemBytesAsync(byte[] wemBytes, int targetWidth, CancellationToken cancellationToken)
        {
            if (wemBytes == null || wemBytes.Length == 0)
                throw new ArgumentNullException(nameof(wemBytes));

            var baseSettings = CreateBaseWaveformSettings(targetWidth);
            var overlaySettings = CreateOverlayWaveformSettings(targetWidth);

            return await Task.Run(() =>
            {
                var codebookLibrary = new WwiseCodebookLibrary();
                var decoder = new WemVorbisDecoder(codebookLibrary);
                var oggBytes = decoder.Decode(WemFile.FromBytes(wemBytes)).ToOgg();
                return RenderWaveformFromOggBytes(oggBytes, baseSettings, overlaySettings);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static WaveformRenderResult RenderWaveformFromWavBytes(byte[] wavBytes, WaveFormRendererSettings baseSettings, WaveFormRendererSettings overlaySettings)
        {
            using var baseMemoryStream = new MemoryStream(wavBytes, writable: false);
            using var baseWaveStream = new WaveFileReader(baseMemoryStream);
            var totalTime = baseWaveStream.TotalTime;

            // Use separate readers for base/overlay so both renders start from an identical decode origin.
            // Rewinding and reusing the same decoded stream can produce slight pixel drift on some codecs.
            var baseBitmap = RenderWaveformImage(baseWaveStream, baseSettings);

            using var overlayMemoryStream = new MemoryStream(wavBytes, writable: false);
            using var overlayWaveStream = new WaveFileReader(overlayMemoryStream);
            var overlayBitmap = RenderWaveformImage(overlayWaveStream, overlaySettings);

            var waveformVisualisation = WaveformVisualisation.Create(baseBitmap, overlayBitmap);
            var pixelWidth = baseBitmap.PixelWidth;

            return new WaveformRenderResult(waveformVisualisation, totalTime, pixelWidth);
        }

        private static WaveformRenderResult RenderWaveformFromOggBytes(byte[] oggBytes, WaveFormRendererSettings baseSettings, WaveFormRendererSettings overlaySettings)
        {
            using var baseMemoryStream = new MemoryStream(oggBytes, writable: false);
            using var baseWaveStream = new VorbisWaveReader(baseMemoryStream);
            var totalTime = baseWaveStream.TotalTime;

            // Use separate readers for base/overlay so both renders start from an identical decode origin.
            // Rewinding and reusing the same decoded stream can produce slight pixel drift on some codecs.
            var baseBitmap = RenderWaveformImage(baseWaveStream, baseSettings);

            using var overlayMemoryStream = new MemoryStream(oggBytes, writable: false);
            using var overlayWaveStream = new VorbisWaveReader(overlayMemoryStream);
            var overlayBitmap = RenderWaveformImage(overlayWaveStream, overlaySettings);

            var waveformVisualisation = WaveformVisualisation.Create(baseBitmap, overlayBitmap);
            var pixelWidth = baseBitmap.PixelWidth;

            return new WaveformRenderResult(waveformVisualisation, totalTime, pixelWidth);
        }

        private static BitmapImage RenderWaveformImage(WaveStream waveStream, WaveFormRendererSettings settings)
        {
            using var alignedWaveStream = new BlockAlignReductionStream(waveStream);
            var waveFormRenderer = new WaveFormRenderer();
            using var imageDrawing = waveFormRenderer.Render(alignedWaveStream, settings);
            return ToBitmapImage(imageDrawing);
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
