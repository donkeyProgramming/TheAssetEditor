using Editors.ImportExport.Common.Interfaces;
using Editors.ImportExport.Importing.Importers.PngToDds;
using Editors.ImportExport.Importing.Importers.PngToDds.Helpers.ImageProcessor;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.Types;
using System.Drawing;
using System.Drawing.Imaging;

namespace Test.ImportExport.Importing.Importers.GltfImporterTest;

public class EmbeddedTextureImportTests
{
    [Test]
    public void Import_MemoryImage_CreatesDdsWithoutSourcePath()
    {
        using var bitmap = new Bitmap(2, 2, PixelFormat.Format32bppArgb);
        bitmap.SetPixel(0, 0, Color.White);
        using var imageStream = new MemoryStream();
        bitmap.Save(imageStream, ImageFormat.Png);
        imageStream.Position = 0;

        var packFile = PngToDdsImporter.Import(
            imageStream,
            TextureType.Diffuse,
            GameTypeEnum.Warhammer3,
            "embedded.dds");

        Assert.That(packFile.DataSource.ReadData(), Is.Not.Empty);
    }

    [TestCase(TextureType.MaterialMap)]
    [TestCase(TextureType.Normal)]
    public void DisabledSpecialConversion_UsesDefaultProcessor(TextureType textureType)
    {
        var processor = ImageProcessorFactory.CreateImageProcessor(textureType, false);

        Assert.That(processor, Is.TypeOf<DefaultImageProcessor>());
    }
}
