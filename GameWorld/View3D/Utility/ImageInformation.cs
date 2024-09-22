using Pfim;
using System.Text;

namespace GameWorld.Core.Utility
{
    public class ImageInformation
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public ImageFormat Format { get; set; }
        public int BitsPerPixel { get; set; }
        public bool Compressed { get; set; }
        public byte PixelDepthBytes { get; set; }

        public bool IsDdsImage { get; set; }

        // DDS header
        public uint Header_Depth { get; set; }
        public uint Header_MipMapCount { get; set; }
        public DdsPixelFormat Header_PixelFormat { get; set; }
        public uint Header_Caps { get; set; }
        public uint Header_Caps2 { get; set; }
        public uint Header_Caps3 { get; set; }
        public uint Header_Caps4 { get; set; }

        // Header10
        public DxgiFormat Header10_DxgiFormat { get; set; }
        public D3D10ResourceDimension Header10_ResourceDimension { get; set; }
        public uint Header10_MiscFlag { get; set; }

        public uint Header10_ArraySize { get; set; }
        public uint Header10_MiscFlags2 { get; set; }


        public void SetFromImage(IImage image)
        {
            Format = image.Format;
            Width = image.Width;
            Height = image.Height;

            BitsPerPixel = image.BitsPerPixel;
            Compressed = image.Compressed;


            if (image is Dds ddsImage)
            {
                IsDdsImage = true;

                if (ddsImage.Header != null)
                {
                    Header_Depth = ddsImage.Header.Depth;
                    Header_MipMapCount = ddsImage.Header.MipMapCount;
                    Header_PixelFormat = ddsImage.Header.PixelFormat;
                    Header_Caps = ddsImage.Header.Caps;
                    Header_Caps2 = ddsImage.Header.Caps2;
                    Header_Caps3 = ddsImage.Header.Caps3;
                    Header_Caps4 = ddsImage.Header.Caps4;
                }

                if (ddsImage.Header10 != null)
                {
                    Header10_DxgiFormat = ddsImage.Header10.DxgiFormat;
                    Header10_ResourceDimension = ddsImage.Header10.ResourceDimension;
                    Header10_MiscFlag = ddsImage.Header10.MiscFlag;
                    Header10_ArraySize = ddsImage.Header10.ArraySize;
                    Header10_MiscFlags2 = ddsImage.Header10.MiscFlags2;
                }
            }
        }

        public string GetAsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Format: '{Format}' ");
            sb.AppendLine($"Width: '{Width}' ");
            sb.AppendLine($"Height: '{Height}' ");
            sb.AppendLine($"BitsPerPixel: '{BitsPerPixel}' ");
            sb.AppendLine($"Compressed: '{Compressed}' ");
            sb.AppendLine($"IsDdsImage: '{IsDdsImage}' ");

            if (IsDdsImage)
            {
                sb.AppendLine($"Header");
                sb.AppendLine($"\t Depth: '{Header_Depth}' ");
                sb.AppendLine($"\t MipMapCount: '{Header_MipMapCount}' ");
                sb.AppendLine($"\t Caps: '{Header_Caps} ");
                sb.AppendLine($"\t Caps2: '{Header_Caps2}' ");
                sb.AppendLine($"\t Caps4: '{Header_Caps4}' ");
                sb.AppendLine($"\t PixelFormat");
                sb.AppendLine($"\t\t ABitMask: '{Header_PixelFormat.ABitMask}' ");
                sb.AppendLine($"\t\t BBitMask: '{Header_PixelFormat.BBitMask}' ");
                sb.AppendLine($"\t\t FourCC: '{Header_PixelFormat.FourCC}' ");
                sb.AppendLine($"\t\t GBitMask: '{Header_PixelFormat.GBitMask}' ");
                sb.AppendLine($"\t\t PixelFormatFlags: '{Header_PixelFormat.PixelFormatFlags}' ");
                sb.AppendLine($"\t\t RBitMask: '{Header_PixelFormat.RBitMask}' ");
                sb.AppendLine($"\t\t RGBBitCount: '{Header_PixelFormat.RGBBitCount}' ");
                sb.AppendLine($"\t\t Size: '{Header_PixelFormat.Size}' ");

                sb.AppendLine($"Header10");
                sb.AppendLine($"\t DxgiFormat: '{Header10_DxgiFormat}' ");
                sb.AppendLine($"\t ResourceDimension: '{Header10_ResourceDimension}' ");
                sb.AppendLine($"\t MiscFlag: '{Header10_MiscFlag} ");
                sb.AppendLine($"\t ArraySize: '{Header10_ArraySize}' ");
                sb.AppendLine($"\t MiscFlags2: '{Header10_MiscFlags2}' ");
            }

            return sb.ToString();
        }
    }

}
