using GameWorld.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Events;
using Shared.Core.Services;

//https://github.com/Apostolique/Apos.Gui/blob/main/Source/Dock.cs

namespace Editors.Twui.Editor.Rendering
{
    public class TwuiRenderComponent : BaseComponent, IDisposable
    {
        private readonly IWpfGame _wpfGame;
        private readonly TwuiPreviewBuilder _twuiPreviewBuilder;

        Texture2D _twuiPreview;
        SpriteBatch _spriteBatch;
        Texture2D _whiteSquareTexture;

        public TwuiRenderComponent(IWpfGame wpfGame, TwuiPreviewBuilder twuiPreviewBuilder, IEventHub eventHub)
        {
            _wpfGame = wpfGame;
            _twuiPreviewBuilder = twuiPreviewBuilder;
        }

        TwuiContext? _temp_twuiFile;
        public void SetFile(TwuiContext twuiContext)
        {
            _temp_twuiFile = twuiContext;
            _twuiPreview = _twuiPreviewBuilder.UpdateTexture(_temp_twuiFile);
        }

        public override void Initialize()
        {
            _spriteBatch = new SpriteBatch(_wpfGame.GraphicsDevice);
            _whiteSquareTexture = new Texture2D(_wpfGame.GraphicsDevice, 1, 1);
            _whiteSquareTexture.SetData([Color.White]);

            _twuiPreviewBuilder.Initialize();
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();
             DrawCheckerboardBackground(50);
            _spriteBatch.End();

            if (_twuiPreview == null )
                return;

            var currentTarget = _wpfGame.GraphicsDevice.GetRenderTargets().FirstOrDefault();
            SetFile(_temp_twuiFile);    // Hack to trigger instant rendering for now
            _wpfGame.GraphicsDevice.SetRenderTarget(currentTarget.RenderTarget as RenderTarget2D);

            _spriteBatch.Begin();
            var destinationRectangle = ComputeAspectCorrectDrawingRectangle();
            _spriteBatch.Draw(_twuiPreview, destinationRectangle, Color.White);
            _spriteBatch.End();
        }

        Rectangle ComputeAspectCorrectDrawingRectangle()
        {
            if (_twuiPreview == null)
                return Rectangle.Empty;

            var targetAspectRatio = (float)_twuiPreview.Width / _twuiPreview.Height;
            var screenAspectRatio = (float)_wpfGame.GraphicsDevice.Viewport.Width / _wpfGame.GraphicsDevice.Viewport.Height;

            Rectangle destinationRectangle;
            if (screenAspectRatio >= targetAspectRatio)
            {
                var width = (int)(_wpfGame.GraphicsDevice.Viewport.Height * targetAspectRatio);
                var x = (_wpfGame.GraphicsDevice.Viewport.Width - width) / 2;
                destinationRectangle = new Rectangle(x, 0, width, _wpfGame.GraphicsDevice.Viewport.Height);
            }
            else
            {
                var height = (int)(_wpfGame.GraphicsDevice.Viewport.Width / targetAspectRatio);
                var y = (_wpfGame.GraphicsDevice.Viewport.Height - height) / 2;
                destinationRectangle = new Rectangle(0, y, _wpfGame.GraphicsDevice.Viewport.Width, height);
            }

            return destinationRectangle;
        }

        void DrawCheckerboardBackground(int squareSize)
        {
            var boardWidth = 1 + _wpfGame.GraphicsDevice.Viewport.Width / squareSize;
            var boardHeight = 1 + _wpfGame.GraphicsDevice.Viewport.Height / squareSize;

            for (var row = 0; row < boardHeight; row++)
            {
                for (var col = 0; col < boardWidth; col++)
                {
                    var squareColor = (row + col) % 2 == 0 ? Color.Gray : Color.White;
                    var squareRectangle = new Rectangle(col * squareSize, row * squareSize, squareSize, squareSize);
                    _spriteBatch.Draw(_whiteSquareTexture, squareRectangle, squareColor);
                }
            }
        }

        public void Dispose()
        {
            _whiteSquareTexture?.Dispose();
            _spriteBatch?.Dispose();
        }


    }
}
