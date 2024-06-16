using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Pfim;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using SharpGLTF.Materials;
using SharpGLTF.Memory;

namespace MeshImportExport
{
    internal class TextureHelper
    {

        public static MaterialBuilder BuildMaterial(FileHelper fileHelper, RmvModel model)
        {
            var basePath = model.Material.GetTexture(TextureType.BaseColour);
            var materialPath = model.Material.GetTexture(TextureType.MaterialMap);
            var normalPath = model.Material.GetTexture(TextureType.Normal);

            var baseBytes = fileHelper.FindFile(basePath.Value.Path).DataSource.ReadData();
            var materialBytes = fileHelper.FindFile(materialPath.Value.Path).DataSource.ReadData();
            var normalBytes = fileHelper.FindFile(normalPath.Value.Path).DataSource.ReadData();

            var basePng = ConvertDdsToPng(baseBytes);
            var materialPng = ConvertDdsToPng(materialBytes);
            var normalPng = ConvertDdsToPng(normalBytes);

            var material = new MaterialBuilder(model.Material.ModelName + "_Material")
               .WithDoubleSide(true)
                .WithMetallicRoughness()
                .WithChannelImage(KnownChannel.BaseColor, new MemoryImage(basePng))
                .WithChannelImage(KnownChannel.MetallicRoughness, new MemoryImage(materialPng))
                .WithChannelImage(KnownChannel.Normal, new MemoryImage(normalPng));


            return material;
        }

        public static byte[] ConvertDdsToPng(byte[] dds)
        {
            using var m = new MemoryStream();
            using var w = new BinaryWriter(m);
            w.Write(dds);
            m.Seek(0, SeekOrigin.Begin);
            IImage image = Pfim.Pfim.FromStream(m);
            // Load the DDS image using Pfim


            //IImage image = Pfim.Pfim.FromStream(ddsPath);

            // Create a Bitmap from the DDS image data
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb; // Adjust if needed
            if (image.Format == Pfim.ImageFormat.Rgba32)
            {
                pixelFormat = PixelFormat.Format32bppArgb;
            }
            else if (image.Format == Pfim.ImageFormat.Rgb24)
            {
                pixelFormat = PixelFormat.Format24bppRgb;
            }
            else
            {
                throw new NotSupportedException($"Unsupported DDS format: {image.Format}");
            }

            // Create a Bitmap from the raw image data
            using (Bitmap bitmap = new Bitmap(image.Width, image.Height, pixelFormat))
            {
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, pixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(image.Data, 0, bitmapData.Scan0, image.DataLen);
                bitmap.UnlockBits(bitmapData);

                // Save the Bitmap as a PNG
                var b = new MemoryStream();
              
                bitmap.Save(b, System.Drawing.Imaging.ImageFormat.Png);

                var byteSteam = new BinaryReader(b);
                b.Seek(0, SeekOrigin.Begin);
                var binData = byteSteam.ReadBytes((int)b.Length);
                return binData;
                    
            }
        }
    }
}
