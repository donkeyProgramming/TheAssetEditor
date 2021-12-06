using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel.Types;
using CommonControls.Services;
using Microsoft.Xna.Framework.Graphics;
using Pfim;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TextureEditor.Views;
using View3D.Scene;
using View3D.Utility;

namespace TextureEditor.ViewModels
{
    public class TexturePreviewController : IDisposable
    {
        public static void CreateVindow(string imagePath, PackFileService packFileService)
        {
            TexturePreviewViewModel viewModel = new TexturePreviewViewModel()
            {
                Format = "DDS0",
                Name = imagePath,
                //Height = 512,
                //Width = 1024,
            };

            using (var controller = new TexturePreviewController(imagePath, viewModel, packFileService))
            {
                var containingWindow = new Window();
                containingWindow.Title = "Texture Preview Window";
                containingWindow.Content = new TexturePreviewView() { DataContext = viewModel };
                containingWindow.ShowDialog();
            }
        }

        PackFileService _packFileService;
        ResourceLibary _resourceLib;
        TextureToTextureRenderer _textureRenderer;
        string _imagePath;
        TexturePreviewViewModel _viewModel;

        SceneContainer _scene;
        public TexturePreviewController(string imagePath, TexturePreviewViewModel viewModel, PackFileService packFileService)
        {
            _imagePath = imagePath;
            _viewModel = viewModel;
            _packFileService = packFileService;

            _scene = new SceneContainer();
            _scene.Components.Add(new ResourceLibary(_scene, packFileService ));
            _scene.ForceCreate();
  

            _resourceLib = _scene.GetComponent<ResourceLibary>();
            _textureRenderer = new TextureToTextureRenderer(_scene.GraphicsDevice, new SpriteBatch(_scene.GraphicsDevice), _resourceLib);
            CreateImage();
        }


        void CreateImage()
        {
            var texture = _resourceLib.LoadTexture(_imagePath);

            var settings = new TextureToTextureRenderer.DrawSettings[5];
            settings[0] = new TextureToTextureRenderer.DrawSettings();
            settings[1] = new TextureToTextureRenderer.DrawSettings() { OnlyRed = true };
            settings[2] = new TextureToTextureRenderer.DrawSettings() { OnlyBlue = true };
            settings[3] = new TextureToTextureRenderer.DrawSettings() { OnlyGreen = true };
            settings[4] = new TextureToTextureRenderer.DrawSettings() { OnlyAlpha = true };

            for (int i = 0; i < settings.Count(); i++)
            {
                using (var renderedTexture = _textureRenderer.RenderToTexture(texture, texture.Width, texture.Height, settings[i]))
                {
                    using (var sourceBitmap = new Bitmap(texture.Width, texture.Height))
                    {
                        using (var g = Graphics.FromImage(sourceBitmap))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic; 
                            drawBoard(g, texture.Width, texture.Height);
                            var bitmap = Texture2Image(renderedTexture);
                            g.DrawImage(bitmap, 0, 0);
                            var wpfImg = BitmapToImageSource(sourceBitmap);
                            _viewModel.PreviewImage[i] = wpfImg;
                        }
                    }
                }
            }

            _viewModel.FormatRgbaCheckbox = true;
        }

        private void drawBoard(Graphics g, int width, int height)
        {
            var size = 50;
            var countX = (width / size) + 1;
            var countY = (height / size) + 1;
            using (SolidBrush blackBrush = new SolidBrush(System.Drawing.Color.DarkGray))
            using (SolidBrush whiteBrush = new SolidBrush(System.Drawing.Color.LightGray))
            {
                for (int i = 0; i < countX; i++)
                {
                    for (int j = 0; j < countY; j++)
                    {
                        if ((j % 2 == 0 && i % 2 == 0) || (j % 2 != 0 && i % 2 != 0))
                            g.FillRectangle(blackBrush, i * size, j * size, size, size);
                        else if ((j % 2 == 0 && i % 2 != 0) || (j % 2 != 0 && i % 2 == 0))
                            g.FillRectangle(whiteBrush, i * size, j * size, size, size);
                    }
                }
            }
        }


        public Image Texture2Image(Texture2D texture)
        {
            Image img;
            using (MemoryStream MS = new MemoryStream())
            {
                texture.SaveAsPng(MS, texture.Width, texture.Height);
                //Go To the  beginning of the stream.
                MS.Seek(0, SeekOrigin.Begin);
                //Create the image based on the stream.
                img = Bitmap.FromStream(MS);
            }
            return img;
        }


