using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.PackFiles;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using System.Drawing;
using MeshImportExport;
using Editors.ImportExport;
using System.Windows;

namespace Editors.ImportExport.Importing.Importers.PngToNormalDds
{

    internal class PngToNormalDdsImporter
    {
        private readonly PackFileService _pfs;
        private readonly IImageSaveHandler _imageSaveHandler;
        public PngToNormalDdsImporter(PackFileService pfs, IImageSaveHandler imageSaveHandler)
        {
            _pfs = pfs;
            _imageSaveHandler = imageSaveHandler;
        }

        public void Import(String importFilePath)
        {
            try
            {
                //Image image = Image.FromFile(importFilePath);
                Bitmap image = (Bitmap)Image.FromFile(importFilePath);
                ConvertToOrangeNormalMap(image, "C:/franz/", importFilePath);
            }
            catch (NullReferenceException exception)
            {
                MessageBox.Show(exception.Message + importFilePath);
            }
        }

        private void ConvertToOrangeNormalMap(Bitmap image, string outputPath, string fileDirectory)
        {
            //var ms = new MemoryStream(imgBytes);

            //using Image img = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(image);
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        var G = pixel.G;
                        var A = pixel.A;
                        var R = pixel.R;
                        var B = pixel.B;
                        //var newColor = Color.FromArgb(255, A, G, 255);
                        var newColor = Color.FromArgb(A, R, G, B);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }
                _imageSaveHandler.Save(bitmap, fileDirectory);
            }
        }
    }
}
