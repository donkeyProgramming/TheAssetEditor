using GameWorld.Core.Rendering.Materials.Serialization;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Test.Rendering.Materials
{
    internal class MaterialToWsMaterialSerializerTests
    {
        [Test]
        public void Te()
        {

            var material = new Core.Rendering.Materials.Shaders.MetalRough.DefaultMaterial(null);


            var saveMock = new Mock<IPackFileSaveService>();
            var materialRepoMock = new Mock<IWsMaterialRepository>();
            var materialRepotReturn = false;
            materialRepoMock
                .Setup(x => x.GetExistingOrAddMaterial(It.IsAny<string>(), It.IsAny<string>(), out materialRepotReturn))
                .Returns((string a, string b, out bool c) => { c = materialRepotReturn; return b; });



            var instance = new MaterialToWsMaterialSerializer(saveMock.Object, materialRepoMock.Object, GameTypeEnum.Warhammer3);
            var filePath = instance.ProsessMaterial("", "", UiVertexFormat.Cinematic, material);




        }
    }
}
