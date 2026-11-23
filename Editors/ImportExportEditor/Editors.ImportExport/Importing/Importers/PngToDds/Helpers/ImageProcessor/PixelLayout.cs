using DirectXTexNet;

namespace Editors.ImportExport.Importing.Importers.PngToDds.Helpers.ImageProcessor
{
    /// <summary>
    /// Byte offsets of the red and blue channels in an uncompressed 8-bit image. WIC loads
    /// PNGs with alpha as BGRA but RGB-only PNGs (as Blender writes normal and material maps) as RGBA.
    /// </summary>
    public readonly record struct PixelLayout(int RedOffset, int BlueOffset)
    {
        public static PixelLayout Get(ScratchImage scratchImage)
        {
            var format = scratchImage.GetMetadata().Format;
            return format switch
            {
                DXGI_FORMAT.B8G8R8A8_UNORM or DXGI_FORMAT.B8G8R8A8_UNORM_SRGB => new PixelLayout(2, 0),
                DXGI_FORMAT.R8G8B8A8_UNORM or DXGI_FORMAT.R8G8B8A8_UNORM_SRGB => new PixelLayout(0, 2),
                _ => throw new Exception($"Error: image format is {format}, should be uncompressed 8-bit RGBA or BGRA"),
            };
        }
    }
}
