using System.Drawing;
using System.IO;
using System.Text;
using Editors.ImportExport;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Test.ImportExport.Exporting.Exporters;

public sealed class DdsTextureExporterTests
{
    [Test]
    public void NormalExportWithoutBlueConversionPreservesRawPngAndUsesRawName()
    {
        var source = new Pixel(17, 34, 201, 255);
        var capture = ExportNormal(source, convertToBlueNormalMap: false);
        var pixel = ReadPixel(capture.PngData);

        Assert.That(capture.Path, Does.EndWith("normal_raw.png"));
        Assert.That(pixel.R, Is.EqualTo(source.R));
        Assert.That(pixel.G, Is.EqualTo(source.G));
        Assert.That(pixel.B, Is.EqualTo(source.B));
        Assert.That(pixel.A, Is.EqualTo(source.A));
    }

    [Test]
    public void NormalExportConvertsNeutralPackedNormalToOpaqueBlueNormal()
    {
        var source = new Pixel(255, 128, 128, 128);
        var rawPixel = ReadPixel(ExportNormal(source, convertToBlueNormalMap: false).PngData);
        var expected = DecodePackedNormal(rawPixel);
        var capture = ExportNormal(source, convertToBlueNormalMap: true);
        var pixel = ReadPixel(capture.PngData);

        Assert.That(capture.Path, Does.EndWith("normal.png"));
        Assert.That(pixel.R, Is.EqualTo(expected.R));
        Assert.That(pixel.G, Is.EqualTo(expected.G));
        Assert.That(pixel.B, Is.EqualTo(expected.B));
        Assert.That(pixel.A, Is.EqualTo(255));
    }

    [Test]
    public void NormalExportConvertsSlopedPackedNormalUsingRedAlphaProductWithoutFlippingGreen()
    {
        var source = new Pixel(128, 128, 191, 191);
        var rawPixel = ReadPixel(ExportNormal(source, convertToBlueNormalMap: false).PngData);
        var expected = DecodePackedNormal(rawPixel);
        var capture = ExportNormal(source, convertToBlueNormalMap: true);
        var pixel = ReadPixel(capture.PngData);

        Assert.That(pixel.R, Is.EqualTo(expected.R));
        Assert.That(pixel.G, Is.EqualTo(expected.G));
        Assert.That(pixel.B, Is.EqualTo(expected.B));
        Assert.That(pixel.A, Is.EqualTo(255));
    }

    [Test]
    public void NormalExportUsesBothRedAndAlphaWhenReconstructingX()
    {
        // A conversion that accidentally uses only alpha would encode X as
        // 160 here; the shader's (R / 255) * (A / 255) product encodes 60.
        var source = new Pixel(96, 128, 160, 160);
        var rawPixel = ReadPixel(ExportNormal(source, convertToBlueNormalMap: false).PngData);
        var expected = DecodePackedNormal(rawPixel);
        var alphaOnlyRed = EncodeNormalComponent(2d * (rawPixel.A / 255d) - 1d);
        var capture = ExportNormal(source, convertToBlueNormalMap: true);
        var pixel = ReadPixel(capture.PngData);

        Assert.That(rawPixel.R, Is.GreaterThan(0).And.LessThan(255));
        Assert.That(rawPixel.A, Is.GreaterThan(0).And.LessThan(255));
        Assert.That(expected.R, Is.Not.EqualTo(alphaOnlyRed));
        Assert.That(Math.Abs(expected.R - alphaOnlyRed), Is.GreaterThan(20));
        Assert.That(pixel.R, Is.EqualTo(expected.R));
        Assert.That(pixel.G, Is.EqualTo(expected.G));
        Assert.That(pixel.B, Is.EqualTo(expected.B));
        Assert.That(pixel.A, Is.EqualTo(255));
    }

    [Test]
    public void NormalExportClampsOutOfDiskInputInsteadOfProducingNaN()
    {
        var source = new Pixel(255, 255, 255, 255);
        var rawPixel = ReadPixel(ExportNormal(source, convertToBlueNormalMap: false).PngData);
        var expected = DecodePackedNormal(rawPixel);
        var capture = ExportNormal(source, convertToBlueNormalMap: true);
        var pixel = ReadPixel(capture.PngData);

        Assert.That(pixel.R, Is.EqualTo(expected.R));
        Assert.That(pixel.G, Is.EqualTo(expected.G));
        Assert.That(pixel.B, Is.EqualTo(expected.B));
        Assert.That(pixel.A, Is.EqualTo(255));
    }

