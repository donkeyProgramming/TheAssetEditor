using System;
using System.Collections.Generic;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.Core.Rendering.Shading.Shaders;
using Microsoft.Xna.Framework;
using Shared.Core.Services;
using Shared.EmbeddedResources;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Services.SceneSaving.Material.Strategies
{
    public static class CapabilityMaterialFactory
    {
        readonly private static Dictionary<GameTypeEnum, Func<IWsMaterialBuilder>> s_map = new()
        {
            //{GameTypeEnum.Pharaoh, () => new CapabilityMaterialBuilderPharaoh() },
            //
            //{GameTypeEnum.Warhammer2, () => new CapabilityMaterialBuilderWarhammer2() },
            //{GameTypeEnum.Troy, () => new CapabilityMaterialBuilderWarhammer2() },

            {GameTypeEnum.Warhammer3, () => new CapabilityMaterialBuilderWarhammer3() },
        };


        public static IWsMaterialBuilder GetBuilder(GameTypeEnum currentGame)
        {
            var found = s_map.TryGetValue(currentGame, out var builder);
            if (found == false)
                throw new Exception($"Trying to get {nameof(IWsMaterialBuilder)} for {currentGame}, which is not implemented");
            var newBuilder = builder!();
            return newBuilder;
        }
    }

    public abstract class IWsMaterialBuilder
    {
        protected string? _templateBuffer;
        private string? _templateName;

        public abstract (string FileName, string FileContent) Create(string meshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial);

        protected void LoadTemplate(string templatePath)
        {
            _templateName = templatePath;
            _templateBuffer = ResourceLoader.LoadString(templatePath);
        }

        protected void Add(string templateAttributeName, string value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, value);
        }

        protected void Add(string templateAttributeName, float value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, value.ToString());
        }

        protected void Add(string templateAttributeName, Vector2 value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, $"{value.X}, {value.Y}");
        }

        protected void Add(string templateAttributeName, Vector3 value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, $"{value.X}, {value.Y}, {value.Z}");
        }

        protected void Add(string templateAttributeName, Vector4 value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");
            _templateBuffer = _templateBuffer!.Replace(templateAttributeName, $"{value.X}, {value.Y}, {value.Z}, {value.W}");
        }

        protected void Add(string templateAttributeName, TextureInput value)
        {
            if (_templateBuffer!.Contains(templateAttributeName) == false)
                throw new Exception($"Attribute {templateAttributeName} not found in template {_templateName}");

            if (string.IsNullOrWhiteSpace(value.TexturePath))
                _templateBuffer = _templateBuffer!.Replace(templateAttributeName, "test_mask.dds");
            else
                _templateBuffer = _templateBuffer!.Replace(templateAttributeName, value.TexturePath);
        }

        protected void Verify()
        {
            var hasValue = _templateBuffer!.Contains("TEMPLATE_ATTR");
            if (hasValue)
                throw new Exception("Failed to generate material, not all template attributes are replaced!");
        }
    }
}
