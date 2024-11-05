using System.Drawing;
using System.IO;

namespace Editors.ImportExport
{
    public interface IImageSaveHandler
    {
        void Save(byte[] pngData, string systemFilePath);
    }

    public class SystemImageSaveHandler : IImageSaveHandler
    {
        public void Save(byte[] pngData, string systemFilePath)
        {
            var ms = new MemoryStream(pngData);
            using var img = Image.FromStream(ms);
            using var bitmap = new Bitmap(img);
            bitmap.Save(systemFilePath, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
