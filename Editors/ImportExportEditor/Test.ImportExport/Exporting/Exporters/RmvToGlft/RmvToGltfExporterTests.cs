using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using GameWorld.Core.Services;
using Moq;
using System.IO;
using Shared.Core.Events;
using Shared.GameFormats.RigidModel.Types;
using Shared.TestUtility;
using Test.TestingUtility.TestUtility;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft
{

    public class RmvToGltfExporterTests
    {
        private readonly string _inputPackFileKarl = PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack");
        private readonly string _rmvFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2";
        private readonly string _wsModelFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel";
        private readonly string _inputPackFileRome = PathHelper.GetDataFolder("Data\\Rome_Man_And_Shield_Pack");

        [Test]
        public void ExportsTrackedVariantMeshDefinitionAsOneComposedScene()
        {
            var pfs = PackFileSerivceTestHelper.Create(_inputPackFileRome);
            var definition = pfs.FindFile(@"variantmeshes\_variantmodels\man\shield\celtic_oval_patterns.variantmeshdefinition");
            Assert.That(definition, Is.Not.Null);

            var normalExporter = new Mock<IDdsToNormalPngExporter>();
            var materialExporter = new Mock<IDdsToMaterialPngExporter>();
            var eventHub = new Mock<IGlobalEventHub>();
            var skeletonLookup = new SkeletonAnimationLookUpHelper(pfs, eventHub.Object);
            var compositionResolver = new VariantMeshCompositionResolver(pfs, new ModelAssetResolver(pfs));
            var sceneSaver = new TestGltfSceneSaver();
            var exporter = new RmvToGltfExporter(
                sceneSaver,
                new GltfMeshBuilder(),
                new GltfTextureHandler(normalExporter.Object, materialExporter.Object, pfs),
                new GltfSkeletonBuilder(pfs),
                new GltfAnimationBuilder(pfs),
                skeletonLookup,
                new ModelAssetResolver(pfs),
                compositionResolver);

            exporter.Export(new RmvToGltfExporterSettings(
                definition!,
                [],
                Path.Combine(Path.GetTempPath(), "asset-editor-vmd.glb"),
                false,
                false,
                false,
                false,
                false));

            Assert.That(sceneSaver.IsSaveCalled, Is.True);
            Assert.That(sceneSaver.ModelRoot, Is.Not.Null);
            Assert.That(sceneSaver.ModelRoot!.LogicalMeshes, Is.Not.Empty);
            Assert.That(sceneSaver.ModelRoot.LogicalNodes.Any(x => x.Name.StartsWith("vmd_part_", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void ExportsTrackedVariantMeshDefinitionToReloadableGlb()
        {
            var pfs = PackFileSerivceTestHelper.Create(_inputPackFileRome);
            var definition = pfs.FindFile(@"variantmeshes\_variantmodels\man\shield\celtic_oval_patterns.variantmeshdefinition");
            Assert.That(definition, Is.Not.Null);

            var outputPath = Path.Combine(Path.GetTempPath(), $"asset-editor-vmd-{Guid.NewGuid():N}.glb");
            var normalExporter = new Mock<IDdsToNormalPngExporter>();
            var materialExporter = new Mock<IDdsToMaterialPngExporter>();
            var eventHub = new Mock<IGlobalEventHub>();
            var skeletonLookup = new SkeletonAnimationLookUpHelper(pfs, eventHub.Object);
            var compositionResolver = new VariantMeshCompositionResolver(pfs, new ModelAssetResolver(pfs));
            var exporter = new RmvToGltfExporter(
                new FileGltfSceneSaver(),
                new GltfMeshBuilder(),
                new GltfTextureHandler(normalExporter.Object, materialExporter.Object, pfs),
                new GltfSkeletonBuilder(pfs),
                new GltfAnimationBuilder(pfs),
                skeletonLookup,
                new ModelAssetResolver(pfs),
                compositionResolver);

            try
            {
                exporter.Export(new RmvToGltfExporterSettings(
                    definition!,
                    [],
                    outputPath,
                    false,
                    false,
                    false,
                    false,
                    false));

                Assert.That(File.Exists(outputPath), Is.True);
                ModelRoot.Validate(outputPath);
                var reloaded = ModelRoot.Load(outputPath);
                Assert.That(reloaded.LogicalMeshes, Is.Not.Empty);
                Assert.That(reloaded.LogicalNodes.Any(x => x.Name.StartsWith("vmd_part_", StringComparison.Ordinal)), Is.True);
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        [Test]
        public void Test()
        {
            // Arrange 
            var pfs = PackFileSerivceTestHelper.Create(_inputPackFileKarl);
            var meshBuilder = new GltfMeshBuilder();
            var normalExporter = new Mock<IDdsToNormalPngExporter>();
            var materialExporter = new Mock<IDdsToMaterialPngExporter>();
            var eventHub = new Mock<IGlobalEventHub>();
            var skeletontonLookupHelper = new SkeletonAnimationLookUpHelper(pfs, eventHub.Object);            
            var skeletontonBuilder = new GltfSkeletonBuilder(pfs);
            var animationBuilder = new GltfAnimationBuilder(pfs);
            var textureHandler = new GltfTextureHandler(normalExporter.Object, materialExporter.Object);
            var sceneSaver = new TestGltfSceneSaver();

            // Act
            var mesh = pfs.FindFile(_rmvFilePathKarl);
            var exporter = new RmvToGltfExporter(
                sceneSaver,
                meshBuilder,
                textureHandler,
                skeletontonBuilder,
                animationBuilder,
                skeletontonLookupHelper,
                new ModelAssetResolver(pfs));
            var settings = new RmvToGltfExporterSettings(mesh!, [], @"C:\test\myExport.glb", true, true, true, true, true);
            exporter.Export(settings);

            // Assert
            Assert.That(sceneSaver.IsSaveCalled, Is.True);
            Assert.That(sceneSaver.FullSystemPath, Does.EndWith(".glb"));

            Assert.That(sceneSaver.ModelRoot, Is.Not.Null);
            Assert.That(sceneSaver.ModelRoot!.LogicalMaterials.Count(), Is.EqualTo(4));

            // Validate a materials and textues. Texture paths are not easy to validate, as gltf check file exists on disk 
            // which we do not want
            //sceneSaver.ModelRoot!.LogicalMaterials[0].Channels

            Assert.That(sceneSaver.ModelRoot!.LogicalMeshes.Count(), Is.EqualTo(4));
            // Validate a mesh

            // Validate skeleton

        }

        [Test]
        public void ResolvesExplicitKarlWsModelWithAllLod0Parts()
        {
            var pfs = PackFileSerivceTestHelper.Create(_inputPackFileKarl);
            var wsModel = pfs.FindFile(_wsModelFilePathKarl);
            Assert.That(wsModel, Is.Not.Null);

            var resolved = new ModelAssetResolver(pfs).Resolve(wsModel!);

            Assert.That(resolved.InputFile, Is.SameAs(wsModel));
            Assert.That(resolved.UsesWsModel, Is.True);
            Assert.That(resolved.GeometryFile.Name, Does.EndWith("emp_karl_franz.rigid_model_v2"));
            Assert.That(resolved.FirstLod, Has.Count.EqualTo(4));
            Assert.That(resolved.FirstLod.All(x => x.Material.UsesWsModelMaterial), Is.True);
        }

        [Test]
        public void ResolvesNestedSiblingWsModelFromRmvInputPath()
        {
            var pfs = PackFileSerivceTestHelper.Create(_inputPackFileKarl);
            var rmv = pfs.FindFile(_rmvFilePathKarl);
            Assert.That(rmv, Is.Not.Null);

            var resolved = new ModelAssetResolver(pfs).Resolve(rmv!);

            Assert.That(resolved.WsModelFile, Is.Not.Null);
            Assert.That(pfs.GetFullPath(resolved.WsModelFile!), Does.EndWith("emp_karl_franz.wsmodel"));
            Assert.That(resolved.FirstLod, Has.Count.EqualTo(4));
            Assert.That(resolved.FirstLod.All(x => x.Material.UsesWsModelMaterial), Is.True);
        }

        [Test]
        public void TextureExportUsesResolvedWsModelTexturePaths()
        {
            var pfs = PackFileSerivceTestHelper.Create(_inputPackFileKarl);
            var wsModel = pfs.FindFile(_wsModelFilePathKarl)!;
            var asset = new ModelAssetResolver(pfs).Resolve(wsModel);
            var selectedTexture = asset.MaterialsByLod[0][1].GetTexture(TextureType.BaseColour);
            Assert.That(selectedTexture, Is.EqualTo("VariantMeshes/wh_variantmodels/hu1/emp/emp_karl_franz/tex/emp_karl_franz_body_01_base_colour.dds"));

            var materialPaths = new List<string>();
            var normalPaths = new List<string>();
            var normalExporter = new Mock<IDdsToNormalPngExporter>();
            normalExporter
                .Setup(x => x.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback<string, string, bool>((path, _, _) => normalPaths.Add(path))
                .Returns((string)null!);
            var materialExporter = new Mock<IDdsToMaterialPngExporter>();
            materialExporter
                .Setup(x => x.Export(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback<string, string, bool>((path, _, _) => materialPaths.Add(path))
                .Returns((string)null!);

            var handler = new GltfTextureHandler(normalExporter.Object, materialExporter.Object, pfs);
            var settings = new RmvToGltfExporterSettings(
                wsModel,
                [],
                Path.Combine(Path.GetTempPath(), "asset-editor-ws-test.glb"),
                true,
                false,
                false,
                false,
                false);

            handler.HandleTextures(asset, settings);

            Assert.That(materialPaths, Does.Contain(selectedTexture));
            Assert.That(normalPaths, Does.Contain("VariantMeshes/wh_variantmodels/hu1/emp/emp_karl_franz/tex/emp_karl_franz_body_01_normal.dds"));
        }
    }

    internal sealed class FileGltfSceneSaver : IGltfSceneSaver
    {
        public void Save(ModelRoot modelRoot, string fullSystemPath)
            => modelRoot.Save(fullSystemPath);
    }
}
