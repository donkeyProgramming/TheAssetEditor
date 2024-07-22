using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters;
using Microsoft.Xna.Framework.Graphics;
using Pfim;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
//using SharpDX.DirectWrite;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using System.Windows;

namespace MeshImportExport
{

    public class TextureHelper
    {
        public static MaterialBuilder BuildMaterial(PackFileService pfs, RmvModel model, PackFile inFile)
        {
            var pngBytes = FindFileAndReturnPngList(pfs, model, inFile);
            var basePng = pngBytes[0];
            var materialPng = pngBytes[1];
            var normalPng = pngBytes[2];


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

        //this will be used when a user does not want to have textures
        public static MaterialBuilder BuildFakeMaterial(PackFileService pfs, RmvModel model)
        {
            var baseFile = pfs.FindFile("commontextures/default_base_colour.dds");
            var materialFile = pfs.FindFile("commontextures/default_material_map.dds");
            var normalFile = pfs.FindFile("commontextures/default_normal.dds");
            var baseBytes = baseFile.DataSource.ReadData();
            var materialBytes = materialFile.DataSource.ReadData();
            var normalBytes = normalFile.DataSource.ReadData();


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

        public static List<byte[]> FindFileAndReturnPngList(PackFileService pfs, RmvModel model, PackFile inFile)
        {
            var basePath = model.Material.GetTexture(TextureType.BaseColour);
            var materialPath = model.Material.GetTexture(TextureType.MaterialMap);
            var normalPath = model.Material.GetTexture(TextureType.Normal);
            var baseFile = inFile;
            var materialFile = inFile;
            var normalFile = inFile;
            List<PackFile> packFileList = new List<PackFile>();

            if (basePath == null || materialPath == null || normalPath == null)
            {
                packFileList = FallbackIfFileNotFound(pfs, inFile);
            }
            else
            {
                baseFile = pfs.FindFile(basePath.Value.Path);
                materialFile = pfs.FindFile(materialPath.Value.Path);
                normalFile = pfs.FindFile(normalPath.Value.Path);
                packFileList.Add(baseFile);
                packFileList.Add(materialFile);
                packFileList.Add(normalFile);
            }

            var baseBytes = packFileList[0].DataSource.ReadData();
            var materialBytes = packFileList[1].DataSource.ReadData();
            var normalBytes = packFileList[2].DataSource.ReadData();

            var basePng = ConvertDdsToPng(baseBytes);
            var materialPng = ConvertDdsToPng(materialBytes);
            var normalPng = ConvertDdsToPng(normalBytes);

            List<byte[]> pngList = new List<byte[]>();
            pngList.Add(basePng);
            pngList.Add(materialPng);
            pngList.Add(normalPng);
            return pngList;
        }

        public static List<PackFile> FallbackIfFileNotFound(PackFileService pfs, PackFile inFile)
        {
            var fileList = pfs.SearchForFile(Path.GetFileNameWithoutExtension(pfs.GetFullPath(inFile)));
            List<PackFile> packFileList = new List<PackFile>();
            foreach (var file in fileList)
            {
                if (file.Contains("base"))
                {
                    var baseFile = pfs.FindFile(file);
                    packFileList.Add(baseFile);
                }
                if (file.Contains("material"))
                {
                    var materialFile = pfs.FindFile(file);
                    packFileList.Add(materialFile);
                }
                if (file.Contains("normal"))
                {
                    var normalFile = pfs.FindFile(file);
                    packFileList.Add(normalFile);
                }
            }
            return packFileList;
        }
    }
}
