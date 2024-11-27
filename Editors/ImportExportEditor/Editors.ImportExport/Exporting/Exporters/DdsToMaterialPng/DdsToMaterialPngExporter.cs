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

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var outDirectory = Path.GetDirectoryName(outputPath);
            var outFilePath = outDirectory + "/" + fileName + ".png";

            var bytes = packFile.DataSource.ReadData();
            if (bytes == null || !bytes.Any())
                throw new Exception($"Could not read file data. bytes.Count = {bytes?.Length}");

            var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
            if (imgBytes == null || !imgBytes.Any())
                throw new Exception($"image data invalid/empty. imgBytes.Count = {imgBytes?.Length}");

            if (convertToBlenderFormat)
            {
                imgBytes = ConvertToBlenderFormat(imgBytes, outputPath, outFilePath);
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

        byte[] ConvertToBlenderFormat(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            var ms = new MemoryStream(imgBytes);

            using var image = Image.FromStream(ms);
            using var bitmap = new Bitmap(image);
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        var R = pixel.R;
                        var G = pixel.G;
                        var B = pixel.B;
                        var newColor = Color.FromArgb(255, B, G, R);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }

                // get raw PNG bytes
                using var b = new MemoryStream();
                bitmap.Save(b, System.Drawing.Imaging.ImageFormat.Png);

                return b.ToArray();
            }
        }

        void DoNotConvertExport(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            _imageSaveHandler.Save(imgBytes, fileDirectory);
        }
    }
}
