using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace View3D.Utility
{
    public class TextureToTextureRenderer
    {
        GraphicsDevice _device;
        SpriteBatch _spriteBatch;
        ResourceLibary _resourceLibary;

        public class DrawSettings
        {
            public bool OnlyRed { get; set; } = false;
            public bool OnlyGreen { get; set; } = false;
            public bool OnlyBlue { get; set; } = false;
            public bool OnlyAlpha { get; set; } = false;
        }

        public TextureToTextureRenderer(GraphicsDevice device, SpriteBatch spriteBatch, ResourceLibary resourceLibary)
        {
            _device = device;
            _spriteBatch = spriteBatch;
            _resourceLibary = resourceLibary;
        }

        public Texture2D RenderToTexture(Texture2D texture, int width, int height, DrawSettings settings, string outputPath = null)
        {
            RenderTarget2D renderTarget = new RenderTarget2D(_device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            DrawTextureToTarget(renderTarget, texture, settings);
            if (outputPath != null)
                SaveTexture(renderTarget, outputPath);
            return renderTarget;
        }

        void DrawTextureToTarget(RenderTarget2D renderTarget, Texture2D texture, DrawSettings setings)
        {
            _device.SetRenderTarget(renderTarget);
            _device.Clear(Microsoft.Xna.Framework.Color.Transparent);
            _device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            var previewShader = _resourceLibary.GetEffect(ShaderTypes.TexturePreview);

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.Default,
                    RasterizerState.CullNone, previewShader);

            previewShader.Parameters["OnlyRed"].SetValue(setings.OnlyRed);
            previewShader.Parameters["OnlyGreen"].SetValue(setings.OnlyGreen);
            previewShader.Parameters["OnlyBlue"].SetValue(setings.OnlyBlue);
            previewShader.Parameters["ApplyOnlyAlpha"].SetValue(setings.OnlyAlpha);

            _spriteBatch.Draw(texture, Vector2.Zero, Microsoft.Xna.Framework.Color.Red);
            _spriteBatch.End();

            _device.SetRenderTarget(null);
        }

        public void SaveTexture(Texture2D texture, string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            }
        }
    }
}
