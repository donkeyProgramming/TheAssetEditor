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
            PixelLayout.Get(scratchImage);

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
