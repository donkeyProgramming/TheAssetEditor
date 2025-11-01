using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
using DrawingImage = System.Drawing.Image;

namespace Editors.Audio.AudioEditor.Presentation.WaveformVisualiser
{
    public class WaveformVisualiserHelpers
    {
        public static int DefaultPixelsPerPeak { get; set; } = 2;
        public static int DefaultSpacerPixels { get; set; } = 1;

        public static SoundCloudBlockWaveFormSettings CreateBaseWaveformSettings(int width)
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

        public static SoundCloudBlockWaveFormSettings CreateOverlayWaveformSettings(int width)
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

        public static BitmapImage ToBitmapImage(DrawingImage drawingImage)
        {
            using var memoryStream = new MemoryStream();
            drawingImage.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memoryStream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static WaveStream CreateWaveStream(Stream stream, string fileExtension)
        {
            return fileExtension switch
            {
                ".wav" => new WaveFileReader(stream),
                _ => throw new NotSupportedException("File type not supported."),
            };
        }
    }
}
