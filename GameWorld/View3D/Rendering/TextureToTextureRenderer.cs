using System;
using System.IO;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace GameWorld.Core.Rendering
{
    public class TextureToTextureRenderer : IDisposable
    {
        public readonly GraphicsDevice _device;
        public readonly SpriteBatch _spriteBatch;
        public readonly IScopedResourceLibrary _resourceLibary;

        public class DrawSettings
        {
            public bool OnlyRed { get; set; } = false;
            public bool OnlyGreen { get; set; } = false;
            public bool OnlyBlue { get; set; } = false;
            public bool OnlyAlpha { get; set; } = false;
        }

        public TextureToTextureRenderer(GraphicsDevice device, SpriteBatch spriteBatch, IScopedResourceLibrary resourceLibary)
        {
            _device = device;
            _spriteBatch = spriteBatch;
            _resourceLibary = resourceLibary;
        }

        public Texture2D RenderToTexture(Texture2D texture, int width, int height, DrawSettings settings, string outputPath = null)
        {
            var renderTarget = new RenderTarget2D(_device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            DrawTextureToTarget(renderTarget, texture, settings);
            if (outputPath != null)
                SaveTexture(renderTarget, outputPath);
            return renderTarget;
        }

        void DrawTextureToTarget(RenderTarget2D renderTarget, Texture2D texture, DrawSettings setings)
        {
            _device.SetRenderTarget(renderTarget);
            _device.Clear(Color.Transparent);
            _device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            var previewShader = _resourceLibary.GetStaticEffect(ShaderTypes.TexturePreview);

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.Default,
                    RasterizerState.CullNone, previewShader);

            previewShader.Parameters["OnlyRed"].SetValue(setings.OnlyRed);
            previewShader.Parameters["OnlyGreen"].SetValue(setings.OnlyGreen);
            previewShader.Parameters["OnlyBlue"].SetValue(setings.OnlyBlue);
            previewShader.Parameters["ApplyOnlyAlpha"].SetValue(setings.OnlyAlpha);

            _spriteBatch.Draw(texture, Vector2.Zero, Color.Red);
            _spriteBatch.End();

            _device.SetRenderTarget(null);
        }

        public void SaveTexture(Texture2D texture, string path)
        {
            using var stream = new FileStream(path, FileMode.OpenOrCreate);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
        }

        public void Dispose()
        {
            _spriteBatch.Dispose();
        }
    }
}
