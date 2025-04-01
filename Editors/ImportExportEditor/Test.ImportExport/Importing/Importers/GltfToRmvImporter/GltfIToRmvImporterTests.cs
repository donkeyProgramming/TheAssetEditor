using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.TestUtility;
using Test.ImportExport.Exporting.Exporters.RmvToGlft;
using Test.TestingUtility.TestUtility;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest
{
    class TestData
    {
        public static readonly string InputGtlfFile = PathHelper.GetDataFile(@"Karl_Gltf_Resaved_From_Blender\karl_franz_imported_and_saved_to_blendfile_and_exported_to_gltf_from_blender.gltf");
        public static readonly string InputPack = PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack");

        public static class GltfExpected
        {
            public const int logicalMaterials = 4;
            public const int nodes = 78;
            public const int skinCount = 1;
            public const int textureCount = 12;
            public const int logicalMeshes = 4;
            public const int primtivesMesh0 = 1;
            public const int primtivesMesh0IndexCount = 25539;
        }

        public static class Rmv2Expected
        {
            public const string skeletonName = "humanoid01";            
            public const int lodCount = 1;
            public const int lod0MeshCount = 4;
            public const int Lod0Mesh0IndexCount = 25539;
            public const int Lod0Mesh0VertexCount = 6397;
            public const int Lod0Mesh0TextureCount = 3;
        }
    }

    // Tests Full Import (Importer.Import())
    class GltfToRmv2ImporterTest
    {
        [Test]
        public void Test()
        {
            // Arrange 
            var pfs = PackFileSerivceTestHelper.Create(TestData.InputPack);

            var meshBuilder = new GltfMeshBuilder();
            var eventHub = new Mock<IGlobalEventHub>();
            var skeletontonLookupHelper = new SkeletonAnimationLookUpHelper(pfs, eventHub.Object);
            var skeletontonBuilder = new GltfSkeletonBuilder(pfs);
            var animationBuilder = new GltfAnimationBuilder(pfs);
            var sceneSaver = new TestGltfSceneSaver();
            var standardDialog = new Mock<IStandardDialogs>();
            var sceneLoader = new GltfSceneLoader(standardDialog.Object);
            var materialBuilder = new RmvMaterialBuilder(pfs, standardDialog.Object);
            var importer = new GltfImporter(pfs, standardDialog.Object, skeletontonLookupHelper, materialBuilder);
            var packFileContainer = new PackFileContainer("new");
            var settings = new GltfImporterSettings(TestData.InputGtlfFile, "skeletons", packFileContainer, Shared.Core.Settings.GameTypeEnum.Warhammer3, true, true, true, true, true, 20.0f, true);

            // Act
            importer.Import(settings);
            var rmv2FileName = @$"{settings.DestinationPackPath}\{Path.GetFileNameWithoutExtension(settings.InputGltfFile)}.rigid_model_v2".ToLower();
            var isPackFileAddedToContainer = packFileContainer.FileList.TryGetValue(rmv2FileName, out var packFile);
                         
            //  Assert                        
            Assert.That(isPackFileAddedToContainer, Is.EqualTo(true));            
            var rmv2File = ModelFactory.Create().Load(packFile!.DataSource!.ReadData());
            Assert.That(rmv2File, Is.Not.Null);
            Assert.That(rmv2File!.LodHeaders.Length, Is.EqualTo(TestData.Rmv2Expected.lodCount));            
            Assert.That(rmv2File!.LodHeaders[0].MeshCount, Is.EqualTo(TestData.Rmv2Expected.lod0MeshCount));
            
            Assert.That(rmv2File!.ModelList.Length, Is.EqualTo(TestData.Rmv2Expected.lodCount));
            Assert.That(rmv2File!.ModelList[0].Length, Is.EqualTo(TestData.Rmv2Expected.lod0MeshCount));
            
            Assert.That(rmv2File!.ModelList[0][0]!.Material!.GetAllTextures().Count(), Is.EqualTo(TestData.Rmv2Expected.Lod0Mesh0TextureCount));
            Assert.That(rmv2File!.ModelList[0][0]!.Mesh.IndexList.Length, Is.EqualTo(TestData.Rmv2Expected.Lod0Mesh0IndexCount));
            Assert.That(rmv2File!.ModelList[0][0]!.Mesh.VertexList.Length, Is.EqualTo(TestData.Rmv2Expected.Lod0Mesh0VertexCount));
        }
    }

    // Tests some Components of Importer pipeline
    class GltfToRmv2ImporterComponentTest
    {
        [Test]
        public void TestGltfApiLoad()
        {
            // Arrange 
            var pfs = PackFileSerivceTestHelper.Create(TestData.InputPack);

            var meshBuilder = new GltfMeshBuilder();
            var eventHub = new Mock<IGlobalEventHub>();
            var standardDialog = new Mock<IStandardDialogs>();
            var skeletontonLookupHelper = new SkeletonAnimationLookUpHelper(pfs, eventHub.Object);
            var skeletontonBuilder = new GltfSkeletonBuilder(pfs);
            var animationBuilder = new GltfAnimationBuilder(pfs);
            var materialBuilder = new RmvMaterialBuilder(pfs, standardDialog.Object);
            var sceneLoader = new GltfSceneLoader(standardDialog.Object);
            var skeletonFile = skeletontonLookupHelper.GetSkeletonFileFromName(TestData.Rmv2Expected.skeletonName);
            var packFileContainer = new PackFileContainer("new");
            var settings = new GltfImporterSettings(TestData.InputGtlfFile, "skeletons", packFileContainer, Shared.Core.Settings.GameTypeEnum.Warhammer3, true, true, true, true, true, 20.0f, true);

            // Act....          
            var modelRoot = sceneLoader.Load(settings);

            // Assert            
            // Assert API correct load
            Assert.That(modelRoot, Is.Not.Null);
            Assert.That(modelRoot.LogicalMaterials!.Count(), Is.EqualTo(TestData.GltfExpected.logicalMaterials));
            Assert.That(modelRoot.LogicalNodes!.Count(), Is.EqualTo(TestData.GltfExpected.nodes));
            Assert.That(modelRoot.LogicalSkins!.Count(), Is.EqualTo(TestData.GltfExpected.skinCount));
            Assert.That(modelRoot.LogicalTextures!.Count(), Is.EqualTo(TestData.GltfExpected.textureCount));
            Assert.That(modelRoot.LogicalMeshes!.Count(), Is.EqualTo(TestData.GltfExpected.logicalMeshes));
            Assert.That(modelRoot.LogicalMeshes[0].Primitives!.Count(), Is.EqualTo(TestData.GltfExpected.primtivesMesh0));
            Assert.That(modelRoot.LogicalMeshes[0].Primitives[0].GetIndices().Count(), Is.EqualTo(TestData.GltfExpected.primtivesMesh0IndexCount));

        }

        [Test]
        public void TestRmvMesgBuilder()
        {
            //  Arrange *
            var pfs = PackFileSerivceTestHelper.Create(TestData.InputPack);

            var meshBuilder = new GltfMeshBuilder();
            var eventHub = new Mock<IGlobalEventHub>();
            var standardDialog = new Mock<IStandardDialogs>();
            var skeletontonLookupHelper = new SkeletonAnimationLookUpHelper(pfs, eventHub.Object);
            var skeletontonBuilder = new GltfSkeletonBuilder(pfs);
            var animationBuilder = new GltfAnimationBuilder(pfs);
            var materialBuilder = new RmvMaterialBuilder(pfs, standardDialog.Object);
            var sceneLoader = new GltfSceneLoader(standardDialog.Object);
            var skeletonFile = skeletontonLookupHelper.GetSkeletonFileFromName(TestData.Rmv2Expected.skeletonName);
            var packFileContainer = new PackFileContainer("new");
            var settings = new GltfImporterSettings(TestData.InputGtlfFile, "skeletons", packFileContainer, Shared.Core.Settings.GameTypeEnum.Warhammer3, true, true, true, true, true, 20.0f, true);

            //  Act 
            var modelRoot = sceneLoader.Load(settings);
            var rmv2Mesh = RmvMeshBuilder.Build(settings, modelRoot, skeletonFile, TestData.Rmv2Expected.skeletonName.ToLower());

            // Assert             
            Assert.That(skeletonFile, Is.Not.Null);
            Assert.That(rmv2Mesh, Is.Not.Null);
            Assert.That(rmv2Mesh.Header.SkeletonName, Is.EqualTo(TestData.Rmv2Expected.skeletonName));
            Assert.That(rmv2Mesh.ModelList, Is.Not.Null);
            Assert.That(rmv2Mesh.ModelList.Length, Is.EqualTo(TestData.Rmv2Expected.lodCount));
            Assert.That(rmv2Mesh.ModelList[0].Length, Is.EqualTo(TestData.Rmv2Expected.lod0MeshCount));
            Assert.That(rmv2Mesh.ModelList[0][0].Mesh, Is.Not.Null);
            Assert.That(rmv2Mesh.ModelList[0][0].Material, Is.Not.Null);
            Assert.That(rmv2Mesh.ModelList[0][0].Material.MaterialId, Is.EqualTo(ModelMaterialEnum.weighted));
            Assert.That(rmv2Mesh.ModelList[0][0].Material.BinaryVertexFormat, Is.EqualTo(VertexFormat.Cinematic));
            Assert.That(rmv2Mesh.ModelList[0][0].Mesh.IndexList, Is.Not.Null);
            Assert.That(rmv2Mesh.ModelList[0][0].Mesh.IndexList.Length, Is.EqualTo(TestData.Rmv2Expected.Lod0Mesh0IndexCount));
            Assert.That(rmv2Mesh.ModelList[0][0].Mesh.VertexList.Length, Is.EqualTo(TestData.Rmv2Expected.Lod0Mesh0VertexCount));
        }
    }
}
