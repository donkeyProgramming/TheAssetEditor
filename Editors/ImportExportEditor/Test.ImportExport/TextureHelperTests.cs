using System.Drawing;
using System.IO;
using System.Text;
using MeshImportExport;

namespace Test.ImportExport;

public class TextureHelperTests
{
    [Test]
    public void ConvertDdsToPngPreservesRedForSolidDxt1Texture()
    {
        var png = TextureHelper.ConvertDdsToPng(CreateSolidRedDxt1Dds());

        using var pngStream = new MemoryStream(png);
        using var bitmap = new Bitmap(pngStream);
        var pixel = bitmap.GetPixel(0, 0);

        Assert.That(pixel.R, Is.GreaterThan(200));
        Assert.That(pixel.G, Is.LessThan(20));
        Assert.That(pixel.B, Is.LessThan(20));
    }

    private static byte[] CreateSolidRedDxt1Dds()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("DDS "));
        writer.Write(124); // DDS_HEADER.dwSize
        writer.Write(0x00081007); // CAPS | HEIGHT | WIDTH | PIXELFORMAT | LINEARSIZE
        writer.Write(4); // dwHeight
        writer.Write(4); // dwWidth
        writer.Write(8); // dwPitchOrLinearSize: one DXT1 block
        writer.Write(0); // dwDepth
        writer.Write(0); // dwMipMapCount

        for (var i = 0; i < 11; i++)
            writer.Write(0); // dwReserved1

        writer.Write(32); // DDS_PIXELFORMAT.dwSize
        writer.Write(0x00000004); // DDPF_FOURCC
        writer.Write(Encoding.ASCII.GetBytes("DXT1"));
        writer.Write(0); // dwRGBBitCount
        writer.Write(0); // dwRBitMask
        writer.Write(0); // dwGBitMask
        writer.Write(0); // dwBBitMask
        writer.Write(0); // dwABitMask

        writer.Write(0x00001000); // DDSCAPS_TEXTURE
        writer.Write(0); // dwCaps2
        writer.Write(0); // dwCaps3
        writer.Write(0); // dwCaps4
        writer.Write(0); // dwReserved2

        // DXT1 endpoint 0 is solid red, endpoint 1 is black, and all pixels
        // select endpoint 0.
        writer.Write((ushort)0xF800); // RGB565 red
        writer.Write((ushort)0x0000); // RGB565 black
        writer.Write(0u); // four 2-bit selectors, all zero

        return stream.ToArray();
    }
}
