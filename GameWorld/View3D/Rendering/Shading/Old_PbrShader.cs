using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Shading.Shaders;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Shading
{

    /*
    public class PbrShader : IShader
    {
        protected ResourceLibrary _resourceLibrary;

        Dictionary<TextureType, string> _textureMap { get; set; } = [];
        Dictionary<TextureType, bool> _useTextureMap { get; set; } = [];

        public RenderFormats RenderFormat { get; private set; }

        public bool UseAlpha { set; get; } = false;
        public bool UseAnimation { set; get; } = false;
        public Matrix[]? AnimationTransforms { get; set; }
        public int AnimationWeightCount { get; set; }
        public float ScaleMult { get; set; } = 1;

        public PbrShader(ResourceLibrary resourceLibrary, RenderFormats renderFormats)
        {
            _resourceLibrary = resourceLibrary;
            RenderFormat = renderFormats;
        }

        public void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            var effect = GetEffect();
            effect.Parameters["View"].SetValue(commonShaderParameters.View);
            effect.Parameters["Projection"].SetValue(commonShaderParameters.Projection);
            effect.Parameters["EnvMapTransform"].SetValue(Matrix.CreateRotationY(commonShaderParameters.EnvLightRotationsRadians_Y));
            effect.Parameters["DirLightTransform"].SetValue(Matrix.CreateRotationY(commonShaderParameters.DirLightRotationRadians_Y) * Matrix.CreateRotationX(commonShaderParameters.DirLightRotationRadians_X));
            effect.Parameters["LightMult"].SetValue(commonShaderParameters.LightIntensityMult);
            effect.Parameters["World"].SetValue(modelMatrix);
            effect.Parameters["CameraPos"].SetValue(commonShaderParameters.CameraPosition);
        }

        public virtual Effect GetEffect()
        {
            if (RenderFormat == RenderFormats.MetalRoughness)
                return _resourceLibrary.GetStaticEffect(ShaderTypes.Pbs_MetalRough);
            else if (RenderFormat == RenderFormats.SpecGloss)
                return _resourceLibrary.GetStaticEffect(ShaderTypes.Pbr_SpecGloss);

            throw new System.Exception("Unable to determine Shader");
        }

        public void ApplyObjectParameters()
        {
            var effect = GetEffect();
            if (RenderFormat == RenderFormats.MetalRoughness)
            {
                ApplyTextures(effect, _resourceLibrary, [TextureType.BaseColour, TextureType.Normal, TextureType.MaterialMap, TextureType.Mask]);
            }
            else if (RenderFormat == RenderFormats.SpecGloss)
            {
                ApplyTextures(effect, _resourceLibrary, [TextureType.Diffuse, TextureType.Normal, TextureType.Specular]);
            }

            effect.Parameters["UseAlpha"].SetValue(UseAlpha);

            // Apply Animation 
            effect.Parameters["doAnimation"].SetValue(UseAnimation);
            effect.Parameters["WeightCount"].SetValue(AnimationWeightCount);
            effect.Parameters["tranforms"].SetValue(AnimationTransforms);
        }

        void ApplyTextures(Effect effect, ResourceLibrary resourceLibrary, TextureType[] textureTypes)
        {
            foreach (var textureType in textureTypes)
            {
                var textureParam = ShaderParameterHelper.TextureTypeToParamName[textureType];
                var useTextureParam = ShaderParameterHelper.UseTextureTypeToParamName[textureType];

                var textureName = _textureMap[textureType];
                var useTexture = _useTextureMap[textureType];
                var texture = resourceLibrary.GetTexture(textureName);
                if (texture == null || useTexture == false)
                {
                    effect.Parameters[useTextureParam].SetValue(false);
                    continue;
                }

                effect.Parameters[useTextureParam].SetValue(true);
                effect.Parameters[textureParam].SetValue(texture);
            }
        }

        public void SetTexture(TextureType type, string path)
        {
            _textureMap[type] = path;
            UseTexture(type, true);
        }

        public void UseTexture(TextureType type, bool value)
        {
            _useTextureMap[type] = value;
        }

        public PbrShader Clone()
        {
            var shaderClone = new PbrShader(_resourceLibrary, RenderFormat)
            {
                AnimationTransforms = AnimationTransforms,
                AnimationWeightCount = AnimationWeightCount,
                ScaleMult = ScaleMult,
                UseAlpha = UseAlpha,
                UseAnimation = UseAnimation,
                _textureMap = _textureMap.ToDictionary(),
                _useTextureMap = _useTextureMap.ToDictionary()
            };
            return shaderClone;
        }

        public void SetTechnique(RenderingTechnique technique)
        {
            switch (technique)
            {
                case RenderingTechnique.Normal:
                    GetEffect().CurrentTechnique = GetEffect().Techniques["BasicColorDrawing"];
                    return;

                case RenderingTechnique.Emissive:
                    GetEffect().CurrentTechnique = GetEffect().Techniques["GlowDrawing"];
                    return;

                default:
                    throw new Exception($"Unsupported RenderingTechnique {technique}");
            }
        }

        public bool SupportsTechnique(RenderingTechnique technique)
        {
            var supported = new[]{ RenderingTechnique.Normal, RenderingTechnique.Emissive };
            if (supported.Contains(technique))
                return true;
            return false;
        }
    }*/
}
