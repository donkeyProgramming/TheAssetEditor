using System;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities.Utility
{
    public static class CapabilityHelper
    {
        public static void SetTextureFromModel(IRmvMaterial? rmvMaterial, WsModelMaterialFile? wsModelMaterial, TextureInput textureInput)
        {
            if (wsModelMaterial != null)
            {
                var hasKey = wsModelMaterial.Textures.ContainsKey(textureInput.Type);
                if (hasKey)
                {
                    textureInput.TexturePath = wsModelMaterial.Textures[textureInput.Type];
                    textureInput.UseTexture = true;
                    return;
                }
            }

            if (rmvMaterial != null)
            {
                var textureType = textureInput.Type;
                var modelTexture = rmvMaterial.GetTexture(textureType);
                if (modelTexture != null)
                {
                    textureInput.TexturePath = modelTexture.Value.Path;
                    textureInput.UseTexture = true;
                }
            }
        }

        public static bool UseAlpha(IRmvMaterial rmvMaterial, WsModelMaterialFile? wsModelMaterial)
        {
            if (wsModelMaterial != null)
                return wsModelMaterial.Alpha;

            return rmvMaterial.AlphaMode == AlphaMode.Transparent;
        }

        public static float GetParameterFloat(WsModelMaterialFile? wsModelMaterial, string parameterName, float defaultValue)
        {
            if (wsModelMaterial == null)
                return defaultValue;

            var parameter = wsModelMaterial.Parameters.FirstOrDefault(x => x.Name == parameterName);
            if (parameter == null)
                return defaultValue;

            if (parameter.Type != "float")
                throw new Exception($"Parameter {parameterName} was expected to be float2, but was {parameter.Type}");

            var parsedValue = float.Parse(parameter.Value);
            return parsedValue;
        }


        public static Vector2 GetParameterVector2(WsModelMaterialFile? wsModelMaterial, string parameterName, Vector2 defaultValue)
        {
            if (wsModelMaterial == null)
                return defaultValue;

            var parameter = wsModelMaterial.Parameters.FirstOrDefault(x => x.Name == parameterName);
            if (parameter == null)
                return defaultValue;

            if (parameter.Type != "float2")
                throw new Exception($"Parameter {parameterName} was expected to be float2, but was {parameter.Type}");

            var values = parameter.Value.Split(",");
            Guard.IsTrue(values.Length == 2);

            var x = float.Parse(values[0]);
            var y = float.Parse(values[1]);

            return new Vector2(x, y);
        }

        public static Vector3 GetParameterVector3(WsModelMaterialFile? wsModelMaterial, string parameterName, Vector3 defaultValue)
        {
            if (wsModelMaterial == null)
                return defaultValue;

            var parameter = wsModelMaterial.Parameters.FirstOrDefault(x => x.Name == parameterName);
            if (parameter == null)
                return defaultValue;

            if (parameter.Type != "float3")
                throw new Exception($"Parameter {parameterName} was expected to be float3, but was {parameter.Type}");

            var values = parameter.Value.Split(",");
            Guard.IsTrue(values.Length == 3);

            var x = float.Parse(values[0]);
            var y = float.Parse(values[1]);
            var z = float.Parse(values[2]);

            return new Vector3(x, y, z);
        }

        public static Vector4 GetParameterVector4(WsModelMaterialFile? wsModelMaterial, string parameterName, Vector4 defaultValue)
        {
            if (wsModelMaterial == null)
                return defaultValue;

            var parameter = wsModelMaterial.Parameters.FirstOrDefault(x => x.Name == parameterName);
            if (parameter == null)
                return defaultValue;

            if (parameter.Type != "float4")
                throw new Exception($"Parameter {parameterName} was expected to be float4, but was {parameter.Type}");

            var values = parameter.Value.Split(",");
            Guard.IsTrue(values.Length == 4);

            var x = float.Parse(values[0]);
            var y = float.Parse(values[1]);
            var z = float.Parse(values[2]);
            var w = float.Parse(values[3]);

            return new Vector4(x, y, z, w);
        }
    }

}
