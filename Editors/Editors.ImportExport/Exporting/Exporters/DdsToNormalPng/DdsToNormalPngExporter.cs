using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System.Drawing;
using System.IO;
using MeshImportExport;
using Editors.ImportExport;
using System.Windows;
using System.Numerics;


namespace Editors.ImportExport.Exporting.Exporters.DdsToNormalPng
{
    public class DdsToNormalPngExporter
    {
        private readonly PackFileService _pfs;
        private readonly IImageSaveHandler _imageSaveHandler;
        public DdsToNormalPngExporter(PackFileService packFileService, IImageSaveHandler imageSaveHandler) 
        {
            _pfs = packFileService;
            _imageSaveHandler = imageSaveHandler;
        }

        public string Export(string filePath, string outputPath, bool convert)
        {
            try
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
                return fileDirectory;
            } catch(NullReferenceException exception)
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
                _imageSaveHandler.Save(bitmap, fileDirectory);
            }
        }

        public void DoNotConvertExport(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            var ms = new MemoryStream(imgBytes);
            using Image img = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(img);
            _imageSaveHandler.Save(bitmap, fileDirectory);
        }
    }
}