        IImage LoadTextureAsTexture2d(string fileName)
        {
            var file = _packFileService.FindFile(fileName);
            if (file == null)
                return null;

            var content = (file ).DataSource.ReadData();
            using (MemoryStream stream = new MemoryStream(content))
            {
                var image = Pfim.Dds.Create(stream, new Pfim.PfimConfig(32768, Pfim.TargetFormat.Native, true));
                if (image as Pfim.Dxt1Dds != null)
                {

                }
                else if (image as Pfim.Dxt5Dds != null)
                {

                }
                else
                {
                    throw new Exception("Unknow texture format: " + image.ToString());
                }
                return image;
            }
        }

        void DisplayTexture(TexureType type, string path)
        {
            try
            {

                var img = LoadTextureAsTexture2d(path);
                var wpfImg = ImageToBitmap(img);
                var ff = BitmapToImageSource(wpfImg);
                TexturePreviewView view = new TexturePreviewView();
                var dataContext = new TexturePreviewViewModel()
                {
                    Format = "DDS0",
                    Name = "MyTestImage.png",
                    Height = 512,
                    Width = 1024
                };
                view.DataContext = dataContext;
                var w = new Window();

                w.Title = "Texture Preview Window";

                /*var bitmap = new Bitmap(dataContext.Width, dataContext.Height);
                using (Graphics gfx = Graphics.FromImage(bitmap))
                using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0)))
                {
                    gfx.FillRectangle(brush, 0, 0, dataContext.Width, dataContext.Height);
                }*/
                dataContext.Image = ff;
                w.Content = view;
                w.ShowDialog();
            }
            catch (Exception e)
            {

            }
           
        }

        private static ImageSource WpfImage(IImage image)
        {
            var pinnedArray = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            var addr = pinnedArray.AddrOfPinnedObject();
            ImageSource bsource = WriteableBitmap.Create(image.Width, image.Height, 96.0, 96.0,
                PixelFormat(image), null, addr, image.DataLen, image.Stride);


            GCHandle handle = pinnedArray;
            return bsource;/*
            return new System.Windows.Controls.Image
            {
                Source = bsource,
                Width = image.Width,
                Height = image.Height,
                MaxHeight = image.Height,
                MaxWidth = image.Width,
                Margin = new Thickness(4)
            };*/
            /*
            var mip = image.MipMaps.First();
            {
                var mipAddr = addr + mip.DataOffset;
                var mipSource = BitmapSource.Create(mip.Width, mip.Height, 96.0, 96.0,
                    PixelFormat(image), null, mipAddr, mip.DataLen, mip.Stride);
                return new System.Windows.Controls.Image
                {
                    Source = mipSource,
                    Width = mip.Width,
                    Height = mip.Height,
                    MaxHeight = mip.Height,
                    MaxWidth = mip.Width,
                    Margin = new Thickness(4)
                };
            }*/
        }

        private static PixelFormat PixelFormat(IImage image)
        {
            switch (image.Format)
            {
                case ImageFormat.Rgb24:
                    return PixelFormats.Bgr24;
                case ImageFormat.Rgba32:
                    return PixelFormats.Bgr32;
                case ImageFormat.Rgb8:
                    return PixelFormats.Gray8;
                case ImageFormat.R5g5b5a1:
                case ImageFormat.R5g5b5:
                    return PixelFormats.Bgr555;
                case ImageFormat.R5g6b5:
                    return PixelFormats.Bgr565;
                default:
                    throw new Exception($"Unable to convert {image.Format} to WPF PixelFormat");
            }
        }

        Bitmap ImageToBitmap(IImage image)
        {
            //var image = Pfim.FromFile(dialog.FileName);

            System.Drawing.Imaging.PixelFormat format;
            switch (image.Format)
            {
                case ImageFormat.Rgb24:
                    format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
                    break;

                case ImageFormat.Rgba32:
                    format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                    break;

                case ImageFormat.R5g5b5:
                    format = System.Drawing.Imaging.PixelFormat.Format16bppRgb555;
                    break;

                case ImageFormat.R5g6b5:
                    format = System.Drawing.Imaging.PixelFormat.Format16bppRgb565;
                    break;

                case ImageFormat.R5g5b5a1:
                    format = System.Drawing.Imaging.PixelFormat.Format16bppArgb1555;
                    break;

                case ImageFormat.Rgb8:
                    format = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
                    break;

                default:
                    throw new Exception("unkownformat");
            }

            // Pin image data as the picture box can outlive the Pfim Image
            // object, which, unless pinned, will garbage collect the data
            // array causing image corruption.
            var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
            var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, ptr);


            for (int y = 0; y < bitmap.Height; y++)
            {

                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    var alpha = pixel.A;
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(255, alpha, alpha, alpha));
                }
            }

            return bitmap;
        }

        BitmapImage BitmapToImageSource(Image bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public void Dispose()
        {
            _textureRenderer.Dispose();
            _scene.Dispose();
        }
    }
}
