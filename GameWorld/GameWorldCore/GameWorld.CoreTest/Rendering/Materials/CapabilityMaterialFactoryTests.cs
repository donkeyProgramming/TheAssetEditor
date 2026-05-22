using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Test.TestUtility;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Rendering.Materials
{
    internal class CapabilityMaterialFactoryTests
    {

        [Test]
        public void Create_FromRmv_Wh3_Default()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted)
                .SetAlpha(true)
                .AssignMaterials([TextureType.Normal, TextureType.MaterialMap]);

            var appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            var abstractMaterialFactory = new CapabilityMaterialFactory(appSettings, null);
            var material = abstractMaterialFactory.Create(rmvMaterial, null);

            Assert.That(material, Is.TypeOf<Core.Rendering.Materials.Shaders.MetalRough.DefaultMaterial>());

            var defaultCapabiliy = material.TryGetCapability<MetalRoughCapability>();
            Assert.That(defaultCapabiliy, Is.Not.Null);

            Assert.That(defaultCapabiliy.MaterialMap.TexturePath, Is.EqualTo($"texturePath/{TextureType.MaterialMap}.dds"));
            Assert.That(defaultCapabiliy.MaterialMap.UseTexture, Is.True);

            Assert.That(defaultCapabiliy.NormalMap.TexturePath, Is.EqualTo($"texturePath/{TextureType.Normal}.dds"));
            Assert.That(defaultCapabiliy.NormalMap.UseTexture, Is.True);

            Assert.That(defaultCapabiliy.BaseColour.TexturePath, Is.EqualTo(""));
            Assert.That(defaultCapabiliy.BaseColour.UseTexture, Is.False);

            Assert.That(defaultCapabiliy.UseAlpha, Is.True);
        }

        [Test]
        public void Create_FromWs_Wh3_Default()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted)
                .SetAlpha(true)
                .AssignMaterials([TextureType.Normal, TextureType.MaterialMap]);

            var wsMaterial = new WsModelMaterialFile()
            {
                Alpha = false,
                Name = "cth_celestial_general_body_01_weighted4_alpha_off.xml",
                ShaderPath = "shaders/weighted4_character.xml.shader",
                Textures = new()
                {
                    {TextureType.Normal, $"texturePath/wsmodel/{TextureType.Normal}.dds"},
                    {TextureType.MaterialMap, $"texturePath/wsmodel/{TextureType.MaterialMap}.dds"}
                }
            };

            var appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            var abstractMaterialFactory = new CapabilityMaterialFactory(appSettings, null);
            var material = abstractMaterialFactory.Create(null, wsMaterial);

            Assert.That(material, Is.TypeOf<Core.Rendering.Materials.Shaders.MetalRough.DefaultMaterial>());

            var defaultCapabiliy = material.TryGetCapability<MetalRoughCapability>();
            Assert.That(defaultCapabiliy, Is.Not.Null);

            Assert.That(defaultCapabiliy.MaterialMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.MaterialMap}.dds"));
            Assert.That(defaultCapabiliy.MaterialMap.UseTexture, Is.True);

            Assert.That(defaultCapabiliy.NormalMap.TexturePath, Is.EqualTo($"texturePath/wsmodel/{TextureType.Normal}.dds"));
            Assert.That(defaultCapabiliy.NormalMap.UseTexture, Is.True);

            Assert.That(defaultCapabiliy.BaseColour.TexturePath, Is.EqualTo(""));
            Assert.That(defaultCapabiliy.BaseColour.UseTexture, Is.False);

            Assert.That(defaultCapabiliy.UseAlpha, Is.False);
        }

        [Test]
        public void Create_FromWs_Wh3_Emissive()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted);

            var wsMaterial = new WsModelMaterialFile()
            {
                Name = "cth_celestial_general_body_01_weighted4_alpha_off.xml",
                ShaderPath = "shaders/weighted4_character_emissive.xml.shader",
            };

            var appSettings = new ApplicationSettingsService(GameTypeEnum.Warhammer3);
            var abstractMaterialFactory = new CapabilityMaterialFactory(appSettings, null);
            var material = abstractMaterialFactory.Create(rmvMaterial, wsMaterial);

            Assert.That(material, Is.TypeOf<Core.Rendering.Materials.Shaders.MetalRough.EmissiveMaterial>());

            var emissiveCapability = material.TryGetCapability<EmissiveCapability>();
            Assert.That(emissiveCapability, Is.Not.Null);
        }

        [Test]
        public void Create_FromRmv_Rome_Default()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted)
                .SetAlpha(true)
                .AssignMaterials([TextureType.Normal, TextureType.Gloss]);

            var appSettings = new ApplicationSettingsService(GameTypeEnum.Rome2);
            var abstractMaterialFactory = new CapabilityMaterialFactory(appSettings, null);
            var material = abstractMaterialFactory.Create(rmvMaterial, null);

            Assert.That(material, Is.TypeOf<Core.Rendering.Materials.Shaders.SpecGloss.DefaultMaterial>());

            var defaultCapabiliy = material.TryGetCapability<SpecGlossCapability>();
            Assert.That(defaultCapabiliy, Is.Not.Null);

            Assert.That(defaultCapabiliy.GlossMap.TexturePath, Is.EqualTo($"texturePath/{TextureType.Gloss}.dds"));
            Assert.That(defaultCapabiliy.GlossMap.UseTexture, Is.True);

            Assert.That(defaultCapabiliy.NormalMap.TexturePath, Is.EqualTo($"texturePath/{TextureType.Normal}.dds"));
            Assert.That(defaultCapabiliy.NormalMap.UseTexture, Is.True);

            Assert.That(defaultCapabiliy.DiffuseMap.TexturePath, Is.EqualTo(""));
            Assert.That(defaultCapabiliy.DiffuseMap.UseTexture, Is.False);

            Assert.That(defaultCapabiliy.UseAlpha, Is.True);
        }

        [Test]
        public void Create_FromRmv_Rome_DirtAndDecal()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted_decal_dirtmap)
                .SetAlpha(true)
                .SetDecalAndDirt(true, true)
                .AssignMaterials([TextureType.Diffuse, TextureType.Decal_dirtmap, TextureType.Decal_mask, TextureType.Decal_dirtmask]);
            
            var appSettings = new ApplicationSettingsService(GameTypeEnum.Rome2);
            var abstractMaterialFactory = new CapabilityMaterialFactory(appSettings, null);
            var material = abstractMaterialFactory.Create(rmvMaterial, null);
            
            Assert.That(material, Is.TypeOf<Core.Rendering.Materials.Shaders.SpecGloss.AdvancedRmvMaterial>());
            
            var specGlossCap = material.TryGetCapability<SpecGlossCapability>();
            Assert.That(specGlossCap, Is.Not.Null);
            
            var dirtCap = material.TryGetCapability<AdvancedMaterialCapability>();
            Assert.That(dirtCap, Is.Not.Null);
        }
    }
}
