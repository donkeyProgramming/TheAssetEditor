using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DirectXTexNet;
using Editors.ImportExport.Common.Interfaces;

namespace Editors.ImportExport.Common.Interfaces
{
    public class BlenderToWH3MaterialMapProcessor : IImageProcessor
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
            Marshal.Copy(srcImage.Pixels, rgbaBytes, 0, (int)srcImage.SlicePitch);

            for (int index = 0; index < srcImage.SlicePitch; index += 4)
            {
                var r = rgbaBytes[index + 2];
                var g = rgbaBytes[index + 1];
                var b = rgbaBytes[index + 0];
                                
                rgbaBytes[index + 0] = r;
                rgbaBytes[index + 1] = g;
                rgbaBytes[index + 2] = b;
                rgbaBytes[index + 3] = 255;
                
            }

            Marshal.Copy(rgbaBytes, 0, srcImage.Pixels, (int)srcImage.SlicePitch);
            return copyScratchImage;
        }
    }
}
