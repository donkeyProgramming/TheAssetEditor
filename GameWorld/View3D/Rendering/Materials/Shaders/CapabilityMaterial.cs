using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Shaders
{
    public enum CapabilityMaterialsEnum
    {
        MetalRoughPbr_Default,
        MetalRoughPbr_Emissive,

        SpecGlossPbr_Default,
        SpecGlossPbr_Advanced,
    }

    public abstract class CapabilityMaterial : IShader
    {
        protected readonly IScopedResourceLibrary _resourceLibrary;
        protected readonly ShaderTypes _shaderType;
        protected Dictionary<RenderingTechnique, string> _renderingTechniqueMap = [];

        public ICapability[] Capabilities { get; protected set; } = [];
        public CapabilityMaterialsEnum Type { get; protected set; }

        protected CapabilityMaterial(CapabilityMaterialsEnum materialType, ShaderTypes shaderType, IScopedResourceLibrary resourceLibrary)
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
                var isTypeOrChild = typeof(T).IsAssignableFrom(capability.GetType());
                if (isTypeOrChild)
                    return (capability as T)!;
            }

            return null;
        }

        public void SetTechnique(RenderingTechnique technique)
        {
            var effect = GetEffect();
            if (SupportsTechnique(technique) == false)
                throw new Exception($"Unsupported RenderingTechnique {technique}");

            var techniqueName = _renderingTechniqueMap[technique];
            effect.CurrentTechnique = effect.Techniques[techniqueName];
        }

        public bool SupportsTechnique(RenderingTechnique technique)
        {
            if (_renderingTechniqueMap.ContainsKey(technique))
                return true;
            return false;
        }

        protected Effect GetEffect() => _resourceLibrary.GetStaticEffect(_shaderType);

        public void Apply(CommonShaderParameters commonShaderParameters, Matrix modelMatrix)
        {
            GetCapability<CommonShaderParametersCapability>().Assign(commonShaderParameters, modelMatrix);

            var tintCapability = TryGetCapability<TintCapability>();

            if (tintCapability != null)
                tintCapability.FactionColours = commonShaderParameters.FactionColours;

            var effect = GetEffect();
            OnApply(effect);

            foreach (var capability in Capabilities)
                capability.Apply(effect, _resourceLibrary);

            effect.CurrentTechnique.Passes[0].Apply();
        }

        protected virtual void OnApply(Effect effect)
        { }

        protected abstract CapabilityMaterial CreateCloneInstance();
        protected ICapability[] CloneCapabilities()
        {
            var output = Capabilities.Select(x => x.Clone()).ToArray();
            return output;
        }
        public CapabilityMaterial Clone()
        {
            var copy = CreateCloneInstance();
            copy.Capabilities = CloneCapabilities();
            return copy;
        }

        public (bool Result, string Message) AreEqual(CapabilityMaterial other)
        {
            if (other.Type != Type)
                return (false, $"Different material types {Type} vs {other.Type}");

            for (var i = 0; i < Capabilities.Length; i++)
            {
                var ownCap = Capabilities[i];
                var otherCap = other.Capabilities[i];
                if (ownCap.GetType() != otherCap.GetType())
                    throw new Exception($"Comparing material {Type} and {other.Type}. They have different caps in index {i} {ownCap.GetType()} vs {otherCap}");

                var res = ownCap.AreEqual(otherCap);
                if (res.Result == false)
                    return res;
            }

            return (true, "");
        }

        internal void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            foreach (var capability in Capabilities)
                capability.Initialize(wsModelMaterial, rmvMaterial);
        }
    }
}
