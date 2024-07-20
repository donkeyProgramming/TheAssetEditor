using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using SharpDX.MediaFoundation;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Editors.ImportExport.Exporting.Exporters.DdsToNormalPng
{
    public class DdsToNormalPngExporter
    {

        public void Export(string outputPath, string fileName)
        {
            using (Image image = Image.FromFile("C:/franz/" + fileName))
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

                        // Apply the conversion formulas
                        //this goes from blue to orange
                        //int R1 = 255;
                        //int B1 = 0;
                        //int G1 = G;
                        //int A1 = R;

                        //this goes from orange to blue
                        int R1 = A;
                        int B1 = 255;
                        int G1 = G;
                        int A1 = 255;

                        // Set the new pixel color
                        Color newColor = Color.FromArgb(A1, R1, G1, B1);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }

                // Save the output image
                bitmap.Save("C:/franz/test.png", System.Drawing.Imaging.ImageFormat.Png);
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
