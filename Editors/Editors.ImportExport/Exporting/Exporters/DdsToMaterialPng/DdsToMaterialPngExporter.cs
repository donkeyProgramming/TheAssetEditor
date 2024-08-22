using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System.Drawing;
using System.IO;
using MeshImportExport;
using System.Windows;

namespace Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng
{
    public class DdsToMaterialPngExporter
    {
        private readonly PackFileService pfs;
        private readonly IImageSaveHandler _imageSaveHandler;
        public DdsToMaterialPngExporter(PackFileService packFileService, IImageSaveHandler imageSaveHandler)
        {
            pfs = packFileService;
            _imageSaveHandler = imageSaveHandler;
        }
        public string Export(string filePath, string outputPath, bool convertToBlenderFormat)
        {
            try
            {
                var packFile = pfs.FindFile(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileDirectory = outputPath + "/" + fileName + ".png";
                var bytes = packFile.DataSource.ReadData();
                var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
                if (convertToBlenderFormat)
                {
                    ConvertToBlenderFormat(imgBytes, outputPath, fileDirectory);
                }
                else
                {
                    DoNotConvertExport(imgBytes, outputPath, fileDirectory);
                }
                return fileDirectory;
            }
            catch(NullReferenceException exception)
            {
                MessageBox.Show(exception.Message + filePath);
                return "";
            }
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsMaterialFile(file.Name))
                return ExportSupportEnum.HighPriority;
            else if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        private void ConvertToBlenderFormat(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            var ms = new MemoryStream(imgBytes);

            using Image image = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(image);
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        int R = pixel.R;
                        int G = pixel.G;
                        int B = pixel.B;
                        Color newColor = Color.FromArgb(255, B, G, R);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }
                _imageSaveHandler.Save(bitmap, fileDirectory);
            }
        }

        private void DoNotConvertExport(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            var ms = new MemoryStream(imgBytes);
            using Image img = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(img);
            _imageSaveHandler.Save(bitmap, fileDirectory);
        }
    }
}
