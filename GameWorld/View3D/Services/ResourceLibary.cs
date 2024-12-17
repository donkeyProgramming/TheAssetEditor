using System;
using System.Collections.Generic;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;

namespace GameWorld.Core.Services
{
    public enum ShaderTypes
    {
        Line,
        Mesh,
        TexturePreview,
        Pbr_SpecGloss,
        Pbs_MetalRough,
        BasicEffect,
        GeometryInstance,
        Glow,
        BloomFilter
    }

    public class ResourceLibrary
    {
        private readonly ILogger _logger = Logging.Create<ResourceLibrary>();
        private readonly Dictionary<ShaderTypes, Effect> _cachedShaders = new();

        private readonly IPackFileService _pfs;
        private GraphicsDevice _graphicsDevice;
        private ContentManager _content;
        private bool _isInitialized = false;
        private TextureCube _pbrDiffuse;
        private TextureCube _pbrSpecular;
        private Texture2D _pbrLut;

        public ContentManager Content => _content;

        public ResourceLibrary(IPackFileService pf)
        {
            _pfs = pf;
        }

        public void Initialize(GraphicsDevice graphicsDevice, ContentManager content)
        {
            try
            {
                if (_isInitialized)
                    return;

                _isInitialized = true;
                _content = content;
                _graphicsDevice = graphicsDevice;

                // Load default resources
                var mr = LoadEffect("Shaders\\Pbr\\MetalRoughness\\MetalRoughness_main", ShaderTypes.Pbs_MetalRough);
                var sg = LoadEffect("Shaders\\Pbr\\SpecGloss\\SpecGloss_Main", ShaderTypes.Pbr_SpecGloss);
                LoadEffect("Shaders\\Geometry\\BasicShader", ShaderTypes.BasicEffect);
                LoadEffect("Shaders\\TexturePreview", ShaderTypes.TexturePreview);
                LoadEffect("Shaders\\LineShader", ShaderTypes.Line);
                LoadEffect("Shaders\\InstancingShader", ShaderTypes.GeometryInstance);

                _pbrDiffuse = _content.Load<TextureCube>("textures\\phazer\\DiffuseAmbientLightCubeMap");
                _pbrSpecular= _content.Load<TextureCube>("textures\\phazer\\SpecularAmbientLightCubemap");
                _pbrLut = _content.Load<Texture2D>("textures\\phazer\\Brdf_rgba32f_raw");
                
                mr.Parameters["tex_cube_diffuse"]?.SetValue(_pbrDiffuse);
                mr.Parameters["tex_cube_specular"]?.SetValue(_pbrSpecular);
                mr.Parameters["specularBRDF_LUT"]?.SetValue(_pbrLut);
                sg.Parameters["tex_cube_diffuse"]?.SetValue(_pbrDiffuse);
                sg.Parameters["tex_cube_specular"]?.SetValue(_pbrSpecular);
                sg.Parameters["specularBRDF_LUT"]?.SetValue(_pbrLut);
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Failed to create rendering resources - {e.Message}");
                throw;
            }
        }

        public void Reset()
        {
            foreach (var item in _cachedShaders)
                item.Value.Dispose();
            _cachedShaders.Clear();

            _pbrDiffuse?.Dispose();
            _pbrDiffuse = null;

            _pbrSpecular?.Dispose();
            _pbrSpecular = null;

            _pbrLut?.Dispose();
            _pbrLut = null;

            _graphicsDevice = null;
            _content = null;
            _isInitialized = false;
        }

        public Texture2D ForceLoadImage(string imagePath, out ImageInformation imageInformation)
        {
            return ImageLoader.ForceLoadImage(imagePath, _pfs, _graphicsDevice, out imageInformation);
        }

        public Texture2D LoadTexture(string fileName, bool forceRefreshTexture = false, bool fromFile = false)
        {
            var texture = ImageLoader.LoadTextureAsTexture2d(fileName, _pfs, _graphicsDevice, out var _, fromFile);
            return texture;
        }

        public Effect LoadEffect(string fileName, ShaderTypes type)
        {
            if (_cachedShaders.TryGetValue(type, out var value))
                return value;

            var effect = _content.Load<Effect>(fileName);
            _cachedShaders[type] = effect;
            return effect;
        }

        public Effect GetStaticEffect(ShaderTypes type)
        {
            if (_cachedShaders.TryGetValue(type, out var value))
                return value;
            throw new Exception($"Shader not found: ShaderTypes::{type}");
        }
    }
}
