using System;
using System.Collections.Generic;
using GameWorld.WpfWindow;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.PackFiles;

namespace GameWorld.WpfWindow.ResourceHandling
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
    }

    public class ResourceLibrary
    {
        //private readonly  ILogger _logger = Logging.Create<ResourceLibary>();

        private readonly Dictionary<string, Texture2D> _cachedTextures = new();
        private readonly Dictionary<ShaderTypes, Effect> _cachedShaders = new();

        private readonly PackFileService _pfs;
        private IWpfGame _gameWorld;
        private bool _isInitialized = false;

        public SpriteBatch CommonSpriteBatch { get; private set; }
        public SpriteFont DefaultFont { get; private set; }

        public TextureCube PbrDiffuse { get; private set; }
        public TextureCube PbrSpecular { get; private set; }
        public Texture2D PbrLut { get; private set; }

        public ResourceLibrary(PackFileService pf)
        {
            _pfs = pf;
        }

        public SpriteFont LoadFont(string path) => _gameWorld.Content.Load<SpriteFont>(path);

        public void Initialize(IWpfGame game)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            _gameWorld = game;

            // Load default shaders
            LoadEffect("Shaders\\Phazer\\MetalRoughness_main", ShaderTypes.Pbs_MetalRough);
            LoadEffect("Shaders\\Phazer\\SpecGloss_main", ShaderTypes.Pbr_SpecGloss);
            LoadEffect("Shaders\\Geometry\\BasicShader", ShaderTypes.BasicEffect);
            LoadEffect("Shaders\\TexturePreview", ShaderTypes.TexturePreview);
            LoadEffect("Shaders\\LineShader", ShaderTypes.Line);

            DefaultFont = LoadFont("Fonts//DefaultFont");
            CommonSpriteBatch = new SpriteBatch(_gameWorld.GraphicsDevice);

            PbrDiffuse = _gameWorld.Content.Load<TextureCube>("textures\\phazer\\DIFFUSE_IRRADIANCE_edited_kloppenheim_06_128x128");
            PbrSpecular = _gameWorld.Content.Load<TextureCube>("textures\\phazer\\SPECULAR_RADIANCE_edited_kloppenheim_06_512x512");
            PbrLut = _gameWorld.Content.Load<Texture2D>("textures\\phazer\\Brdf_rgba32f_raw");
        }

        public void Reset()
        {
            foreach (var item in _cachedTextures)
                item.Value.Dispose();
            _cachedTextures.Clear();

            foreach (var item in _cachedShaders)
                item.Value.Dispose();
            _cachedShaders.Clear();

            PbrDiffuse?.Dispose();
            PbrDiffuse = null;

            PbrSpecular?.Dispose();
            PbrSpecular = null;

            PbrLut?.Dispose();
            PbrLut = null;

            CommonSpriteBatch?.Dispose();
            CommonSpriteBatch = null;

            _gameWorld = null;
            _isInitialized = false;
        }

        public Texture2D ForceLoadImage(string imagePath, out ImageInformation imageInformation)
        {
            return ImageLoader.ForceLoadImage(imagePath, _pfs, _gameWorld.GraphicsDevice, out imageInformation);
        }

        public Texture2D LoadTexture(string fileName, bool forceRefreshTexture = false, bool fromFile = false)
        {
            if (forceRefreshTexture == false)
            {
                if (_cachedTextures.ContainsKey(fileName))
                    return _cachedTextures[fileName];
            }

            var texture = ImageLoader.LoadTextureAsTexture2d(fileName, _pfs, _gameWorld.GraphicsDevice, out var _, fromFile);
            if (texture != null)
                _cachedTextures[fileName] = texture;
            return texture;
        }

        public Effect LoadEffect(string fileName, ShaderTypes type)
        {
            if (_cachedShaders.ContainsKey(type))
                return _cachedShaders[type];
            var effect = _gameWorld.Content.Load<Effect>(fileName);
            _cachedShaders[type] = effect;
            return effect;
        }

        public Effect GetEffect(ShaderTypes type)
        {
            if (_cachedShaders.ContainsKey(type))
                return _cachedShaders[type].Clone();
            throw new Exception($"Shader not found: ShaderTypes::{type}");
        }

        public Effect GetStaticEffect(ShaderTypes type)
        {
            if (_cachedShaders.ContainsKey(type))
                return _cachedShaders[type];
            throw new Exception($"Shader not found: ShaderTypes::{type}");
        }

        public SpriteBatch CreateSpriteBatch() => new SpriteBatch(_gameWorld.GraphicsDevice);
    }
}
