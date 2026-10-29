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
            => File.WriteAllBytes(systemFilePath, pngData);
    }
}
