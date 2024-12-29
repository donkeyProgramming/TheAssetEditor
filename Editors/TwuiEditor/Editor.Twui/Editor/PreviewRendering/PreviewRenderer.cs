using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.TextureEditor.ViewModels;
using Editors.Twui.Editor.Datatypes;
using GameWorld.Core.Rendering;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Twui.Editor.PreviewRendering
{
    public partial class PreviewRenderer : ObservableObject
    {
        private readonly IScopedResourceLibrary _resourceLib;
        private readonly TextureToTextureRenderer _textureRenderer;
        private readonly IWpfGame _wpfGame;
        private readonly IPackFileService _packFileService;
        TwuiFile _currentFile;

        [ObservableProperty] System.Windows.Media.ImageSource _previewImage;

        public PreviewRenderer(IScopedResourceLibrary resourceLibrary, IWpfGame wpfGame, IPackFileService packFileService)
        {
            _resourceLib = resourceLibrary;
            _wpfGame = wpfGame;
            _packFileService = packFileService;
            _wpfGame.ForceEnsureCreated();

            _textureRenderer = new TextureToTextureRenderer(_wpfGame.GraphicsDevice, new Microsoft.Xna.Framework.Graphics.SpriteBatch(_wpfGame.GraphicsDevice), _resourceLib);
        }

        public void SetFile(TwuiFile file)
        {
            _currentFile = file;
            Refresh();
        }

        public void Refresh()
        {
            if (_currentFile == null)
                return;

            var width = 1600;
            var height = 900;
           

            var textures = _currentFile.Components
                .SelectMany(x=>x.ComponentImages)
                .Select(x=>x.ImagePath)
                .Distinct()
                .ToList();
     

            var device = _wpfGame.GraphicsDevice;
            var spriteBatch = new Microsoft.Xna.Framework.Graphics.SpriteBatch(device);
            var renderTarget = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);

            device.SetRenderTarget(renderTarget);
            device.Clear(Microsoft.Xna.Framework.Color.Transparent);
            device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default,RasterizerState.CullNone);

            var notFound = new List<string>();

            var componentList = _currentFile.Components.OrderByDescending(x=>x.Priority).ToList();
            foreach (var comp in _currentFile.Components)
            {
                foreach (var image in comp.ComponentImages)
                {
                    if (string.IsNullOrWhiteSpace(image.ImagePath))
                        continue;

                    var found = _packFileService.FindFile(image.ImagePath);
                    if (found == null)
                    {
                        notFound.Add(image.ImagePath);
                        continue;
                    }

                    var texture = _resourceLib.ForceLoadImage(image.ImagePath, out var imageInformation);
                    spriteBatch.Draw(texture, comp.Offset.GetAsVector2(), Microsoft.Xna.Framework.Color.White);

                }
            }


            device.SetRenderTarget(null);
         
            using var sourceBitmap = new System.Drawing.Bitmap(renderTarget.Width, renderTarget.Height);
            using var g = Graphics.FromImage(sourceBitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            DrawCheckerBoard(g, renderTarget.Width, renderTarget.Height);
            var bitmap = TextureBuilder.ConvertTextureToImage(renderTarget);
            g.DrawImage(bitmap, 0, 0);
            
            
            PreviewImage = TextureBuilder.BitmapToImageSource(sourceBitmap);

            renderTarget.Dispose();
            spriteBatch.Dispose();

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
