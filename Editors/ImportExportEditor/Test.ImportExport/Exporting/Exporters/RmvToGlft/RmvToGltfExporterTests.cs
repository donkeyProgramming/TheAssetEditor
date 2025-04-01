using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.Events;
using Shared.TestUtility;
using Test.TestingUtility.TestUtility;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft
{

    public class RmvToGltfExporterTests
    {
        private readonly string _inputPackFileKarl = PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack");
        private readonly string _rmvFilePathKarl = @"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2";

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
            var exporter = new RmvToGltfExporter(sceneSaver, meshBuilder, textureHandler, skeletontonBuilder, animationBuilder, skeletontonLookupHelper);
            var settings = new RmvToGltfExporterSettings(mesh!, [], @"C:\test\myExport.gltf", true, true, true, true);
            exporter.Export(settings);

            // Assert
            Assert.That(sceneSaver.IsSaveCalled, Is.True);

            Assert.That(sceneSaver.ModelRoot, Is.Not.Null);
            Assert.That(sceneSaver.ModelRoot!.LogicalMaterials.Count(), Is.EqualTo(4));

            // Validate a materials and textues. Texture paths are not easy to validate, as gltf check file exists on disk 
            // which we do not want
            //sceneSaver.ModelRoot!.LogicalMaterials[0].Channels

            Assert.That(sceneSaver.ModelRoot!.LogicalMeshes.Count(), Is.EqualTo(4));
            // Validate a mesh

            // Validate skeleton

        }
    }
}
