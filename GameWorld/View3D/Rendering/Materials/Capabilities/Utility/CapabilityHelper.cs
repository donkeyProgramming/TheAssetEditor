using System;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities.Utility
{
    public static class CapabilityHelper
    {
        public static void SetTextureFromModel(IRmvMaterial? rmvMaterial, WsModelMaterialFile? wsModelMaterial, TextureInput textureInput, string defaultPath = "")
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
                    return;
                }
            }

            textureInput.TexturePath = defaultPath;
            textureInput.UseTexture = false;
        }

        public static float GetParameterFloat(WsModelMaterialFile? wsModelMaterial, WsModelParamters.Instance parameterInstance, float defaultValue)
        {
            if (wsModelMaterial == null)
                return defaultValue;

            var parameter = wsModelMaterial.Parameters.FirstOrDefault(x => x.Name == parameterInstance.Name);
            if (parameter == null)
                return defaultValue;

            try
            {
                if (parameter.Type != "float")
                    throw new Exception($"Parameter {parameterInstance.Name} was expected to be float, but was {parameter.Type}");

                var parsedValue = float.Parse(parameter.Value, CultureInfo.InvariantCulture);
                return parsedValue;
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to parse {parameterInstance.Name}. Actuall value was {parameter?.Value}", e);
            }
        }

        public static Vector2 GetParameterVector2(WsModelMaterialFile? wsModelMaterial, WsModelParamters.Instance parameterInstance, Vector2 defaultValue)
        {
            if (wsModelMaterial == null)
                return defaultValue;

            var parameter = wsModelMaterial.Parameters.FirstOrDefault(x => x.Name == parameterInstance.Name);
            if (parameter == null)
                return defaultValue;

            try 
            {
                if (parameter.Type != "float2")
                    throw new Exception($"Parameter {parameterInstance.Name} was expected to be float2, but was {parameter.Type}");

                var values = parameter.Value.Split(",");
                Guard.IsTrue(values.Length == 2);

                var x = float.Parse(values[0], CultureInfo.InvariantCulture);
                var y = float.Parse(values[1], CultureInfo.InvariantCulture);

                return new Vector2(x, y);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to parse {parameterInstance.Name}. Actuall value was {parameter?.Value}", e);
            }
        }

        public static Vector3 GetParameterVector3(WsModelMaterialFile? wsModelMaterial, WsModelParamters.Instance parameterInstance, Vector3 defaultValue)
        {
            if (wsModelMaterial == null)
                return defaultValue;

            var parameter = wsModelMaterial.Parameters.FirstOrDefault(x => x.Name == parameterInstance.Name);
            if (parameter == null)
                return defaultValue;

            try
            {
                if (parameter.Type != "float3")
                    throw new Exception($"Parameter {parameterInstance.Name} was expected to be float3, but was {parameter.Type}");

                var values = parameter.Value.Split(",");
                Guard.IsTrue(values.Length == 3);

                var x = float.Parse(values[0], CultureInfo.InvariantCulture);
                var y = float.Parse(values[1], CultureInfo.InvariantCulture);
                var z = float.Parse(values[2], CultureInfo.InvariantCulture);

                return new Vector3(x, y, z);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to parse {parameterInstance.Name}. Actuall value was {parameter?.Value}", e);
            }
        }

        public static Vector4 GetParameterVector4(WsModelMaterialFile? wsModelMaterial, WsModelParamters.Instance parameterInstance, Vector4 defaultValue)
        {
            if (wsModelMaterial == null)
                return defaultValue;

            var parameter = wsModelMaterial.Parameters.FirstOrDefault(x => x.Name == parameterInstance.Name);
            if (parameter == null)
                return defaultValue;

            try 
            { 
                if (parameter.Type != "float4")
                    throw new Exception($"Parameter {parameterInstance.Name} was expected to be float4, but was {parameter.Type}");

                var values = parameter.Value.Split(",");
                Guard.IsTrue(values.Length == 4);

                var x = float.Parse(values[0], CultureInfo.InvariantCulture);
                var y = float.Parse(values[1], CultureInfo.InvariantCulture);
                var z = float.Parse(values[2], CultureInfo.InvariantCulture);
                var w = float.Parse(values[3], CultureInfo.InvariantCulture);

                return new Vector4(x, y, z, w);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to parse {parameterInstance.Name}. Actuall value was {parameter?.Value}", e);
            }
        }
    }

}
