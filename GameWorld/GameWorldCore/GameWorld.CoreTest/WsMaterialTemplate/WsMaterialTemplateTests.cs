using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;

namespace Test.GameWorld.Core.WsMaterialTemplate
{
    public class WsMaterialTemplateTests
    {
        [Test] //Pharaoh weighted
        public void AddTemplateHeader_ReturnCorrectFileNameWeighted()
        {
            var material = new CapabilityMaterialMock(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            var editor = new WsMaterialTemplateEditor(material, GameTypeEnum.Pharaoh);
            var meshName = "testMesh";
            var vertexFormat = UiVertexFormat.Weighted;

            var result = editor.AddTemplateHeader(meshName, vertexFormat, material);
            Assert.That(result, Is.EqualTo("testMesh_weighted_standard_2.xml.material"));
        }
        [Test] //Pharaoh static
        public void AddTemplateHeader_ReturnCorrectFileNameStatic()
        {
            var material = new CapabilityMaterialMock(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            var editor = new WsMaterialTemplateEditor(material, GameTypeEnum.Pharaoh);
            var meshName = "testMesh";
            var vertexFormat = UiVertexFormat.Static;
            var result = editor.AddTemplateHeader(meshName, vertexFormat, material);
            Assert.That(result, Is.EqualTo("testMesh_rigid.xml.material"));
        }
        [Test] //Pharaoh cinematic
        public void AddTemplateHeader_ReturnCorrectFileNameCinematic()
        {
            var material = new CapabilityMaterialMock(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            var editor = new WsMaterialTemplateEditor(material, GameTypeEnum.Pharaoh);
            var meshName = "testMesh";
            var vertexFormat = UiVertexFormat.Cinematic;
            var result = editor.AddTemplateHeader(meshName, vertexFormat, material);
            Assert.That(result, Is.EqualTo("testMesh_weighted_standard_4.xml.material"));
        }
        [Test] //All other games static no alpha
        public void AddTemplateHeader_ReturnCorrectFileNameStaticAllOtherGames()
        {
            var material = new CapabilityMaterialMock(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            var editor = new WsMaterialTemplateEditor(material, GameTypeEnum.Warhammer2);
            var meshName = "testMesh";
            var vertexFormat = UiVertexFormat.Static;
            var result = editor.AddTemplateHeader(meshName, vertexFormat, material);
            Assert.That(result, Is.EqualTo("testMesh_rigid_alpha_off.xml.material"));
        }
        [Test] //All other games weighted no alpha
        public void AddTemplateHeader_ReturnCorrectFileNameWeightedAllOtherGames()
        {
            var material = new CapabilityMaterialMock(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            var editor = new WsMaterialTemplateEditor(material, GameTypeEnum.Warhammer2);
            var meshName = "testMesh";
            var vertexFormat = UiVertexFormat.Weighted;
            var result = editor.AddTemplateHeader(meshName, vertexFormat, material);
            Assert.That(result, Is.EqualTo("testMesh_weighted2_alpha_off.xml.material"));
        }
        [Test] //All other games cinematic no alpha
        public void AddTemplateHeader_ReturnCorrectFileNameCinematicAllOtherGames()
        {
            var material = new CapabilityMaterialMock(CapabilityMaterialsEnum.SpecGlossPbr_Default);
            var editor = new WsMaterialTemplateEditor(material, GameTypeEnum.Warhammer2);
            var meshName = "testMesh";
            var vertexFormat = UiVertexFormat.Cinematic;
            var result = editor.AddTemplateHeader(meshName, vertexFormat, material);
            Assert.That(result, Is.EqualTo("testMesh_weighted4_alpha_off.xml.material"));
        }

    }
}
