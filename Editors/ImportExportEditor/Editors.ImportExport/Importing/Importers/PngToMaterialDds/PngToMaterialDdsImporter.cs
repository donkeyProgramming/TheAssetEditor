using System.Drawing;
using System.Windows;
using Shared.Core.PackFiles;

namespace Editors.ImportExport.Importing.Importers.PngToMaterialDds
{
    internal class PngToMaterialDdsImporter
    {
        private readonly IPackFileService _pfs;
        private readonly IImageSaveHandler _imageSaveHandler;

        public PngToMaterialDdsImporter(IPackFileService pfs, IImageSaveHandler imageSaveHandler)
        {
            _pfs = pfs;
            _imageSaveHandler = imageSaveHandler;
        }

        public void Import(String importFilePath)
        {
            try
            {
                Bitmap image = (Bitmap)Image.FromFile(importFilePath);
                ConvertToTotalWarFormat(image, "C:/franz/", importFilePath);
            }
            catch (NullReferenceException exception)
            {
                MessageBox.Show(exception.Message + importFilePath);
            }
        }

        internal void ConvertToTotalWarFormat(Bitmap image, string outputPath, string fileDirectory)
        {
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
                        var A = pixel.A;
                        Color newColor = Color.FromArgb(A, R, G, B);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }
                throw new NotImplementedException();
                //_imageSaveHandler.Save(bitmap, fileDirectory);
            }
        }
    }
}
