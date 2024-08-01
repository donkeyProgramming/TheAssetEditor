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
using System.ComponentModel;

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

        public void Export(RmvToGltfExporterSettings settings)
        {
            var rmv2 = new ModelFactory().Load(settings.InputFile.DataSource.ReadData());
            int tally = 0;
            if (rmv2.Header.SkeletonName != "")
            {
                tally = MeshWithSkeleton(rmv2, settings.InputFile, settings);
            }
            else
            {
                //tally = MeshWithoutSkeleton(rmv2, settings.InputFile, settings);
            }

            var name = Path.GetFileNameWithoutExtension(settings.InputFile.Name);
            //checks for normal conversion and material conversion, the tally is to line up with
            //how gltf names the exported textures so we can find the exact ones we want to convert
            if (settings.ExportTextures == true)
            {
                for (int i = 0; i <= tally; i = i + 3)
                {
                    _exporterNormalBlue.Export(settings.OutputPath, name + "_" + i + ".png", true);
                }
            }
            if (settings.ConvertMaterialTextureToBlender == true)
            {
                for (int i = 2; i <= tally + 1; i = i + 3)
                {
                    _exporterMaterial.Export(settings.OutputPath, "", true);
                }
            }
            //MessageBox.Show("Export successful to: " + settings.OutputPath);
            //setting for animations export still needed
        }

        internal int MeshWithSkeleton(RmvFile rmv2, PackFile file, RmvToGltfExporterSettings settings)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("default");
            var lodLevel = rmv2.ModelList.First();

            var skeletonName = rmv2.Header.SkeletonName + ".anim";
            var skeletonSearchList = _packFileService.SearchForFile(skeletonName);
            var skeletonPath = _packFileService.GetFullPath(_packFileService.FindFile(skeletonSearchList[0]));
            var skeletonPackFile = _packFileService.FindFile(skeletonPath);

            var invMatrixName = rmv2.Header.SkeletonName + ".bone_inv_trans_mats";
            var invMatrixSearchList = _packFileService.SearchForFile(invMatrixName);
            var invMatrixFilePath = _packFileService.GetFullPath(_packFileService.FindFile(invMatrixSearchList[0]));
            var invMatrixPackFile = _packFileService.FindFile(invMatrixFilePath);

            var animFile = AnimationFile.Create(skeletonPackFile);
            var invMatrixFile = AnimInvMatrixFile.Create(invMatrixPackFile.DataSource.ReadDataAsChunk());
            Node bone = scene.CreateNode("Export root");
            var bindings = SkeletonExporter.CreateSkeletonFromGameSkeleton(animFile, invMatrixFile, bone);

            foreach (var rmvMesh in lodLevel)
            {
                var material = new MaterialBuilder();
                if (settings.ExportTextures == true)
                {
                    material = _ddsToPngExporter.BuildMaterialPerMesh(rmvMesh, file, settings);
                }
                else
                {
                    material = _ddsToPngExporter.BuildFakeMaterialPerMesh(rmvMesh, file);
                }
                var mesh = model.CreateMesh(MeshExport.CreateMesh(rmvMesh, material));

                scene.CreateNode(rmvMesh.Material.ModelName).WithSkinnedMesh(mesh, bindings.ToArray());
            }

            var tally = model.LogicalImages.Count();

            model.SaveGLTF(settings.OutputPath + Path.GetFileNameWithoutExtension(file.Name) + ".gltf");
            return tally - 1;
        }


        /**internal int MeshWithoutSkeleton(RmvFile rmv2, PackFile file, RmvToGltfExporterSettings settings)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("default");
            var lodLevel = rmv2.ModelList.First();
            Node bone = scene.CreateNode("Export root");


            foreach (var rmvMesh in lodLevel)
            {
                var material = _ddsToPngExporter.BuildMaterial(_packFileService, rmvMesh, file, settings);
                var mesh = model.CreateMesh(MeshExport.ToStaticMeshBuilder(rmvMesh, material));
                scene.CreateNode(rmvMesh.Material.ModelName).WithMesh(mesh);
            }

            var tally = model.LogicalImages.Count();

            model.SaveGLTF(settings.OutputPath + Path.GetFileNameWithoutExtension(file.Name) + ".gltf");
            return tally - 1;
        }**/
    }
}
