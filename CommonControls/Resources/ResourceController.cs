using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace CommonControls.Resources
{
    public static class ResourceController
    {
        public static BitmapImage FolderIcon { get; private set; }
        public static BitmapImage CollectionIcon { get; private set; }
        public static BitmapImage FileIcon { get; private set; }

        public static void Load()
        {
            string iconsFolder = @"Resources\Icons\";
            FolderIcon = BitmapToImageSource(iconsFolder + "icons8-folder-48.png");
            FileIcon = BitmapToImageSource(iconsFolder + "icons8-file-48.png");
            CollectionIcon = BitmapToImageSource(iconsFolder + "icons8-collectibles-48.png");
        }

        static BitmapImage BitmapToImageSource(string path)
        {
            var bitmap = Bitmap.FromFile(path);
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

    }
}
