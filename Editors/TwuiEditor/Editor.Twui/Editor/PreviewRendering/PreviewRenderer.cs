﻿using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Twui.Editor.Datatypes;
using GameWorld.Core.Rendering;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;
using static GameWorld.Core.Rendering.TextureToTextureRenderer;

namespace Editors.Twui.Editor.PreviewRendering
{
    public partial class PreviewRenderer : ObservableObject
    {
        private readonly IScopedResourceLibrary _resourceLib;
        private readonly TextureToTextureRenderer _textureRenderer;
        private readonly IWpfGame _wpfGame;

        TwuiFile _currentFile;

        [ObservableProperty] ImageSource _previewImage;

        public PreviewRenderer(IScopedResourceLibrary resourceLibrary, IWpfGame wpfGame)
        {
            _resourceLib = resourceLibrary;
            _wpfGame = wpfGame;
            _wpfGame.ForceEnsureCreated();

            _textureRenderer = new TextureToTextureRenderer(_wpfGame.GraphicsDevice, new SpriteBatch(_wpfGame.GraphicsDevice), _resourceLib);
        }

        public void SetFile(TwuiFile file)
        {
            _currentFile = file;
            Refresh();
        }

        public void Refresh()
        {
            var texture = _resourceLib.ForceLoadImage(@"ui\skins\default\dlc25_book_of_grudges\panel_back.png", out var imageInformation);


            var settings = new DrawSettings();
            using var renderedTexture = _textureRenderer.RenderToTexture(texture, texture.Width, texture.Height, settings);
            using var sourceBitmap = new Bitmap(texture.Width, texture.Height);
            using var g = Graphics.FromImage(sourceBitmap);

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            DrawCheckerBoard(g, texture.Width, texture.Height);
            var bitmap = ConvertTextureToImage(renderedTexture);
            g.DrawImage(bitmap, 0, 0);
            PreviewImage = BitmapToImageSource(sourceBitmap);

        }

        public static Image ConvertTextureToImage(Texture2D texture)
        {
            using var stream = new MemoryStream();
            texture.SaveAsPng(stream, texture.Width, texture.Height);
            stream.Seek(0, SeekOrigin.Begin);
            return Image.FromStream(stream);
        }

        static BitmapImage BitmapToImageSource(Image bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }


        private static void DrawCheckerBoard(Graphics g, int width, int height)
        {
            var size = 50;
            var countX = width / size + 1;
            var countY = height / size + 1;
            using var blackBrush = new SolidBrush(System.Drawing.Color.DarkGray);
            using var whiteBrush = new SolidBrush(System.Drawing.Color.LightGray);

            for (var i = 0; i < countX; i++)
            {
                for (var j = 0; j < countY; j++)
                {
                    if (j % 2 == 0 && i % 2 == 0 || j % 2 != 0 && i % 2 != 0)
                        g.FillRectangle(blackBrush, i * size, j * size, size, size);
                    else if (j % 2 == 0 && i % 2 != 0 || j % 2 != 0 && i % 2 == 0)
                        g.FillRectangle(whiteBrush, i * size, j * size, size, size);
                }
            }
        }
    }
}