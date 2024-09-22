using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.TestUtility.Material
{
    internal static class WsMaterialHelper
    {
        public static WsModelMaterialFile GetDefaultMetalRoughWsModelFile()
        {
            var wsMaterial = new WsModelMaterialFile()
            {
                Alpha = true,
                Name = "cth_celestial_general_body_01_weighted4_alpha_on.xml",
                ShaderPath = "shaders/weighted4_character_alpha.xml.shader",
                Textures = new()
                {
                    {TextureType.BaseColour, $"texturePath/wsmodel/{TextureType.BaseColour}.dds"},
                    {TextureType.MaterialMap, $"texturePath/wsmodel/{TextureType.MaterialMap}.dds"},
                    {TextureType.Normal, $"texturePath/wsmodel/{TextureType.Normal}.dds"},
                    {TextureType.Mask, $"texturePath/wsmodel/{TextureType.Mask}.dds"},
                    {TextureType.Distortion, $"texturePath/wsmodel/{TextureType.Distortion}.dds"},
                    {TextureType.DistortionNoise, $"texturePath/wsmodel/{TextureType.DistortionNoise}.dds"},
                    {TextureType.Blood, $"texturePath/wsmodel/{TextureType.Blood}.dds"},
                },
                Parameters =
                [
                    new WsModelMaterialParam(WsModelParamters.Blood_Use.Name, 1),
                    new WsModelMaterialParam(WsModelParamters.Blood_Scale.Name, new Vector2(1,2)),
                ]
            };
            return wsMaterial;
        }

        public static WsModelMaterialFile GetEmissiveWsModelFile()
        {
            var material = GetDefaultMetalRoughWsModelFile();
            material.Alpha = false;
            material.Name = "cth_celestial_general_body_01_weighted4_alpha_off.xml";
            material.ShaderPath = "shaders/weighted2_character_emissive.xml.shader";

            material.Textures[TextureType.Emissive] = $"texturePath/wsmodel/{TextureType.Emissive}.dds";
            material.Textures[TextureType.EmissiveDistortion] = $"texturePath/wsmodel/{TextureType.EmissiveDistortion}.dds";

            WsModelMaterialParam[] parameters =[
                new WsModelMaterialParam(WsModelParamters.Emissive_Direction.Name, new Vector2(1,2)),
                new WsModelMaterialParam(WsModelParamters.Emissive_DistortStrength.Name, 2),
                new WsModelMaterialParam(WsModelParamters.Emissive_FesnelStrength.Name, 3),
                new WsModelMaterialParam(WsModelParamters.Emissive_Speed.Name, 4),
                new WsModelMaterialParam(WsModelParamters.Emissive_PulseSpeed.Name, 5),
                new WsModelMaterialParam(WsModelParamters.Emissive_PulseStrength.Name, 6),

                new WsModelMaterialParam(WsModelParamters.Emissive_GradientColour1.Name, new Vector3(1,2,3)),
                new WsModelMaterialParam(WsModelParamters.Emissive_GradientColour2.Name, new Vector3(4,5,6)),
                new WsModelMaterialParam(WsModelParamters.Emissive_GradientColour3.Name, new Vector3(7,8,9)),
                new WsModelMaterialParam(WsModelParamters.Emissive_GradientColour4.Name, new Vector3(10,11,12)),

                new WsModelMaterialParam(WsModelParamters.Emissive_GradientTime1.Name, 0),
                new WsModelMaterialParam(WsModelParamters.Emissive_GradientTime2.Name, 1),
                new WsModelMaterialParam(WsModelParamters.Emissive_GradientTime3.Name, 2),
                new WsModelMaterialParam(WsModelParamters.Emissive_GradientTime4.Name, 3),

                new WsModelMaterialParam(WsModelParamters.Emissive_Strength.Name, 7),
                new WsModelMaterialParam(WsModelParamters.Emissive_Tiling.Name, new Vector2(4, 5)),
                new WsModelMaterialParam(WsModelParamters.Emissive_Tint.Name, new Vector3(5,6,7)),
            ];

            material.Parameters.AddRange(parameters);
            return material;
        }

        public static void ValidateEmissive(WsModelMaterialFile generatedMaterial)
        {
            ValidateMetalRough(generatedMaterial);

            Assert.That(generatedMaterial.Textures[TextureType.Emissive], Is.EqualTo($"texturePath/wsmodel/{TextureType.Emissive}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.EmissiveDistortion], Is.EqualTo($"texturePath/wsmodel/{TextureType.EmissiveDistortion}.dds"));

            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Direction).Value, Is.EqualTo("1,2"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_DistortStrength).Value, Is.EqualTo("2"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_FesnelStrength).Value, Is.EqualTo("3"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Speed).Value, Is.EqualTo("4"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_PulseSpeed).Value, Is.EqualTo("5"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_PulseStrength).Value, Is.EqualTo("6"));

            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientColour1).Value, Is.EqualTo("1,2,3"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientColour2).Value, Is.EqualTo("4,5,6"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientColour3).Value, Is.EqualTo("7,8,9"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientColour4).Value, Is.EqualTo("10,11,12"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientTime1).Value, Is.EqualTo("0"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientTime2).Value, Is.EqualTo("1"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientTime3).Value, Is.EqualTo("2"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientTime4).Value, Is.EqualTo("3"));

            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Strength).Value, Is.EqualTo("7"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Tiling).Value, Is.EqualTo("4,5"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Tint).Value, Is.EqualTo("5,6,7"));
        }

        public static void ValidateMetalRough(WsModelMaterialFile generatedMaterial)
        {
            Assert.That(generatedMaterial.Textures[TextureType.BaseColour], Is.EqualTo($"texturePath/wsmodel/{TextureType.BaseColour}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.MaterialMap], Is.EqualTo($"texturePath/wsmodel/{TextureType.MaterialMap}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Normal], Is.EqualTo($"texturePath/wsmodel/{TextureType.Normal}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Mask], Is.EqualTo($"texturePath/wsmodel/{TextureType.Mask}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Distortion], Is.EqualTo($"texturePath/wsmodel/{TextureType.Distortion}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.DistortionNoise], Is.EqualTo($"texturePath/wsmodel/{TextureType.DistortionNoise}.dds"));

            Assert.That(generatedMaterial.Textures[TextureType.Blood], Is.EqualTo($"texturePath/wsmodel/{TextureType.Blood}.dds"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Blood_Use).Value, Is.EqualTo("1"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Blood_Scale).Value, Is.EqualTo("1,2"));
        }
    }
}
