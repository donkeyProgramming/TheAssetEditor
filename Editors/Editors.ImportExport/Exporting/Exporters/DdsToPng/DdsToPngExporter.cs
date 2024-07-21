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


namespace Editors.ImportExport.Exporting.Exporters.DdsToPng
{

    public record DdsToPngExporterSettings(
    bool ConvertMaterialTextureToBlender,
    bool ConvertNormalTextureToBlue);

    public class DdsToPngExporter
    {
        private readonly PackFileService _packFileService;

        public DdsToPngExporter(PackFileService pfs)
        {
            _packFileService = pfs;
        }
        
        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        internal void Export(string outputPath, PackFile file, DdsToPngExporterSettings settings)
        {
            var rmv2 = new ModelFactory().Load(file.DataSource.ReadData());
            var lodLevel = rmv2.ModelList.First();
            foreach (var rmvMesh in lodLevel)
            {
                var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file, settings);
            }
        }

        internal MaterialBuilder BuildMaterialPerMesh(RmvModel rmvMesh, PackFile file, DdsToPngExporterSettings settings)
        {
            var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file, settings);
            return material;
        }

        internal MaterialBuilder BuildFakeMaterialPerMesh(RmvModel rmvMesh, PackFile file)
        {
            var material = TextureHelper.BuildFakeMaterial(_packFileService, rmvMesh);
            return material;
        }
    }
}
