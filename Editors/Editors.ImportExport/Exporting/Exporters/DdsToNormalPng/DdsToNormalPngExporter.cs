using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System.Drawing;
using System.IO;
using MeshImportExport;


namespace Editors.ImportExport.Exporting.Exporters.DdsToNormalPng
{
    public class DdsToNormalPngExporter
    {
        private readonly PackFileService _pfs;
        public DdsToNormalPngExporter(PackFileService packFileService) 
        {
            _pfs = packFileService;
        }

        public void Export(string filePath, string outputPath, bool convert)
        {
            var packFile = _pfs.FindFile(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileDirectory = outputPath + "/" + fileName + ".png";
            var bytes = packFile.DataSource.ReadData();
            var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
            if (convert)
            {
                ConvertToBlueNormalMap(imgBytes, outputPath, fileDirectory);
            }
            else
            {
                DoNotConvertExport(imgBytes, outputPath, fileDirectory);
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

        private void ConvertToBlueNormalMap(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            var ms = new MemoryStream(imgBytes);

            using Image img = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(img);
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        var G = pixel.G;
                        var A = pixel.A;
                        var newColor = Color.FromArgb(255, A, G, 255);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }
                bitmap.Save(fileDirectory, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        public void DoNotConvertExport(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            var ms = new MemoryStream(imgBytes);
            using Image img = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(img);
            bitmap.Save(fileDirectory, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
