using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DirectXTexNet;
using Editors.ImportExport.Common.Interfaces;

namespace Editors.ImportExport.Importing.Importers.PngToDds.Helpers.ImageProcessor
{
    public class DefaultImageProcessor : IImageProcessor
    {
        public ScratchImage Transform(ScratchImage scratchImage)
        {          
            if (!(scratchImage.GetMetadata().Format == DXGI_FORMAT.B8G8R8A8_UNORM || scratchImage.GetMetadata().Format == DXGI_FORMAT.B8G8R8A8_UNORM_SRGB))
            {
                throw new Exception($"Error: image format is {scratchImage.GetMetadata().Format}  should be uncompressed RGBA8 (BC_B8G8R8A8_UNORM)");
            }

            var outScratchImage = scratchImage.CreateImageCopy(0, false, CP_FLAGS.NONE);

            var srcImage = scratchImage.GetImage(0, 0, 0);
            var destImage = outScratchImage.GetImage(0, 0, 0);
            

            // copy the pixel pointer's content to a byte array
            byte[] rgbaBytes = new byte[srcImage.SlicePitch];
            Marshal.Copy(srcImage.Pixels, rgbaBytes, 0, (int)srcImage.SlicePitch);
            
            return outScratchImage;
        }
    }

}
