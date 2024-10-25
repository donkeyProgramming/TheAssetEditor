using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editors.ImportExport
{
    public interface IImageSaveHandler
    {
        void Save(Bitmap image, string systemFilePath);
    }

    public class SystemImageSaveHandler : IImageSaveHandler
    {
        public void Save(Bitmap image, string systemFilePath)
        {
            image.Save(systemFilePath, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
