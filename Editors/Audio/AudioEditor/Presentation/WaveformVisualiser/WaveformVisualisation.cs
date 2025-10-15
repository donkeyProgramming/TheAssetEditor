using System.Windows.Media.Imaging;

namespace Editors.Audio.AudioEditor.Presentation.WaveformVisualiser
{
    public class WaveformVisualisation
    {
        public required BitmapImage BaseImage { get; init; }
        public required BitmapImage OverlayImage { get; init; }
        public required int PixelWidth { get; init; }
        public required int PixelHeight { get; init; }

        public static WaveformVisualisation Create(BitmapImage baseImage, BitmapImage overlayImage)
        {
            return new WaveformVisualisation
            { 
                BaseImage = baseImage, 
                OverlayImage = overlayImage, 
                PixelWidth = baseImage.PixelWidth, 
                PixelHeight = baseImage.PixelHeight
            };
        }
    }
}
