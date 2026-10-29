using System.Drawing;
using System.IO;
using Editors.ImportExport.Misc;
using MeshImportExport;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng
{

    public interface IDdsToMaterialPngExporter
    {
        public string Export(string filePath, string outputPath, bool convertToBlenderFormat);
        public ExportSupportEnum CanExportFile(PackFile file);
    }

    public class DdsToMaterialPngExporter : IDdsToMaterialPngExporter
    {
        private readonly IPackFileService _pfs;
        private readonly IImageSaveHandler _imageSaveHandler;
        public DdsToMaterialPngExporter(IPackFileService packFileService, IImageSaveHandler imageSaveHandler)
        {
            _pfs = packFileService;
            _imageSaveHandler = imageSaveHandler;
        }

        public string Export(string filePath, string outputPath, bool convertToBlenderFormat)
        {
            var packFile = _pfs.FindFile(filePath);
            if (packFile == null)            
                return "";

            var fileName = Path.GetFileNameWithoutExtension(
                filePath.Replace('\\', Path.DirectorySeparatorChar));
            var outDirectory = Path.GetDirectoryName(outputPath) ?? string.Empty;
            var outFilePath = Path.Combine(outDirectory, fileName + ".png");

            var bytes = packFile.DataSource.ReadData();
            if (bytes == null || !bytes.Any())
                throw new Exception($"Could not read file data. bytes.Count = {bytes?.Length}");

            var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
            if (imgBytes == null || !imgBytes.Any())
                throw new Exception($"image data invalid/empty. imgBytes.Count = {imgBytes?.Length}");

            if (convertToBlenderFormat)
            {
                imgBytes = ConvertToBlenderFormat(imgBytes);
            }

            _imageSaveHandler.Save(imgBytes, outFilePath);

            return outFilePath;
        }

        public ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsMaterialFile(file.Name))
                return ExportSupportEnum.HighPriority;
            else if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        private static byte[] ConvertToBlenderFormat(byte[] imgBytes)
        {
            using var ms = new MemoryStream(imgBytes);

            using var image = Image.FromStream(ms);
            using var bitmap = new Bitmap(image);
            var pixels = BitmapPixelBuffer.ReadBgra(bitmap);
            for (var index = 0; index < pixels.Length; index += 4)
            {
                (pixels[index], pixels[index + 2]) = (pixels[index + 2], pixels[index]);
                pixels[index + 3] = 255;
            }

            using var converted = BitmapPixelBuffer.CreateBitmap(bitmap.Width, bitmap.Height, pixels);
            using var output = new MemoryStream();
            converted.Save(output, System.Drawing.Imaging.ImageFormat.Png);
            return output.ToArray();
        }
    }
}
