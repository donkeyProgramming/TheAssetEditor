using System.Drawing;

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
