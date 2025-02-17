using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Rendering.Materials.Shaders.SpecGloss;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Test.TestingUtility.TestUtility;

namespace GameWorld.Core.Test.Rendering.Shaders.SpecGloss
{
    internal class DecalAndDirtMaterialTests
    {

        [Test]
        public void CreateDecalAndDirt_FromRmvMaterial()
        {
            // Arrange
            var settings = new ApplicationSettingsService(GameTypeEnum.Attila);
            var materialFactory = new CapabilityMaterialFactory(settings, null);

            var path = PathHelper.GetDataFile("Rome_Man_And_Shield_Pack//variantmeshes//_variantmodels//man//shield//celtic_oval_shield_a.rigid_model_v2");
            var packFile = PackFile.CreateFromFileSystem("mymodel.rigid_model_v2", path);
            var rmvFile = ModelFactory.Create().Load(packFile.DataSource.ReadData());

            // Act
            var material = materialFactory.Create(rmvFile.ModelList[0][0].Material, null);

            // Assert
            Assert.That(material.Type, Is.EqualTo(CapabilityMaterialsEnum.SpecGlossPbr_Advanced));

            var typedMaterial = material as AdvancedRmvMaterial;
            Assert.That(typedMaterial, Is.Not.Null);

            var capability = typedMaterial.GetCapability<AdvancedMaterialCapability>();
            Assert.That(capability.UvScale, Is.EqualTo(new Vector2(2, 2)));
            Assert.That(capability.TextureTransform, Is.EqualTo(new Vector4(0.03337255f, 0.23629414f, 0.39431375f, 0.95299995f)));

            Assert.That(capability.DecalMask.TexturePath, Is.EqualTo("variantmeshes/_variantmodels/man/shield/tex/celtic_oval_shield_decalmask.dds"));
            Assert.That(capability.DirtMask.TexturePath, Is.EqualTo("variantmeshes/_variantmodels/man/shield/tex/test_black.dds"));
            Assert.That(capability.DirtMap.TexturePath, Is.EqualTo("variantmeshes/_variantmodels/man/shield/tex/default_decaldirtmap.dds"));
        }

        [Test]
        public void CreateDirt_FromRmvMaterial()
        {
            // Arrange
            var settings = new ApplicationSettingsService(GameTypeEnum.Attila);
            var materialFactory = new CapabilityMaterialFactory(settings, null);

            var path = PathHelper.GetDataFile("Rome_Man_And_Shield_Pack//variantmeshes//_variantmodels//man//helmets//carthaginian_pylos.rigid_model_v2");
            var packFile = PackFile.CreateFromFileSystem("mymodel.rigid_model_v2", path);
            var rmvFile = ModelFactory.Create().Load(packFile.DataSource.ReadData());

            // Act
            var material = materialFactory.Create(rmvFile.ModelList[0][0].Material, null);

            // Assert
            Assert.That(material.Type, Is.EqualTo(CapabilityMaterialsEnum.SpecGlossPbr_Advanced));

            var typedMaterial = material as AdvancedRmvMaterial;
            Assert.That(typedMaterial, Is.Not.Null);

            var capability = typedMaterial.GetCapability<AdvancedMaterialCapability>();
            Assert.That(capability.UvScale, Is.EqualTo(new Vector2(2, 2)));
            Assert.That(capability.TextureTransform, Is.EqualTo(new Vector4(0, 0, 0, 0)));

            Assert.That(capability.DecalMask.TexturePath, Is.EqualTo(string.Empty));
            Assert.That(capability.DirtMask.TexturePath, Is.EqualTo("variantmeshes/_variantmodels/man/helmets/tex/carthaginian_pylos_decaldirtmask.dds"));
            Assert.That(capability.DirtMap.TexturePath, Is.EqualTo("variantmeshes/_variantmodels/man/helmets/tex/default_decaldirtmap.dds"));
        }

        [Test]
        public void DecalAndDirt_GenerateRmvMaterial()
        {
            // Arrange
            var path = PathHelper.GetDataFile("Rome_Man_And_Shield_Pack//variantmeshes//_variantmodels//man//shield//celtic_oval_shield_a.rigid_model_v2");
            var packFile = PackFile.CreateFromFileSystem("mymodel.rigid_model_v2", path);
            var rmvFile = ModelFactory.Create().Load(packFile.DataSource.ReadData());

            var settings = new ApplicationSettingsService(GameTypeEnum.Attila);
            var materialFactory = new CapabilityMaterialFactory(settings, null);
            var material = materialFactory.Create(rmvFile.ModelList[0][0].Material, null);
            material.GetCapability<SpecGlossCapability>().UseAlpha = false;
            material.GetCapability<AdvancedMaterialCapability>().UvScale = new Vector2(2.5f, 3);
            material.GetCapability<AdvancedMaterialCapability>().UseDecal = true;
            material.GetCapability<AdvancedMaterialCapability>().UseDirt = true;

            // Act
            var serializer = new MaterialToRmvSerializer();
            var createdRmvMaterial = serializer.CreateMaterialFromCapabilityMaterial(material);
            createdRmvMaterial.UpdateInternalState(UiVertexFormat.Weighted);
            var typedMaterial = createdRmvMaterial as WeightedMaterial;
   
            // Assert
            Assert.That(typedMaterial, Is.Not.Null);
            Assert.That(typedMaterial.MaterialId, Is.EqualTo(ModelMaterialEnum.weighted_decal_dirtmap));

            Assert.That(typedMaterial.FloatParams.Values.Count, Is.EqualTo(2));
            Assert.That(typedMaterial.FloatParams.Values[0], Is.EqualTo(((int)WeightedParamterIds.FloatParams_UvScaleX, 2.5f)));
            Assert.That(typedMaterial.FloatParams.Values[1], Is.EqualTo(((int)WeightedParamterIds.FloatParams_UvScaleY, 3)));

            Assert.That(typedMaterial.IntParams.Values.Count, Is.EqualTo(3));
            Assert.That(typedMaterial.IntParams.Get(WeightedParamterIds.IntParams_Alpha_index), Is.EqualTo(0));
            Assert.That(typedMaterial.IntParams.Get(WeightedParamterIds.IntParams_Decal_index), Is.EqualTo(1));
            Assert.That(typedMaterial.IntParams.Get(WeightedParamterIds.IntParams_Dirt_index), Is.EqualTo(1));

            Assert.That(typedMaterial.Vec4Params.Values.Count, Is.EqualTo(1));

            Assert.That(typedMaterial.TexturesParams[5].TexureType, Is.EqualTo(TextureType.Decal_dirtmap));
            Assert.That(typedMaterial.TexturesParams[5].Path, Is.EqualTo("variantmeshes/_variantmodels/man/shield/tex/default_decaldirtmap.dds"));

            Assert.That(typedMaterial.TexturesParams[6].TexureType, Is.EqualTo(TextureType.Decal_dirtmask));
            Assert.That(typedMaterial.TexturesParams[6].Path, Is.EqualTo("variantmeshes/_variantmodels/man/shield/tex/test_black.dds"));

            Assert.That(typedMaterial.TexturesParams[7].TexureType, Is.EqualTo(TextureType.Decal_mask));
            Assert.That(typedMaterial.TexturesParams[7].Path, Is.EqualTo("variantmeshes/_variantmodels/man/shield/tex/celtic_oval_shield_decalmask.dds"));
        }
    }
}
