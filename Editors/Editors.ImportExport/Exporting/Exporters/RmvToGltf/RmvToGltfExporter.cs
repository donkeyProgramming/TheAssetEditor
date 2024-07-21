using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.Core.PackFiles;
using System.IO;
using SharpGLTF.Materials;
using System.Numerics;
using SharpGLTF.Schema2;
using Shared.GameFormats.Animation;
using MeshImportExport;
using System.Windows;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry;
using System.Runtime.Serialization;
using System.Diagnostics.Eventing.Reader;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public record RmvToGltfExporterSettings(
        PackFile InputFile,
        string OutputPath,
        bool ExportTextures,
        bool ConvertMaterialTextureToBlender,
        bool ConvertNormalTextureToBlue,
        bool ExportAnimations
    );


    public class RmvToGltfExporter
    {
        private readonly PackFileService _packFileService;
        private readonly DdsToNormalPngExporter _exporterNormalBlue;
        private readonly DdsToPngExporter _ddsToPngExporter;
        private readonly DdsToMaterialPngExporter _exporterMaterial;

        public RmvToGltfExporter(PackFileService packFileSerivce, DdsToNormalPngExporter ddsToNormalPngExporter, DdsToPngExporter ddsToPngExporter, DdsToMaterialPngExporter ddsToMaterialPngExporter)
        {
            _packFileService = packFileSerivce;
            _exporterNormalBlue = ddsToNormalPngExporter;
            _ddsToPngExporter = ddsToPngExporter;
            _exporterMaterial = ddsToMaterialPngExporter;
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsRmvFile(file.Name))
                return ExportSupportEnum.HighPriority;
            if(FileExtensionHelper.IsWsModelFile(file.Name))
                return ExportSupportEnum.HighPriority;
            return ExportSupportEnum.NotSupported;
        }

        internal void Export(RmvToGltfExporterSettings settings)
        {
            var rmv2 = new ModelFactory().Load(settings.InputFile.DataSource.ReadData());
            //need to adjust for static mesh
            //need to have a mesh only export
            //_ddsToPngExporter.Export(settings.OutputPath, settings.InputFile);
            int tally = 0;
            if (rmv2.Header.SkeletonName != "")
            {
                tally = MeshWithSkeleton(settings.OutputPath, rmv2, settings.InputFile, settings);
            }
            else
            {
                tally = MeshWithoutSkeleton(settings.OutputPath, rmv2, settings.InputFile, settings);
            }

            var name = Path.GetFileNameWithoutExtension(settings.InputFile.Name);
            if (settings.ExportTextures == true)
            {
                for (int i = 0; i <= tally; i = i + 3)
                {
                    _exporterNormalBlue.Export("C:/franz/", name + "_" + i + ".png", i);
                }
            }
            if (settings.ConvertMaterialTextureToBlender == true)
            {
                for (int i = 2; i <= tally + 1; i = i + 3)
                {
                    _exporterMaterial.Export("C:/franz/", true, name + "_" + i + ".png", i);
                }
            }
            //setting for material texture conversion
            //setting for animations export

            //Have not attached the output path yet as the UI does not change that value as of now.
            //The file will go to C:/franz/ each export currently.
            //model.SaveGLTF(settings.OutputPath + settings.InputFile.Name);
        }

        internal int MeshWithSkeleton(string outputPath, RmvFile rmv2, PackFile file, RmvToGltfExporterSettings settings)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("default");
            var lodLevel = rmv2.ModelList.First();
            var skeletonName = rmv2.Header.SkeletonName + ".anim";
            var skeletonPath = "animations/skeletons/" + skeletonName;
            var invMatrixPackFilePath = "animations/skeletons/" + rmv2.Header.SkeletonName + ".bone_inv_trans_mats";
            var invMatrixPackFile = _packFileService.FindFile(invMatrixPackFilePath);
            var skeletonPackFile = _packFileService.FindFile(skeletonPath);
            var animFile = AnimationFile.Create(skeletonPackFile);
            var invMatrixFile = AnimInvMatrixFile.Create(invMatrixPackFile.DataSource.ReadDataAsChunk());
            Node bone = scene.CreateNode("Export root");

            var bindings = SkeletonExporter.CreateSkeletonFromGameSkeleton(animFile, invMatrixFile, bone);
            var tally = 0;

            foreach (var rmvMesh in lodLevel)
            {
                var material = new MaterialBuilder();
                if (settings.ExportTextures == true)
                {
                    material = _ddsToPngExporter.BuildMaterialPerMesh(rmvMesh, file, new DdsToPngExporterSettings(settings.ConvertMaterialTextureToBlender, settings.ConvertNormalTextureToBlue));
                }
                else
                {
                    material = _ddsToPngExporter.BuildFakeMaterialPerMesh(rmvMesh, file);
                }
                var mesh = model.CreateMesh(MeshExport.CreateMesh(rmvMesh, material));

                scene.CreateNode(rmvMesh.Material.ModelName).WithSkinnedMesh(mesh, bindings.ToArray());
                tally++;
            }
            model.SaveGLTF("C:/franz/" + Path.GetFileNameWithoutExtension(file.Name) + ".gltf");
            return tally;
        }


        internal int MeshWithoutSkeleton(string outputPath, RmvFile rmv2, PackFile file, RmvToGltfExporterSettings settings)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("default");
            var lodLevel = rmv2.ModelList.First();
            Node bone = scene.CreateNode("Export root");
            var tally = 0;
            foreach (var rmvMesh in lodLevel)
            {
                var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file, new DdsToPngExporterSettings(settings.ConvertMaterialTextureToBlender, settings.ConvertNormalTextureToBlue));
                var mesh = model.CreateMesh(MeshExport.ToStaticMeshBuilder(rmvMesh, material));
                scene.CreateNode(rmvMesh.Material.ModelName).WithMesh(mesh);
                tally++;
            }
            model.SaveGLTF("C:/franz/" + Path.GetFileNameWithoutExtension(file.Name) + ".gltf");
            return tally;
        }
    }
}
