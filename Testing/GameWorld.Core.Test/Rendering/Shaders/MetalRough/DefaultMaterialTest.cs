﻿using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Test.TestUtility;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Rendering.Shaders.MetalRough
{
    internal class DefaultMaterialTest
    {
        PackFileService _pfs;

        [SetUp]
        public void Setup()
        {
            var selectedGame = GameTypeEnum.Warhammer3;
            var appSettings = new ApplicationSettingsService(selectedGame);
            _pfs = new PackFileService(new PackFileDataBase(), appSettings, new GameInformationFactory(), null, null, null);
            var _ = _pfs.CreateNewPackFileContainer("output", PackFileCAType.MOD, true);
        }

        CapabilityMaterialFactory GetMaterialFactory(GameTypeEnum gameTypeEnum)
        {
            var appSettings = new ApplicationSettingsService(gameTypeEnum);
            return new CapabilityMaterialFactory(appSettings, null);
        }

        MaterialToWsMaterialSerializer CreateWsMaterialSerializer(GameTypeEnum gameTypeEnum)
        {
            var saveHelper = new PackFileSaveService(_pfs);
            var materialRepo = new WsMaterialRepository(_pfs);
            return new MaterialToWsMaterialSerializer(saveHelper, materialRepo, gameTypeEnum);
        }

        WsModelMaterialFile GetWsModelFile()
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

        IRmvMaterial GetRmvMaterial()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted)
                .SetAlpha(true)
                .AssignMaterials([TextureType.BaseColour, TextureType.MaterialMap, TextureType.Normal, TextureType.Mask]);

            return rmvMaterial;
        }

        [Test]
        public void CreateFromWsMaterial()
        {
            // Arrange
            var wsMaterial = GetWsModelFile();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.Warhammer3).Create(null, wsMaterial);

            // Assert
            var metalRoughCap = material.GetCapability<MetalRoughCapability>();
            Assert.That(metalRoughCap.UseAlpha, Is.EqualTo(true));
            Assert.That(metalRoughCap.BaseColour.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.BaseColour}.dds"));
            Assert.That(metalRoughCap.MaterialMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.MaterialMap}.dds"));
            Assert.That(metalRoughCap.NormalMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Normal}.dds"));
            Assert.That(metalRoughCap.Mask.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Mask}.dds"));
            Assert.That(metalRoughCap.Distortion.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Distortion}.dds"));
            Assert.That(metalRoughCap.DistortionNoise.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.DistortionNoise}.dds"));

            var bloodCap = material.GetCapability<BloodCapability>();
            Assert.That(bloodCap.BloodMask.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Blood}.dds"));
            Assert.That(bloodCap.UseBlood, Is.EqualTo(true));
            Assert.That(bloodCap.UvScale, Is.EqualTo(new Vector2(1,2)));
        }

        [Test]
        [TestCase(GameTypeEnum.Warhammer3)]
        [TestCase(GameTypeEnum.ThreeKingdoms)]
        public void GenerateWsMaterial(GameTypeEnum gameType)
        {
            // Arrange
            var wsMaterial = GetWsModelFile();

            // Act
            var material = GetMaterialFactory(gameType).Create(null, wsMaterial);
            var serializer = CreateWsMaterialSerializer(gameType);
            var wsMaterialPath = serializer.ProsessMaterial("custompath/materials", "mymesh", UiVertexFormat.Cinematic, material);
            var packfile = _pfs.FindFile(wsMaterialPath);
            var generatedMaterial = new WsModelMaterialFile(packfile);

            // Assert
            Assert.That(generatedMaterial.VertexType, Is.EqualTo(UiVertexFormat.Cinematic));
            Assert.That(generatedMaterial.Alpha, Is.EqualTo(true));
           
            Assert.That(generatedMaterial.ShaderPath, Is.EqualTo("shaders/weighted4_character_alpha.xml.shader"));
            Assert.That(generatedMaterial.Name, Is.EqualTo("mymesh_weighted4_alpha_on.xml"));

            Assert.That(generatedMaterial.Textures[TextureType.BaseColour], Is.EqualTo($"texturePath/wsmodel/{TextureType.BaseColour}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.MaterialMap], Is.EqualTo($"texturePath/wsmodel/{TextureType.MaterialMap}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Normal], Is.EqualTo($"texturePath/wsmodel/{TextureType.Normal}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Mask], Is.EqualTo($"texturePath/wsmodel/{TextureType.Mask}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.Distortion], Is.EqualTo($"texturePath/wsmodel/{TextureType.Distortion}.dds"));
            Assert.That(generatedMaterial.Textures[TextureType.DistortionNoise], Is.EqualTo($"texturePath/wsmodel/{TextureType.DistortionNoise}.dds"));

            Assert.That(generatedMaterial.Textures[TextureType.Blood], Is.EqualTo($"texturePath/wsmodel/{TextureType.Blood}.dds"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Blood_Use).Value, Is.EqualTo("1"));
            Assert.That(generatedMaterial.GetParameter(WsModelParamters.Blood_Scale).Value, Is.EqualTo("1, 2"));
        }

        [Test]
        public void CreateFromRmvMaterial()
        {
            // Arrange
            var rmvMaterial = GetRmvMaterial();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.Warhammer3).Create(rmvMaterial, null);

            // Assert
            var metalRoughCap = material.GetCapability<MetalRoughCapability>();
            Assert.That(metalRoughCap.UseAlpha, Is.EqualTo(true));
            Assert.That(metalRoughCap.BaseColour.TexturePath, Is.EqualTo($"texturePath/{TextureType.BaseColour}.dds"));
            Assert.That(metalRoughCap.MaterialMap.TexturePath, Is.EqualTo($"texturePath/{TextureType.MaterialMap}.dds"));
            Assert.That(metalRoughCap.NormalMap.TexturePath, Is.EqualTo($"texturePath/{TextureType.Normal}.dds"));
            Assert.That(metalRoughCap.Mask.TexturePath, Is.EqualTo($"texturePath/{TextureType.Mask}.dds"));
            Assert.That(metalRoughCap.Distortion.TexturePath, Is.EqualTo("commontextures/winds_of_magic_specular.dds"));
            Assert.That(metalRoughCap.DistortionNoise.TexturePath, Is.EqualTo($"commontextures/winds_of_magic_noise.dds"));

            var bloodCap = material.GetCapability<BloodCapability>();
            Assert.That(bloodCap.BloodMask.TexturePath, Is.EqualTo("commontextures/bloodmap.dds"));
            Assert.That(bloodCap.UseBlood, Is.EqualTo(false));
            Assert.That(bloodCap.UvScale, Is.EqualTo(new Vector2(1, 1)));
        }

        [Test]
        public void GenerateRmvMaterial()
        {
            // Arrange
            var rmvMaterial = GetRmvMaterial();

            // Act
            var material = GetMaterialFactory(GameTypeEnum.ThreeKingdoms).Create(rmvMaterial, null);
            var serializer = new MaterialToRmvSerializer();
            var createdRmvMaterial = serializer.CreateMaterialFromCapabilityMaterial(material);

            // Assert
            Assert.That(createdRmvMaterial.AlphaMode, Is.EqualTo(AlphaMode.Transparent));
            Assert.That(createdRmvMaterial.MaterialId, Is.EqualTo(ModelMaterialEnum.weighted));

            Assert.That(createdRmvMaterial.GetTexture(TextureType.BaseColour).Value.Path, Is.EqualTo($"texturePath/{TextureType.BaseColour}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.MaterialMap).Value.Path, Is.EqualTo($"texturePath/{TextureType.MaterialMap}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.Normal).Value.Path, Is.EqualTo($"texturePath/{TextureType.Normal}.dds"));
            Assert.That(createdRmvMaterial.GetTexture(TextureType.Mask).Value.Path, Is.EqualTo($"texturePath/{TextureType.Mask}.dds"));
        }

    }
}
