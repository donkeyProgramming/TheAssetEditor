using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Microsoft.Xna.Framework.Graphics;
using Pfim;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using SharpDX.DirectWrite;
using SharpGLTF.Materials;
using SharpGLTF.Memory;

namespace MeshImportExport
{
    public enum ExporterType
    { 
        NormalTexture,
        MaterialTexture,
        Geometry,
    }

    public class ExportFactory
    {
        public T GetExporter<T>() where T:IExporter
        {
            throw new NotImplementedException();
        }
    }

    public abstract class IExporterSettings
    { 
        public PackFile FileToExport { get; set; }
        public string OutputPath { get; set; }
        public bool GenerateImportSettings { get; set; }
    }

    public interface IExporter
    {
        //ExporterType ExporterType { get; }
        public string Name { get; }
        public string[] SupportedFileExtensions { get; }
    }

    //-----------------------

    public class NormalTextureExportSettings : IExporterSettings
    { 
        public bool ConvertToBlueMap { get; set; }
    }

    public class NormalTextureExporter : IExporter
    {
        public string Name => "DDS_To_Png_Normal";
        public string[] SupportedFileExtensions => [".dds"];

        public void Export(NormalTextureExportSettings settings)
        {
            var data = TextureHelper.ConvertDdsToPng(settings.FileToExport.DataSource.ReadData());
            File.WriteAllBytes(settings.OutputPath, data);
        }
    }



    //-----------------------

    public class MaterialTextureExportSettings : IExporterSettings
    {
        public bool SwapRoughness { get; set; }
    }

    public class MaterialTextureExporter : IExporter
    {
        public string Name => "DDS_To_Png_Material";
        public string[] SupportedFileExtensions => [".dds"];

        public void Export(MaterialTextureExportSettings settings)
        {
            var data = TextureHelper.ConvertDdsToPng(settings.FileToExport.DataSource.ReadData());
            File.WriteAllBytes(settings.OutputPath, data);
        }
    }


    //-----------------------

    public class GltfExportSettings : IExporterSettings
    {
        public bool ExportTextures { get; set; }
    }

    public class GlTfExporter : IExporter
    {
        private readonly ExportFactory _exportFactory;

        public string Name => "Rmv2_To_Gltf";
        public string[] SupportedFileExtensions => [".rmv2"];


        public void Export(GltfExportSettings settings)
        {
            _exportFactory.GetExporter<MaterialTextureExporter>().Export(new MaterialTextureExportSettings() { SwapRoughness = true });
            _exportFactory.GetExporter<NormalTextureExporter>().Export(new NormalTextureExportSettings() { ConvertToBlueMap = true });

            // Export gltf
        }
    }


    public class GlTfExporterView
    {
        public NotifyAttr<bool> Swap { get; set; } = new NotifyAttr<bool>(true);
        public string SwapDescription { get; } = "Bla bla, blender likes this";
    }




    internal class TextureHelper
    {
        public static MaterialBuilder BuildMaterial(PackFileService pfs, RmvModel model, PackFile inFile)
        {
            var basePath = model.Material.GetTexture(Shared.GameFormats.RigidModel.Types.TextureType.BaseColour);
            var materialPath = model.Material.GetTexture(Shared.GameFormats.RigidModel.Types.TextureType.MaterialMap);
            var normalPath = model.Material.GetTexture(Shared.GameFormats.RigidModel.Types.TextureType.Normal);
            var baseFile = inFile;
            var materialFile = inFile;
            var normalFile = inFile;

            if (basePath == null || materialPath == null ||  normalPath == null)
            {
                var fileList = pfs.SearchForFile(Path.GetFileNameWithoutExtension(pfs.GetFullPath(inFile)));
                foreach (var file in fileList)
                {
                    if (file.Contains("base"))
                    {
                        baseFile = pfs.FindFile(file);
                    }
                    if (file.Contains("material"))
                    {
                        materialFile = pfs.FindFile(file);
                    }
                    if (file.Contains("normal"))
                    {
                        normalFile = pfs.FindFile(file);
                    }
                }
            }
            else
            {
                baseFile = pfs.FindFile(basePath.Value.Path);
                materialFile = pfs.FindFile(materialPath.Value.Path);
                normalFile = pfs.FindFile(normalPath.Value.Path);
            }

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
        public static MaterialBuilder BuildFakeMaterial(PackFileService pfs, RmvModel model, PackFile inFile)
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
    }
}
