using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using System.Drawing.Imaging;
using System.IO;
using Editors.ImportExport.Misc;
using Pfim;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using System;
using System.Drawing;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel.Types;
//using SharpGLTF.Schema2;
using MeshImportExport;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.Animation;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
//using SharpDX.MediaFoundation.DirectX;


namespace Editors.ImportExport.Exporting.Exporters.DdsToPng
{

    //public record DdsToPngExporterSettings(
    //bool ConvertMaterialTextureToBlender,
    //bool ConvertNormalTextureToBlue);

    public class DdsToPngExporter
    {
        private readonly PackFileService _packFileService;
        private readonly RmvToGltfExporterSettings _settings;
        private readonly DdsToNormalPngExporter _ddsToNormalPngExporter;
        private readonly DdsToMaterialPngExporter _ddsToMaterialPngExporter;

        public DdsToPngExporter(PackFileService pfs, RmvToGltfExporterSettings settings, DdsToNormalPngExporter ddsToNormalPngExporter, DdsToMaterialPngExporter ddsToMaterialPngExporter)
        {
            _packFileService = pfs;
            _settings = settings;
            _ddsToNormalPngExporter = ddsToNormalPngExporter;
            _ddsToMaterialPngExporter = ddsToMaterialPngExporter;
        }
        
        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        internal void Export(string outputPath, PackFile file)
        {
            var rmv2 = new ModelFactory().Load(file.DataSource.ReadData());
            var lodLevel = rmv2.ModelList.First();
            foreach (var rmvMesh in lodLevel)
            {
                
                //var material = BuildMaterial(_packFileService, rmvMesh, file, _settings, _ddsToNormalPngExporter, _ddsToMaterialPngExporter);
                //var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file, _settings);
            }
        }

        internal MaterialBuilder BuildMaterialPerMesh(RmvModel rmvMesh, RmvToGltfExporterSettings settings)
        {
            var material = GenerateMaterial(settings, rmvMesh);
            //var material = BuildMaterial(_packFileService, rmvMesh, file, _settings, _ddsToNormalPngExporter, _ddsToMaterialPngExporter);
            //var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file, settings);
            return material;
        }

        internal MaterialBuilder BuildFakeMaterialPerMesh(RmvModel rmvMesh, PackFile file)
        {
            var material = TextureHelper.BuildFakeMaterial(_packFileService, rmvMesh);
            return material;
        }

        /**public static MaterialBuilder BuildMaterial(PackFileService pfs, RmvModel model, PackFile inFile, 
            RmvToGltfExporterSettings settings, DdsToNormalPngExporter ddsToNormalPngExporter, DdsToMaterialPngExporter ddsToMaterialPngExporter)
        {

            var normalMapTexture = model.Material.GetTexture(TextureType.Normal);
            var materialMapTexture = model.Material.GetTexture(TextureType.MaterialMap);
            var baseColourTexture = model.Material.GetTexture(TextureType.BaseColour);

            var baseColourFile = pfs.FindFile(baseColourTexture.Value.Path);
            var materialMapFile = pfs.FindFile(materialMapTexture.Value.Path);
            var normalMapFile = pfs.FindFile(normalMapTexture.Value.Path);

            var basePath = pfs.GetFullPath(baseColourFile);
            var materialMapPath = pfs.GetFullPath(materialMapFile);
            var normalMapPath = pfs.GetFullPath(normalMapFile);

            if (normalMapTexture != null && settings.ConvertNormalTextureToBlue == true)
            {
                ddsToNormalPngExporter.Export("", "", true); //need to change this export to use path
            }
            if (materialMapTexture != null && settings.ConvertMaterialTextureToBlender == true)
            {
                ddsToMaterialPngExporter.Export("","", true); //need to change this export to use path
            }

            var material = new MaterialBuilder(model.Material.ModelName + "_Material")
               .WithDoubleSide(true)
                .WithMetallicRoughness()
                .WithChannelImage(KnownChannel.BaseColor, new MemoryImage(TextureHelper.ConvertDdsToPng(baseColourFile.DataSource.ReadData())))
                .WithChannelImage(KnownChannel.MetallicRoughness, new MemoryImage())
                .WithChannelImage(KnownChannel.Normal, new MemoryImage(normalMapPath));

            return material;
        }**/

        public MaterialBuilder GenerateMaterial(RmvToGltfExporterSettings settings, RmvModel rmv2Mesh)
        {
            var textures = rmv2Mesh.Material.GetAllTextures();

            var normalMapTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.Normal);
            if(normalMapTexture.Path != null)
            {
                _ddsToNormalPngExporter.Export(_packFileService.GetFullPath(settings.InputFile), settings.OutputPath, settings.ConvertNormalTextureToBlue);
            }

            var materialTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.MaterialMap);
            if (materialTexture.Path != null)
            {
                _ddsToMaterialPngExporter.Export(_packFileService.GetFullPath(settings.InputFile), settings.OutputPath, settings.ConvertMaterialTextureToBlender);
            }

            var baseColourTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.BaseColour);
            if (baseColourTexture.Path != null)
            {
                var packFile = _packFileService.FindFile(_packFileService.GetFullPath(settings.InputFile));
                var bytes = packFile.DataSource.ReadData();
                var fileDirectory = settings.OutputPath + "/" + settings.InputFile.Name + ".png";
                var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
                GenericExportNoConversion(imgBytes, settings.OutputPath, fileDirectory);
            }


            var material = new MaterialBuilder(rmv2Mesh.Material.ModelName + "_Material")
            .WithDoubleSide(true)
            .WithMetallicRoughness()
            .WithChannelImage(KnownChannel.BaseColor, baseColourTexture.Path)
            .WithChannelImage(KnownChannel.MetallicRoughness, materialTexture.Path)
            .WithChannelImage(KnownChannel.Normal, normalMapTexture.Path);

            return material;
        }


        public void GenericExportNoConversion(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            var ms = new MemoryStream(imgBytes);
            using Image img = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(img);
            bitmap.Save(fileDirectory, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
