using System;
using System.Linq;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Shading
{
    public abstract class ICapabilityMaterial : IShader
    {
        public ICapability[] Capabilities { get; protected set; } = [];

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

  
        public abstract ICapabilityMaterial Clone();
        public abstract void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix);
        public abstract void ApplyObjectParameters();
        public abstract Effect GetEffect();
        public abstract void SetTechnique(RenderingTechnique technique);
        public abstract bool SupportsTechnique(RenderingTechnique technique);
    }

    public class DefaultCapabilityMaterialWh3 : ICapabilityMaterial
    {
        public override ICapabilityMaterial Clone()  => throw new NotImplementedException();

        protected ResourceLibrary _resourceLibrary;


        public DefaultCapabilityMaterialWh3(ResourceLibrary resourceLibrary)
        {
            _resourceLibrary = resourceLibrary;
            Capabilities = 
                [
                    new CommonShaderParametersCapability(), 
                    new DefaultCapability(), 
                    new AnimationCapability(), 
                    new BloodCapability(), 
                    new EmissiveCapability()
                ];
        }

        public override void SetCommonParameters(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            GetCapability<CommonShaderParametersCapability>().Assign(commonShaderParameters, modelMatrix);
        }

        public override void ApplyObjectParameters()
        {
            var effect = GetEffect();

            foreach (var capability in Capabilities)
                capability.Apply(effect, _resourceLibrary); 
        }

        public override void SetTechnique(RenderingTechnique technique)
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

        public override bool SupportsTechnique(RenderingTechnique technique)
        {
            var supported = new[] { RenderingTechnique.Normal, RenderingTechnique.Emissive };
            if (supported.Contains(technique))
                return true;
            return false;
        }

        public override Effect GetEffect() => _resourceLibrary.GetStaticEffect(ShaderTypes.Pbs_MetalRough);
    }
}