    [Test]
    public void MaterialExportSwapsRedAndBlueOnlyForBlenderFormat()
    {
        var source = new Pixel(17, 34, 201, 77);
        var raw = ExportMaterial(source, convertToBlenderFormat: false);
        var blender = ExportMaterial(source, convertToBlenderFormat: true);

        var rawPixel = ReadPixel(raw.PngData);
        var blenderPixel = ReadPixel(blender.PngData);

        Assert.That(raw.Path, Does.EndWith("material.png"));
        Assert.That(blender.Path, Does.EndWith("material.png"));
        Assert.That(blenderPixel.R, Is.EqualTo(rawPixel.B));
        Assert.That(blenderPixel.G, Is.EqualTo(rawPixel.G));
        Assert.That(blenderPixel.B, Is.EqualTo(rawPixel.R));
        Assert.That(blenderPixel.A, Is.EqualTo(255));
    }

    [Test]
    public void MaterialExportSwapsEveryPixelAcrossRowsAndMakesItOpaque()
    {
        var source = new[]
        {
            new Pixel(11, 22, 33, 44),
            new Pixel(55, 66, 77, 88),
            new Pixel(99, 111, 122, 133),
            new Pixel(144, 155, 166, 177),
            new Pixel(188, 199, 210, 221),
            new Pixel(232, 243, 14, 25)
        };

        var raw = ReadPixels(ExportMaterial(source, 3, 2, convertToBlenderFormat: false).PngData);
        var actual = ReadPixels(ExportMaterial(source, 3, 2, convertToBlenderFormat: true).PngData);
        var expected = raw
            .Select(pixel => Color.FromArgb(255, pixel.B, pixel.G, pixel.R))
            .ToArray();

        Assert.That(actual.Select(pixel => pixel.ToArgb()), Is.EqualTo(expected.Select(pixel => pixel.ToArgb())));
    }

    [Test]
    public void NormalExportConvertsEveryPixelAcrossRows()
    {
        var source = new[]
        {
            new Pixel(17, 34, 201, 255),
            new Pixel(96, 128, 160, 160),
            new Pixel(255, 128, 128, 128),
            new Pixel(128, 128, 191, 191),
            new Pixel(44, 213, 91, 37),
            new Pixel(255, 255, 255, 255)
        };

        var raw = ReadPixels(ExportNormal(source, 3, 2, convertToBlueNormalMap: false).PngData);
        var actual = ReadPixels(ExportNormal(source, 3, 2, convertToBlueNormalMap: true).PngData);
        var expected = raw.Select(DecodePackedNormal).ToArray();

        Assert.That(actual.Select(pixel => pixel.ToArgb()), Is.EqualTo(expected.Select(pixel => pixel.ToArgb())));
    }

    [Test]
    public void SystemImageSaveHandlerWritesPngDataWithoutReencoding()
    {
        var source = new[]
        {
            new Pixel(11, 22, 33, 44),
            new Pixel(55, 66, 77, 88),
            new Pixel(99, 111, 122, 133),
            new Pixel(144, 155, 166, 177),
            new Pixel(188, 199, 210, 221),
            new Pixel(232, 243, 14, 25)
        };
        var pngData = CreatePng(3, 2, source);
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"asset-editor-save-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(outputDirectory, "texture.png");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            new SystemImageSaveHandler().Save(pngData, outputPath);

            Assert.That(File.ReadAllBytes(outputPath), Is.EqualTo(pngData));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private static CapturedImage ExportNormal(Pixel pixel, bool convertToBlueNormalMap)
        => ExportNormal(new[] { pixel }, 1, 1, convertToBlueNormalMap);

    private static CapturedImage ExportNormal(
        IReadOnlyList<Pixel> pixels,
        int width,
        int height,
        bool convertToBlueNormalMap)
    {
        const string sourcePath = "textures/normal.dds";
        var packFileService = CreatePackFileService(sourcePath, CreateA8R8G8B8Dds(width, height, pixels));
        var capture = new CapturedImage();
        var exporter = new DdsToNormalPngExporter(packFileService.Object, capture);

        var outputPath = Path.Combine(Path.GetTempPath(), "asset-host-textures", "model.glb");
        exporter.Export(sourcePath, outputPath, convertToBlueNormalMap);
        return capture;
    }

    private static CapturedImage ExportMaterial(Pixel pixel, bool convertToBlenderFormat)
        => ExportMaterial(new[] { pixel }, 1, 1, convertToBlenderFormat);

    private static CapturedImage ExportMaterial(
        IReadOnlyList<Pixel> pixels,
        int width,
        int height,
        bool convertToBlenderFormat)
    {
        const string sourcePath = "textures/material.dds";
        var packFileService = CreatePackFileService(sourcePath, CreateA8R8G8B8Dds(width, height, pixels));
        var capture = new CapturedImage();
        var exporter = new DdsToMaterialPngExporter(packFileService.Object, capture);

        var outputPath = Path.Combine(Path.GetTempPath(), "asset-host-textures", "model.glb");
        exporter.Export(sourcePath, outputPath, convertToBlenderFormat);
        return capture;
    }

    private static Mock<IPackFileService> CreatePackFileService(string path, byte[] dds)
    {
        var service = new Mock<IPackFileService>();
        service
            .Setup(x => x.FindFile(path, It.IsAny<IPackFileContainer?>()))
            .Returns(PackFile.CreateFromBytes(path, dds));
        return service;
    }

    private static Color ReadPixel(byte[]? pngData)
        => ReadPixels(pngData)[0];

    private static Color[] ReadPixels(byte[]? pngData)
    {
        Assert.That(pngData, Is.Not.Null);
        Assert.That(pngData, Is.Not.Empty);
        using var stream = new MemoryStream(pngData!);
        using var image = Image.FromStream(stream);
        using var bitmap = new Bitmap(image);

        var pixels = new Color[checked(bitmap.Width * bitmap.Height)];
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
                pixels[y * bitmap.Width + x] = bitmap.GetPixel(x, y);
        }

        return pixels;
    }

