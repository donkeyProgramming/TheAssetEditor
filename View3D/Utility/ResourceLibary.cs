using Common;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using View3D.Components;

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

    public class ResourceLibary : BaseComponent
    {
        ILogger _logger = Logging.Create<ResourceLibary>();

        Dictionary<string, Texture2D> _textureMap = new Dictionary<string, Texture2D>();
        Dictionary<ShaderTypes, Effect> _shaders = new Dictionary<ShaderTypes, Effect>();

        PackFileService _pf;
        public ContentManager Content { get; set; }

        public TextureCube PbrDiffuse { get; private set; }
        public TextureCube PbrSpecular { get; private set; }
        public Texture2D PbrLut{ get; private set; }


        public ResourceLibary(WpfGame game, PackFileService pf) : base(game)
        {
            _pf = pf;
        }

        public override void Initialize()
        {
            Content = Game.Content;

            // Load default shaders
            LoadEffect("Shaders\\Phazer\\main", ShaderTypes.Phazer);
            LoadEffect("Shaders\\Geometry\\BasicShader", ShaderTypes.BasicEffect);
            LoadEffect("Shaders\\TexturePreview", ShaderTypes.TexturePreview);
            LoadEffect("Shaders\\LineShader", ShaderTypes.Line);

            PbrDiffuse = Content.Load<TextureCube>("textures\\phazer\\rustig_koppie_DiffuseHDR");
            PbrSpecular = PbrDiffuse;// resourceLibary.XnaContentManager.Load<TextureCube>("textures\\phazer\\rustig_koppie_SpecularHDR");
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

            try
            {
                //r content = File.ReadAllBytes(@"C:\Users\ole_k\Desktop\New folder\rad_rustig.dds");
                var file = _pf.FindFile(fileName) as PackFile;
                if (file == null)
                {
                    _logger.Here().Error($"Unable to find texture: {fileName}");
                    return null;
                }

                var content = file.DataSource.ReadData();
                using (MemoryStream stream = new MemoryStream(content))
                {
                    var image = Pfim.Dds.Create(stream, new Pfim.PfimConfig(32768, Pfim.TargetFormat.Native, false));
                    if (image as Pfim.Dxt1Dds != null)
                    {
                        var texture = new Texture2D(device, image.Width, image.Height, false, SurfaceFormat.Dxt1);

                        texture.SetData(image.Data, 0, (int)image.Header.PitchOrLinearSize);
                        return texture;
                    }
                    else if (image as Pfim.Dxt5Dds != null)
                    {
                        var texture = new Texture2D(device, image.Width, image.Height, false, SurfaceFormat.Dxt5);
                        texture.SetData(image.Data, 0, (int)image.Header.PitchOrLinearSize);
                        return texture;
                    }
                    else if (image as Pfim.Dxt3Dds != null)
                    {
                        var texture = new Texture2D(device, image.Width, image.Height, false, SurfaceFormat.Dxt3);
                        texture.SetData(image.Data, 0, (int)image.Header.PitchOrLinearSize); 
                        return texture;
                    }  
                    else
                    {
                        throw new Exception("Unknow texture format: " + image.ToString() + " Path = " + fileName);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Error loading texture ({fileName}): {e}");
            }
            return null;
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
    }
}
