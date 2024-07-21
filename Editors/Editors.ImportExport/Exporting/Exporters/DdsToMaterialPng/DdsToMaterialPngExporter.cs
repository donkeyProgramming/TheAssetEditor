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

namespace Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng
{
    public class DdsToMaterialPngExporter
    {
        //tally is i=3 and then i=i+3
        public void Export(string outputPath, bool convertToBlenderFormat, string fileName, int tally)
        {
            //nothing yet, need to know the difference between the two types of material images

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

                        //grab the red channel for metalness
                        int R1 = R;
                        int B1 = 0;
                        int G1 = 0;
                        int A1 = 255;

                        // Set the new pixel color
                        Color newColor = Color.FromArgb(A1, R1, G1, B1);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }
                fileName = Path.GetFileNameWithoutExtension(fileName);
                // Save the output image
                bitmap.Save("C:/franz/" + fileName + "_metal_" + tally + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
            using (Image image = Image.FromFile("C:/franz/" + fileName + ".png"))
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

                        //grab the red channel for roughness
                        int R1 = 0;
                        int B1 = 0;
                        int G1 = G;
                        int A1 = 255;

                        // Set the new pixel color
                        Color newColor = Color.FromArgb(A1, R1, G1, B1);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }
                fileName = Path.GetFileNameWithoutExtension(fileName);
                // Save the output image
                bitmap.Save("C:/franz/" + fileName + "_roughness_" + tally + ".png", System.Drawing.Imaging.ImageFormat.Png);
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
