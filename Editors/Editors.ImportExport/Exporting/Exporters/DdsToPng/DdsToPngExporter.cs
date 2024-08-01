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
using SharpGLTF.Schema2;
using MeshImportExport;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.Animation;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;


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
                var material = BuildMaterial(_packFileService, rmvMesh, file, _settings, _ddsToNormalPngExporter, _ddsToMaterialPngExporter);
                //var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file, _settings);
            }
        }

        internal MaterialBuilder BuildMaterialPerMesh(RmvModel rmvMesh, PackFile file, RmvToGltfExporterSettings settings)
        {
            var material = BuildMaterial(_packFileService, rmvMesh, file, _settings, _ddsToNormalPngExporter, _ddsToMaterialPngExporter);
            //var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file, settings);
            return material;
        }

        internal MaterialBuilder BuildFakeMaterialPerMesh(RmvModel rmvMesh, PackFile file)
        {
            var material = TextureHelper.BuildFakeMaterial(_packFileService, rmvMesh);
            return material;
        }

        public static MaterialBuilder BuildMaterial(PackFileService pfs, RmvModel model, PackFile inFile, 
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
        }
    }
}
