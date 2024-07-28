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
        public ICapability[] Capabilities { get; }

        public T GetCapability<T>() where T : class, ICapability
        {
            var cap = TryGetCapability<T>();
            if(cap == null)
                throw new Exception($"{typeof(T)} is not registered capability in {nameof(ICapabilityMaterial)}");
            return cap;
        }

        public T? TryGetCapability<T>() where T : class, ICapability
        {
            foreach (var capability in Capabilities)
            {
                if (capability.GetType() == typeof(T))
                    return (capability as T)!;
            }

            return null;
        }

        public ICapabilityMaterial Clone();
    }

    public class DefaultCapabilityMaterialWh3 : ICapabilityMaterial
    {
        public ICapabilityMaterial Clone()  => throw new NotImplementedException();


        protected ResourceLibrary _resourceLibrary;

        private readonly CommonShaderParametersCapability _commonShaderParametersCapability = new();
        //private readonly DefaultCapability _defaultCapability = new();
        //private readonly AnimationCapability _animationCapability = new();
        //private readonly BloodCapability _bloodCapability = new();

        public ICapability[] Capabilities { get; private set; }

        public DefaultCapabilityMaterialWh3(ResourceLibrary resourceLibrary)
        {
            Capabilities = [_commonShaderParametersCapability, new DefaultCapability(), new AnimationCapability(), new BloodCapability()];
            _resourceLibrary = resourceLibrary;
        }

        public void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            _commonShaderParametersCapability.Assign(commonShaderParameters, modelMatrix);
        }

        public void ApplyObjectParameters()
        {
            var effect = GetEffect();

            foreach (var capability in Capabilities)
                capability.Apply(effect, _resourceLibrary); 
        }

        public void SetTechnique(RenderingTechnique technique)
        {
            var effect = GetEffect();
            switch (technique)
            {
                case RenderingTechnique.Normal:
                    effect.CurrentTechnique = effect.Techniques["BasicColorDrawing"];
                    return;

                case RenderingTechnique.Emissive:
                    effect.CurrentTechnique = effect.Techniques["GlowDrawing"];
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
