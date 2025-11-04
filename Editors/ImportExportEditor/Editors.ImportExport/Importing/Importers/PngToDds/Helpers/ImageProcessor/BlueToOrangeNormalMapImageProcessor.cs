using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DirectXTexNet;
using Editors.ImportExport.Common.Interfaces;
using Microsoft.Xna.Framework;

namespace Editors.ImportExport.Importing.Importers.PngToDds.Helpers.ImageProcessor
{
    public class BlueToOrangeNormalMapProcessor : IImageProcessor
    {
        public ScratchImage Transform(ScratchImage scratchImage)
        {
            if (!(scratchImage.GetMetadata().Format == DXGI_FORMAT.B8G8R8A8_UNORM || scratchImage.GetMetadata().Format == DXGI_FORMAT.B8G8R8A8_UNORM_SRGB))
            {
                throw new Exception($"Error: image format is {scratchImage.GetMetadata().Format}  should be uncompressed RGBA8 (BC_B8G8R8A8_UNORM)");
            }

            var copyScratchImage = scratchImage.CreateImageCopy(0, false, CP_FLAGS.NONE);
            var srcImage = copyScratchImage.GetImage(0, 0, 0);
            byte[] rgbaBytes = new byte[srcImage.SlicePitch];

            // copy data from image pixel pointer to byte array
            Marshal.Copy(srcImage.Pixels, rgbaBytes, 0, (int)srcImage.SlicePitch);

            for (int index = 0; index < srcImage.SlicePitch; index += 4)
            {
                var x_red = rgbaBytes[index + 2];
                var y_green = rgbaBytes[index + 1];

                rgbaBytes[index + 0] = 0;
                rgbaBytes[index + 1] = y_green;
                rgbaBytes[index + 2] = 255;
                rgbaBytes[index + 3] = x_red;

                rgbaBytes[index + 1] = ColorChannels.GammaComponent(rgbaBytes[index + 1], 1 / 2.2f);
            }

            // copy processed pixel back to the image pixel pointer½
            Marshal.Copy(rgbaBytes, 0, srcImage.Pixels, (int)srcImage.SlicePitch);
            return copyScratchImage;
        }
    }
}
