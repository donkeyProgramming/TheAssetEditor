using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using System.IO;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
//using SharpGLTF.Materials;
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
                var textures = rmvMesh.Material.GetAllTextures();
                _ddsToNormalPngExporter.Export(textures.FirstOrDefault(t => t.TexureType == TextureType.Normal).Path, settings.OutputPath, settings.ConvertNormalTextureToBlue);
                _ddsToMaterialPngExporter.Export(textures.FirstOrDefault(t => t.TexureType == TextureType.MaterialMap).Path, settings.OutputPath, settings.ConvertMaterialTextureToBlender);
                GenericExportNoConversion(settings.OutputPath, textures.FirstOrDefault(t => t.TexureType == TextureType.BaseColour));
            }
        }
        public string GenericExportNoConversion(string outputPath, RmvTexture texture)
        {
            var packFile = _packFileService.FindFile(texture.Path);
            var bytes = packFile.DataSource.ReadData();
            var fileDirectory = outputPath + "/" + Path.GetFileNameWithoutExtension(packFile.Name) + ".png";
            var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
            var ms = new MemoryStream(imgBytes);
            using Image img = Image.FromStream(ms);
            using Bitmap bitmap = new Bitmap(img);
            _imageSaveHandler.Save(bitmap, fileDirectory);
            return fileDirectory;
        }
    }
}