    private static Color DecodePackedNormal(Color packed)
    {
        var x01 = (packed.R / 255d) * (packed.A / 255d);
        var y01 = packed.G / 255d;
        var normalX = Math.Clamp(2d * x01 - 1d, -1d, 1d);
        var normalY = Math.Clamp(2d * y01 - 1d, -1d, 1d);
        var normalZ = Math.Sqrt(Math.Max(0d, 1d - normalX * normalX - normalY * normalY));
        return Color.FromArgb(
            255,
            EncodeNormalComponent(normalX),
            EncodeNormalComponent(normalY),
            EncodeNormalComponent(normalZ));
    }

    private static byte EncodeNormalComponent(double component)
    {
        var encoded = ((Math.Clamp(component, -1d, 1d) + 1d) * 0.5d) * 255d;
        return (byte)Math.Clamp((int)Math.Round(encoded, MidpointRounding.AwayFromZero), 0, 255);
    }

    private static byte[] CreateA8R8G8B8Dds(Pixel pixel)
        => CreateA8R8G8B8Dds(1, 1, new[] { pixel });

    private static byte[] CreateA8R8G8B8Dds(int width, int height, IReadOnlyList<Pixel> pixels)
    {
        if (pixels.Count != checked(width * height))
            throw new ArgumentException("The pixel count must match the DDS dimensions.", nameof(pixels));

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("DDS "));
        writer.Write(124); // DDS_HEADER.dwSize
        writer.Write(0x0000100f); // CAPS | HEIGHT | WIDTH | PIXELFORMAT | PITCH
        writer.Write(height); // dwHeight
        writer.Write(width); // dwWidth
        writer.Write(checked(width * 4)); // dwPitchOrLinearSize
        writer.Write(0); // dwDepth
        writer.Write(0); // dwMipMapCount

        for (var i = 0; i < 11; i++)
            writer.Write(0); // dwReserved1

        writer.Write(32); // DDS_PIXELFORMAT.dwSize
        writer.Write(0x41); // DDPF_ALPHAPIXELS | DDPF_RGB
        writer.Write(0); // dwFourCC
        writer.Write(32); // dwRGBBitCount
        writer.Write(0x00ff0000); // dwRBitMask
        writer.Write(0x0000ff00); // dwGBitMask
        writer.Write(0x000000ff); // dwBBitMask
        writer.Write(unchecked((int)0xff000000)); // dwABitMask

        writer.Write(0x00001000); // DDSCAPS_TEXTURE
        writer.Write(0); // dwCaps2
        writer.Write(0); // dwCaps3
        writer.Write(0); // dwCaps4
        writer.Write(0); // dwReserved2

        // A8R8G8B8 is stored little-endian as B, G, R, A.
        foreach (var pixel in pixels)
        {
            writer.Write(pixel.B);
            writer.Write(pixel.G);
            writer.Write(pixel.R);
            writer.Write(pixel.A);
        }
        return stream.ToArray();
    }

    private static byte[] CreatePng(int width, int height, IReadOnlyList<Pixel> pixels)
    {
        if (pixels.Count != checked(width * height))
            throw new ArgumentException("The pixel count must match the PNG dimensions.", nameof(pixels));

        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                bitmap.SetPixel(x, y, Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B));
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    private readonly record struct Pixel(byte R, byte G, byte B, byte A);

    private sealed class CapturedImage : IImageSaveHandler
    {
        public byte[]? PngData { get; private set; }
        public string? Path { get; private set; }

        public void Save(byte[] pngData, string systemFilePath)
        {
            PngData = pngData;
            Path = systemFilePath;
        }
    }
}
