using System.Drawing;
using System.IO;
using System.Numerics;
using Editors.ImportExport.Misc;
using MeshImportExport;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters.DdsToNormalPng
{

    public interface IDdsToNormalPngExporter
    {
        public string Export(string filePath, string outputPath, bool convertToBlueNormalMap);
        public ExportSupportEnum CanExportFile(PackFile file);
    }

    public class DdsToNormalPngExporter : IDdsToNormalPngExporter
    {
        private readonly IPackFileService _pfs;
        private readonly IImageSaveHandler _imageSaveHandler;

        public DdsToNormalPngExporter(IPackFileService packFileService, IImageSaveHandler imageSaveHandler) 
        {
            _pfs = packFileService;
            _imageSaveHandler = imageSaveHandler;
        }

        public ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsMaterialFile(file.Name))
                return ExportSupportEnum.HighPriority;
            else if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        public string Export(string filePath, string outputPath, bool convertToBlueNormalMap)
        {
            var packFile = _pfs.FindFile(filePath);
            if (packFile == null)
                return "";

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var outDirectory = Path.GetDirectoryName(outputPath);
            var fullFilePath = outDirectory + "/" + fileName + ".png";

            var bytes = packFile.DataSource.ReadData();
            if (bytes == null || !bytes.Any())
                throw new Exception($"Could not read file data. bytes.Count = {bytes?.Length}");

            var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
            if (imgBytes == null || !imgBytes.Any())
                throw new Exception($"image data invalid/empty. imgBytes.Count = {imgBytes?.Length}");

            if (convertToBlueNormalMap)
                imgBytes = ConvertToBlueNormalMap(imgBytes, fullFilePath);

            _imageSaveHandler.Save(imgBytes, fullFilePath);
            return fullFilePath;
        }


        private byte[] ConvertToBlueNormalMap(byte[] imgBytes, string fileDirectory)
        {
            var inMs = new MemoryStream(imgBytes);
            using Image inImg = Image.FromStream(inMs);

            using Bitmap bitmap = new Bitmap(inImg);
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        // get pixel from orange map
                        var orangeMapRawPixel = bitmap.GetPixel(x, y);

                        // convert bytes to float to interval [0; 1]
                        Vector4 orangeMapVector = new Vector4()
                        {
                            X = (float)orangeMapRawPixel.R / 255.0f,
                            Y = (float)orangeMapRawPixel.G / 255.0f,
                            Z = (float)orangeMapRawPixel.B / 255.0f,
                            W = (float)orangeMapRawPixel.A / 255.0f,
                        };

                        // fill blue map pixels
                        Vector3 blueMapPixel = new Vector3()
                        {
                            X = orangeMapVector.X * orangeMapVector.W,
                            Y = orangeMapVector.Y,
                            Z = 0
                        };

                        // scale bluemap into interval [-1; 1]
                        blueMapPixel *= 2.0f;
                        blueMapPixel -= new Vector3(1, 1, 1);


                        // calculte z, using an orthogonal projection
                        blueMapPixel.Z = (float)Math.Sqrt(1.0f - blueMapPixel.X * blueMapPixel.X - blueMapPixel.Y * blueMapPixel.Y);
                                           

                        // convert the float values back to bytes, interval [0; 255]
                        var newColor = Color.FromArgb(
                            255,
                            (byte)((blueMapPixel.X + 1.0f) * 0.5f * 255.0f),
                            (byte)((blueMapPixel.Y + 1.0f) * 0.5f * 255.0f),
                            (byte)((blueMapPixel.Z + 1.0f) * 0.5f * 255.0f)                            
                            );                                         
                        
                        bitmap.SetPixel(x, y, newColor);
                    }
                }

                // get raw PNG bytes
                using var b = new MemoryStream();
                bitmap.Save(b, System.Drawing.Imaging.ImageFormat.Png);

                return b.ToArray();
            }
        }

    }
}
