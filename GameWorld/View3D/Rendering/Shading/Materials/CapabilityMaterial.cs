using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Shading.Shaders
{
    public abstract class CapabilityMaterial : IShader
    {
        protected readonly ResourceLibrary _resourceLibrary;
        protected readonly ShaderTypes _shaderType;
        protected Dictionary<RenderingTechnique, string> _renderingTechniqueMap = [];

        public ICapability[] Capabilities { get; protected set; } = [];
        public CapabilityMaterialsEnum Type { get; protected set; }

        protected CapabilityMaterial(CapabilityMaterialsEnum materialType, ShaderTypes shaderType, ResourceLibrary resourceLibrary)
        {
            _shaderType = shaderType;
            _resourceLibrary = resourceLibrary;
            Type = materialType;
        }

        public T GetCapability<T>() where T : class, ICapability
        {
            var cap = TryGetCapability<T>();
            if (cap == null)
                throw new Exception($"{typeof(T)} is not registered capability in {nameof(CapabilityMaterial)} with shader {_shaderType}");
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

        protected ICapability[] CloneCapabilities()
        {
            var output = Capabilities.Select(x => x.Clone()).ToArray();
            return output;
        }

        public void SetTechnique(RenderingTechnique technique)
        {
            var effect = GetEffect();
            if(SupportsTechnique(technique) == false)
                throw new Exception($"Unsupported RenderingTechnique {technique}");

            var techniqueName = _renderingTechniqueMap[technique];
            effect.CurrentTechnique = effect.Techniques[techniqueName];
        }

        public bool SupportsTechnique(RenderingTechnique technique)
        {
            if(_renderingTechniqueMap.ContainsKey(technique)) 
                return true;
            return false;
        }

        Effect GetEffect() => _resourceLibrary.GetStaticEffect(_shaderType);

        public void Apply(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            GetCapability<CommonShaderParametersCapability>().Assign(commonShaderParameters, modelMatrix);

            var effect = GetEffect();

            foreach (var capability in Capabilities)
                capability.Apply(effect, _resourceLibrary);

            effect.CurrentTechnique.Passes[0].Apply();
        }

        public abstract CapabilityMaterial Clone();
    }
}
