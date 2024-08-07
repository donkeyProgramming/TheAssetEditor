using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using System.IO;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Materials;
using System;
using System.Drawing;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Shared.GameFormats.RigidModel.Types;
using MeshImportExport;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;


namespace Editors.ImportExport.Exporting.Exporters.DdsToPng
{

    public class DdsToPngExporter
    {
        private readonly PackFileService _packFileService;
        private readonly DdsToNormalPngExporter _ddsToNormalPngExporter;
        private readonly DdsToMaterialPngExporter _ddsToMaterialPngExporter;
        private readonly IImageSaveHandler _imageSaveHandler;

        public DdsToPngExporter(PackFileService pfs, DdsToNormalPngExporter ddsToNormalPngExporter, DdsToMaterialPngExporter ddsToMaterialPngExporter, IImageSaveHandler imageSaveHandler)
        {
            _packFileService = pfs;
            _ddsToNormalPngExporter = ddsToNormalPngExporter;
            _ddsToMaterialPngExporter = ddsToMaterialPngExporter;
            _imageSaveHandler = imageSaveHandler;
        }
        
        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        public void Export(string outputPath, PackFile file, RmvToGltfExporterSettings settings)
        {
            var rmv2 = new ModelFactory().Load(file.DataSource.ReadData());
            var lodLevel = rmv2.ModelList.First();
            foreach (var rmvMesh in lodLevel)
            {
                var material = GenerateMaterial(settings, rmvMesh);
            }
        }

        //I do not think I need these anymore, but keeping them for now until I work on the RmvToGltfExporter (BuildMaterialPerMesh and BuildFakeMaterialPerMesh)
        internal MaterialBuilder BuildMaterialPerMesh(RmvModel rmvMesh, RmvToGltfExporterSettings settings)
        {
            var material = GenerateMaterial(settings, rmvMesh);
            return material;
        }

        internal MaterialBuilder BuildFakeMaterialPerMesh(RmvModel rmvMesh, PackFile file)
        {
            var material = TextureHelper.BuildFakeMaterial(_packFileService, rmvMesh);
            return material;
        }

        public MaterialBuilder GenerateMaterial(RmvToGltfExporterSettings settings, RmvModel rmv2Mesh)
        {
            var textures = rmv2Mesh.Material.GetAllTextures();

            var normalMapTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.Normal);
            if(normalMapTexture.Path != null)
            {
                _ddsToNormalPngExporter.Export(normalMapTexture.Path, settings.OutputPath, settings.ConvertNormalTextureToBlue);
            }

            var materialTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.MaterialMap);
            if (materialTexture.Path != null)
            {
                _ddsToMaterialPngExporter.Export(materialTexture.Path, settings.OutputPath, settings.ConvertMaterialTextureToBlender);
            }

            var baseColourTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.BaseColour);
            if (baseColourTexture.Path != null)
            {
                var packFile = _packFileService.FindFile(baseColourTexture.Path);
                var bytes = packFile.DataSource.ReadData();
                var fileDirectory = settings.OutputPath + "/" + Path.GetFileNameWithoutExtension(packFile.Name) + ".png";
                var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
                GenericExportNoConversion(imgBytes, settings.OutputPath, fileDirectory);
            }

            var outputBaseColourTexturePath = settings.OutputPath + "/" + Path.GetFileNameWithoutExtension(baseColourTexture.Path) + ".png";
            var outputMaterialTexturePath = settings.OutputPath + "/" + Path.GetFileNameWithoutExtension(materialTexture.Path) + ".png";
            var outputNormalTexturePath = settings.OutputPath + "/" + Path.GetFileNameWithoutExtension(normalMapTexture.Path) + ".png";
            var material = new MaterialBuilder(rmv2Mesh.Material.ModelName + "_Material")
            .WithDoubleSide(true)
            .WithMetallicRoughness()
            .WithChannelImage(KnownChannel.BaseColor, outputBaseColourTexturePath)
            .WithChannelImage(KnownChannel.MetallicRoughness, outputMaterialTexturePath)
            .WithChannelImage(KnownChannel.Normal, outputNormalTexturePath);

            return material;
        }

        public void GenericExportNoConversion(byte[] imgBytes, string outputPath, string fileDirectory)
        {
            var ms = new MemoryStream(imgBytes);
            using Image img = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(img);
            _imageSaveHandler.Save(bitmap, fileDirectory);
        }
    }
}
