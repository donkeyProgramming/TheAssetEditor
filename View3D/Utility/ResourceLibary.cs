using Common;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using Pfim;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;
using View3D.Components;
using System.Windows.Media;
using FileTypes.DB;

namespace View3D.Utility
{
    public enum ShaderTypes
    {
        Line,
        Mesh,
        TexturePreview,
        Phazer,
        BasicEffect
    }

    public class ResourceLibary : BaseComponent, IDisposable
    {
        ILogger _logger = Logging.Create<ResourceLibary>();

        Dictionary<string, Texture2D> _textureMap = new Dictionary<string, Texture2D>();
        Dictionary<ShaderTypes, Effect> _shaders = new Dictionary<ShaderTypes, Effect>();

        public PackFileService Pfs { get; private set; }
        public ContentManager Content { get; set; }

        public TextureCube PbrDiffuse { get; private set; }
        public TextureCube PbrSpecular { get; private set; }
        public Texture2D PbrLut { get; private set; }


        public ResourceLibary(WpfGame game, PackFileService pf) : base(game)
        {
            Pfs = pf;
        }

        public override void Initialize()
        {
            Content = Game.Content;

            // Load default shaders
            LoadEffect("Shaders\\Phazer\\main", ShaderTypes.Phazer);
            LoadEffect("Shaders\\Geometry\\BasicShader", ShaderTypes.BasicEffect);
            LoadEffect("Shaders\\TexturePreview", ShaderTypes.TexturePreview);
            LoadEffect("Shaders\\LineShader", ShaderTypes.Line);

            PbrDiffuse = Content.Load<TextureCube>("textures\\phazer\\DIFFUSE_irr_qwantani_rgba32f");
            PbrSpecular = Content.Load<TextureCube>("textures\\phazer\\SkyOnly_SpecularHDR");   // Skyonly
            PbrLut = Content.Load<Texture2D>("textures\\phazer\\Brdf_rgba32f_raw");
        }


        public Texture2D LoadTexture(string fileName)
        {
            if (_textureMap.ContainsKey(fileName))
                return _textureMap[fileName];

            var texture = LoadTextureAsTexture2d(fileName, Game.GraphicsDevice);
            if (texture != null)
                _textureMap[fileName] = texture;
            return texture;
        }

        public void SaveTexture(Texture2D texture, string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            }
        }

        Texture2D LoadTextureAsTexture2d(string fileName, GraphicsDevice device)
        {
            var file = Pfs.FindFile(fileName);
            if (file == null)
            {
                _logger.Here().Error($"Unable to find texture: {fileName}");
                return null;
            }
            try
            {
                var content = file.DataSource.ReadData();
                using (MemoryStream stream = new MemoryStream(content))
                {
                    var image = Pfim.Pfim.FromStream(stream);

                    if (image.Format != ImageFormat.Rgba32)
                    {
                        _logger.Here().Error($"Error loading texture ({fileName} - Unkown textur format {image.Format})");
                        return null;
                    }

                    var texture = new Texture2D(device, image.Width, image.Height, true, SurfaceFormat.Bgra32);
                    texture.SetData(0, null, image.Data, 0, image.DataLen);

                    // Load mipmaps
                    for (int i = 0; i < image.MipMaps.Length; i++)
                    {
                        var mipmap = image.MipMaps[i];
                        if (mipmap.Width > 4)
                            texture.SetData(i + 1, null, image.Data, mipmap.DataOffset, mipmap.DataLen);

                    }

                    return texture;
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error loading texture {fileName} - {e.Message})");
                return null;
            }
        }


        public Effect LoadEffect(string fileName, ShaderTypes type)
        {
            if (_shaders.ContainsKey(type))
                return _shaders[type];
            var effect = Content.Load<Effect>(fileName);
            _shaders[type] = effect;
            return effect;
        }

        public Effect GetEffect(ShaderTypes type)
        {
            if (_shaders.ContainsKey(type))
                return _shaders[type].Clone();
            throw new Exception($"Shader not found: ShaderTypes::{type}");
        }

        internal Effect GetStaticEffect(ShaderTypes type)
        {
            if (_shaders.ContainsKey(type))
                return _shaders[type];
            throw new Exception($"Shader not found: ShaderTypes::{type}");
        }

        public void Dispose()
        {
            foreach (var item in _textureMap)
                item.Value.Dispose();
            _textureMap.Clear();

            foreach (var item in _shaders)
                item.Value.Dispose();
            _shaders.Clear();


            //Content.Dispose();
            Content = null;

            PbrDiffuse.Dispose();
            PbrDiffuse = null;


            PbrSpecular.Dispose();
            PbrSpecular = null;


            PbrLut.Dispose();
            PbrLut = null;
        }
    }
}
