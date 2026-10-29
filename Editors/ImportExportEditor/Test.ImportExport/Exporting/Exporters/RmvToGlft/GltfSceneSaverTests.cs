using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using System.IO;
using Moq;
using Shared.Core.Services;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft;

public class GltfSceneSaverTests
{
    [Test]
    public void SavesReloadableGlbWhenOutputUsesGlbExtension()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"asset-editor-{Guid.NewGuid():N}.glb");
        var dialogs = new Mock<IStandardDialogs>();

        try
        {
            var model = ModelRoot.CreateModel();
            model.UseScene("default");
            var saver = new GltfSceneSaver(dialogs.Object);

            saver.Save(model, outputPath);

            Assert.That(File.Exists(outputPath), Is.True);
            ModelRoot.Validate(outputPath);
            var reloaded = ModelRoot.Load(outputPath);
            Assert.That(reloaded, Is.Not.Null);
            dialogs.Verify(x => x.ShowExceptionWindow(It.IsAny<Exception>()), Times.Never);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Test]
    public void RetainsGeneratedTexturePngForGltfOutput()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"asset-editor-gltf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "model.gltf");
        var texturePath = Path.Combine(outputDirectory, "body.png");
        File.WriteAllBytes(texturePath, [1, 2, 3]);

        try
        {
            var saver = new GltfSceneSaver(new Mock<IStandardDialogs>().Object);
            var model = ModelRoot.CreateModel();
            model.UseScene("default");
            saver.Save(model, outputPath, [texturePath]);

            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(File.Exists(texturePath), Is.True);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Test]
    public void RemovesOnlyReferencedTexturePngsAfterSuccessfulGlbSave()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"asset-editor-glb-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "model.glb");
        var texturePath = Path.Combine(outputDirectory, "body.png");
        var maskPath = Path.Combine(outputDirectory, "body_mask.png");
        var auxiliaryPath = Path.Combine(outputDirectory, "body_displacement.png");
        File.WriteAllBytes(texturePath, [1, 2, 3]);
        File.WriteAllBytes(maskPath, [4, 5, 6]);
        File.WriteAllBytes(auxiliaryPath, [7, 8, 9]);

        try
        {
            var saver = new GltfSceneSaver(new Mock<IStandardDialogs>().Object);
            var model = ModelRoot.CreateModel();
            model.UseScene("default");
            saver.Save(model, outputPath, [texturePath, texturePath]);

            Assert.That(File.Exists(outputPath), Is.True);
            ModelRoot.Validate(outputPath);
            Assert.That(File.Exists(texturePath), Is.False);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(auxiliaryPath), Is.True);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Test]
    public void RetainsGeneratedTexturePngWhenGlbSaveFails()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"asset-editor-glb-failure-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "missing", "model.glb");
        var texturePath = Path.Combine(outputDirectory, "body.png");
        File.WriteAllBytes(texturePath, [1, 2, 3]);
        var dialogs = new Mock<IStandardDialogs>();

        try
        {
            var saver = new GltfSceneSaver(dialogs.Object);
            var model = ModelRoot.CreateModel();
            model.UseScene("default");
            saver.Save(model, outputPath, [texturePath]);

            Assert.That(File.Exists(texturePath), Is.True);
            dialogs.Verify(x => x.ShowExceptionWindow(It.IsAny<Exception>()), Times.Once);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
