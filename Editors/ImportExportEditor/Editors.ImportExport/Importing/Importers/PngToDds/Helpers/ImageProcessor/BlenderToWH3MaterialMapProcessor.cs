using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DirectXTexNet;
using Editors.ImportExport.Common.Interfaces;
using Editors.ImportExport.Importing.Importers.PngToDds.Helpers.ImageProcessor;

namespace Editors.ImportExport.Common.Interfaces
{
    public class BlenderToWH3MaterialMapProcessor : IImageProcessor
    {
        public ScratchImage Transform(ScratchImage scratchImage)
        {
            var layout = PixelLayout.Get(scratchImage);
            
            var copyScratchImage = scratchImage.CreateImageCopy(0, false, CP_FLAGS.NONE);
            var srcImage = copyScratchImage.GetImage(0, 0, 0);
            byte[] rgbaBytes = new byte[srcImage.SlicePitch];
            Marshal.Copy(srcImage.Pixels, rgbaBytes, 0, (int)srcImage.SlicePitch);

            for (int index = 0; index < srcImage.SlicePitch; index += 4)
            {
                // Swap red and blue: Blender's metallic (blue) goes to the game's red channel.
                var r = rgbaBytes[index + layout.RedOffset];
                var b = rgbaBytes[index + layout.BlueOffset];

                rgbaBytes[index + layout.RedOffset] = b;
                rgbaBytes[index + layout.BlueOffset] = r;
                rgbaBytes[index + 3] = 255;
                
            }

            Marshal.Copy(rgbaBytes, 0, srcImage.Pixels, (int)srcImage.SlicePitch);
            return copyScratchImage;
        }
    }
}
