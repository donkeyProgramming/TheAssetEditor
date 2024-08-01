using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using SharpDX.MediaFoundation;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using Shared.Core.Events;
using System.Windows;
using MeshImportExport;

namespace Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng
{
    public class DdsToMaterialPngExporter
    {
        private readonly PackFileService pfs;
        public DdsToMaterialPngExporter(PackFileService packFileService)
        {
            pfs = packFileService;
        }
        public void Export(string path, string outputPath, bool convertToBlenderFormat)
        {
            //nothing yet, need to know the difference between the two types of material images

            var file = pfs.FindFile(path);
            var bytes = file.DataSource.ReadData();
            var bmp = TextureHelper.ConvertDdsToPng(bytes);

            var ms = new MemoryStream(bmp);

            using (Image image = Image.FromStream(ms))
            using (Bitmap bitmap = new Bitmap(image))
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);

                        int R = pixel.R;
                        int G = pixel.G;
                        int B = pixel.B;
                        int A = pixel.A;

                        //grab the red channel for metalness
                        int R1 = B;
                        int B1 = R;
                        int G1 = G;
                        int A1 = 255;

                        // Set the new pixel color
                        Color newColor = Color.FromArgb(A1, R1, G1, B1);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }
                var fileName = Path.GetFileNameWithoutExtension(path);

                bitmap.Save(outputPath + "/" + fileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
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
    }
}
