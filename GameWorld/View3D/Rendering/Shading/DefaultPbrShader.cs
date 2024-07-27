using System;
using System.Linq;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;



namespace GameWorld.Core.Rendering.Shading
{
    public interface ICapabilityMaterial : IShader
    {
        public T? GetCapability<T>() where T : class, ICapability;

        public ICapabilityMaterial Clone();
    }

    public class DefaultPbrShaderWh3 : ICapabilityMaterial
    {
        public T? GetCapability<T>() where T : class, ICapability
        {
            foreach (var capability in _capabilities)
            {
                if (capability.GetType() == typeof(T))
                    return capability as T;
            }

            return null;
        }

        public ICapabilityMaterial Clone() 
        {
            throw new NotImplementedException();
        }


        protected ResourceLibrary _resourceLibrary;

        private readonly CommonShaderParametersCapability _commonShaderParametersCapability = new();
        private readonly SharedCapability _sharedCapability = new();
        private readonly AnimationCapability _animationCapability = new();

        private readonly ICapability[] _capabilities;

        public DefaultPbrShaderWh3(ResourceLibrary resourceLibrary)
        {
            _capabilities = [_commonShaderParametersCapability, _sharedCapability, _animationCapability];
            _resourceLibrary = resourceLibrary;
        }

        public void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            _commonShaderParametersCapability.Assign(commonShaderParameters, modelMatrix);
        }

        public void ApplyObjectParameters()
        {
            var effect = GetEffect();

            _commonShaderParametersCapability.Apply(effect);
            _sharedCapability.Apply(effect, _resourceLibrary);
            _animationCapability.Apply(effect, _resourceLibrary);
        }

        //public void SetTexture(TextureType type, string texturePath)
        //{
        //    if (_sharedCapability.TextureMap.ContainsKey(type))
        //    {
        //        _sharedCapability.TextureMap[type].TexturePath = texturePath;
        //        _sharedCapability.TextureMap[type].UseTexture = true;
        //    }
        //}
        //
        //public void UseTexture(TextureType type, bool value)
        //{
        //    if (_sharedCapability.TextureMap.ContainsKey(type))
        //        _sharedCapability.TextureMap[type].UseTexture = value;
        //}

        //public PbrShader Clone()
        //{
        //    throw new NotImplementedException();
        //
        //    //var shaderClone = new DefaultPbrShaderWh3(_resourceLibrary)
        //    //{
        //    //    AnimationTransforms = AnimationTransforms,
        //    //    AnimationWeightCount = AnimationWeightCount,
        //    //    ScaleMult = ScaleMult,
        //    //    UseAlpha = UseAlpha,
        //    //    UseAnimation = UseAnimation,
        //    //    //_textureMap = _textureMap.ToDictionary(),
        //    //   // _useTextureMap = _useTextureMap.ToDictionary()
        //    //};
        //    //return shaderClone;
        //}

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
            var supported = new[] { RenderingTechnique.Normal, RenderingTechnique.Emissive };
            if (supported.Contains(technique))
                return true;
            return false;
        }

        public virtual Effect GetEffect() => _resourceLibrary.GetStaticEffect(ShaderTypes.Pbs_MetalRough);
    }
}
