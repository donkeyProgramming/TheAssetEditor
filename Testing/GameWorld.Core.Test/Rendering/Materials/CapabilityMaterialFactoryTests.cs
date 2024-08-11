using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Test.Rendering.Materials
{
    internal class CapabilityMaterialFactoryTests
    {

        [Test]
        public void CreateMaterial_Wh3_CreateDefault_FromRmvMaterial()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted, VertexFormat.Static)
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
        public void CreateMaterial_Wh3_CreateDefault_FromWsMaterial()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted, VertexFormat.Static)
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
            var material = abstractMaterialFactory.Create(rmvMaterial, wsMaterial);

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
        public void CreateMaterial_Wh3_CreateEmissive_FromWsMaterial()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted, VertexFormat.Static);

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
        public void CreateMaterial_Rome_Default()
        {
            var rmvMaterial = RmvMaterialHelper
                .Create(ModelMaterialEnum.weighted, VertexFormat.Static)
                .SetAlpha(true)
                .AssignMaterials([TextureType.Normal, TextureType.Gloss]);

            var appSettings = new ApplicationSettingsService(GameTypeEnum.Rome_2);
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
    }


    public static class IRmvMaterialExtentions
    {
        public static IRmvMaterial AssignMaterials(this IRmvMaterial material, TextureType[] texturesToAssign)
        {
            foreach (var texture in texturesToAssign)
                material.SetTexture(texture, $"texturePath/{texture}.dds");

            return material;
        }

        public static IRmvMaterial SetAlpha(this IRmvMaterial material, bool useAlpha)
        {
            material.AlphaMode = useAlpha ? AlphaMode.Transparent: AlphaMode.Opaque;
            return material;
        }
    }

    public static class RmvMaterialHelper
    {
        public static IRmvMaterial Create(ModelMaterialEnum materialEnum, VertexFormat vertexFormat)
        {
            var rmvMaterial = MaterialFactory.Create().CreateMaterial(materialEnum, vertexFormat);
            return rmvMaterial;
        }


    }


    public static class FileHelper
    {
        public static byte[] GetBytes(string path)
        {
            var fullPath = PathHelper.FileInDataFolder(path);   
            var bytes = File.ReadAllBytes(fullPath);
            return bytes; ;
        }
    }

    public static class PathHelper
    {
        public static string FileInDataFolder(string fileName)
        {
            var fullPath = Path.GetFullPath(@"..\..\..\..\..\Data\" + fileName);
            if (File.Exists(fullPath) == false)
                throw new Exception($"Unable to find data file {fileName}");
            return fullPath;
        }

    }
}
