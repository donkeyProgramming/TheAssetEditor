using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Rendering.Shaders.MetalRough
{
    internal class EmissiveMaterialTest
    {
        CapabilityMaterialFactory _abstractMaterialFactory;
        MaterialToWsMaterialSerializer _wsMaterialSerializer;
        PackFileService _pfs;

        [SetUp]
        public void Setup()
        {
            var selectedGame = GameTypeEnum.Warhammer3;
            var appSettings = new ApplicationSettingsService(selectedGame);
            _pfs = new PackFileService(new PackFileDataBase(), appSettings, new GameInformationFactory(), null, null, null);
            var _outputPack = _pfs.CreateNewPackFileContainer("output", PackFileCAType.MOD, true);

            _abstractMaterialFactory = new CapabilityMaterialFactory(appSettings, null);

            var saveHelper = new PackFileSaveService(_pfs);
            var materialRepo = new WsMaterialRepository(_pfs);
            _wsMaterialSerializer = new MaterialToWsMaterialSerializer(saveHelper, materialRepo, selectedGame);
        }


        WsModelMaterialFile GetWsModelFile()
        {
            var wsMaterial = new WsModelMaterialFile()
            {
                Alpha = false,
                Name = "cth_celestial_general_body_01_weighted4_alpha_off.xml",
                ShaderPath = "shaders/weighted2_character_emissive.xml.shader",
                Textures = new()
                {
                    {TextureType.Emissive, $"texturePath/wsmodel/{TextureType.Emissive}.dds"},
                    {TextureType.EmissiveDistortion, $"texturePath/wsmodel/{TextureType.EmissiveDistortion}.dds"},
                },
                Parameters =
                [
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
                ]
            };
            return wsMaterial;
        }

        [Test]
        public void CreateFromWsMaterial()
        {
            // Arrange
            var wsMaterial = GetWsModelFile();

            // Act
            var material = _abstractMaterialFactory.Create(null, wsMaterial);

            // Assert
            var metalRough = material.GetCapability<MetalRoughCapability>();
            Assert.That(metalRough, Is.Not.Null);

            var emissiveCap = material.GetCapability<EmissiveCapability>();
            Assert.That(emissiveCap.Emissive.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Emissive}.dds"));
            Assert.That(emissiveCap.EmissiveDistortion.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.EmissiveDistortion}.dds"));
            Assert.That(emissiveCap.EmissiveDirection, Is.EqualTo(new Vector2(1,2)));
            Assert.That(emissiveCap.EmissiveDistortStrength, Is.EqualTo(2));
            Assert.That(emissiveCap.EmissiveFresnelStrength, Is.EqualTo(3));
            Assert.That(emissiveCap.EmissiveSpeed, Is.EqualTo(4));
            Assert.That(emissiveCap.EmissivePulseSpeed, Is.EqualTo(5));
            Assert.That(emissiveCap.EmissivePulseStrength, Is.EqualTo(6));
            Assert.That(emissiveCap.GradientColours[0], Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(emissiveCap.GradientColours[1], Is.EqualTo(new Vector3(4, 5, 6)));
            Assert.That(emissiveCap.GradientColours[2], Is.EqualTo(new Vector3(7, 8, 9)));
            Assert.That(emissiveCap.GradientColours[3], Is.EqualTo(new Vector3(10, 11, 12)));
            Assert.That(emissiveCap.GradientTimes[0], Is.EqualTo(0));
            Assert.That(emissiveCap.GradientTimes[1], Is.EqualTo(1));
            Assert.That(emissiveCap.GradientTimes[2], Is.EqualTo(2));
            Assert.That(emissiveCap.GradientTimes[3], Is.EqualTo(3));
            Assert.That(emissiveCap.EmissiveStrength, Is.EqualTo(7));
            Assert.That(emissiveCap.EmissiveTiling, Is.EqualTo(new Vector2(4, 5)));
            Assert.That(emissiveCap.EmissiveTint, Is.EqualTo(new Vector3(5,6,7)));
        }

        [Test]
        public void GenerateWsMaterial()
        {
            // Arrange
            var wsMaterial = GetWsModelFile();

            // Act
            var material = _abstractMaterialFactory.Create(null, wsMaterial);
            var wsMaterialPath = _wsMaterialSerializer.ProsessMaterial("custompath/materials", "mymesh", UiVertexFormat.Weighted, material);
            var packfile = _pfs.FindFile(wsMaterialPath);
            var generatedMaterial = new WsModelMaterialFile(packfile);

            // Assert
            Assert.That(generatedMaterial.VertexType, Is.EqualTo(UiVertexFormat.Weighted));
            Assert.That(generatedMaterial.Alpha, Is.EqualTo(false));

            Assert.That(generatedMaterial.ShaderPath, Is.EqualTo("shaders/weighted2_character_emissive.xml.shader"));
            Assert.That(generatedMaterial.Name, Is.EqualTo("mymesh_weighted2_alpha_off.xml"));

            Assert.That(generatedMaterial.Textures[TextureType.Emissive], Is.EqualTo($"texturePath/wsmodel/{TextureType.Emissive}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.EmissiveDistortion], Is.EqualTo($"texturePath/wsmodel/{TextureType.EmissiveDistortion}.dds"));

            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Direction).Value, Is.EqualTo("1, 2"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_DistortStrength).Value, Is.EqualTo("2"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_FesnelStrength).Value, Is.EqualTo("3"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Speed).Value, Is.EqualTo("4"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_PulseSpeed).Value, Is.EqualTo("5"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_PulseStrength).Value, Is.EqualTo("6"));

            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientColour1).Value, Is.EqualTo("1, 2, 3"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientColour2).Value, Is.EqualTo("4, 5, 6"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientColour3).Value, Is.EqualTo("7, 8, 9"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientColour4).Value, Is.EqualTo("10, 11, 12"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientTime1).Value, Is.EqualTo("0"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientTime2).Value, Is.EqualTo("1"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientTime3).Value, Is.EqualTo("2"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_GradientTime4).Value, Is.EqualTo("3"));

            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Strength).Value, Is.EqualTo("7"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Tiling).Value, Is.EqualTo("4, 5"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Emissive_Tint).Value,  Is.EqualTo("5, 6, 7"));
        }
    }
}
