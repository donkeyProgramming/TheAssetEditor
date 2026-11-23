using Editors.ImportExport.Misc;

namespace Test.ImportExport.Misc;

public class FileExtensionHelperTests
{
    [TestCase("model.gltf")]
    [TestCase("MODEL.GLTF")]
    [TestCase("model.glb")]
    [TestCase("MODEL.GLB")]
    public void IsGltfFile_AcceptsSupportedContainers(string fileName)
    {
        Assert.That(FileExtensionHelper.IsGltfFile(fileName), Is.True);
    }

    [TestCase("model.gltf.bak")]
    [TestCase("model.glb.txt")]
    [TestCase("model.fbx")]
    public void IsGltfFile_RejectsOtherExtensions(string fileName)
    {
        Assert.That(FileExtensionHelper.IsGltfFile(fileName), Is.False);
    }
}
