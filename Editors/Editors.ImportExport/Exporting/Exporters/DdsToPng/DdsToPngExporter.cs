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

        internal void Export(string outputPath, PackFile file)
        {
            var rmv2 = new ModelFactory().Load(file.DataSource.ReadData());
            //make sure the skeleton exists
            if (rmv2.Header.SkeletonName != "")
            {
                MeshWithSkeleton(outputPath, rmv2, file);
            }
            else
            {
                MeshWithoutSkeleton(outputPath, rmv2, file);
            }
            
        }
        
        internal void MeshWithSkeleton(string outputPath, RmvFile rmv2, PackFile file)
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

            foreach (var rmvMesh in lodLevel)
            {
                var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file);
                var mesh = model.CreateMesh(MeshExport.CreateMesh(rmvMesh, material));

                scene.CreateNode(rmvMesh.Material.ModelName).WithSkinnedMesh(mesh, bindings.ToArray());
            }
            model.SaveGLTF("C:/franz/" + Path.GetFileNameWithoutExtension(file.Name) + ".gltf");
        }

        //this redirects properly but does not work yet
        internal void MeshWithoutSkeleton(string outputPath, RmvFile rmv2, PackFile file)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("default");
            var lodLevel = rmv2.ModelList.First();
            Node bone = scene.CreateNode("Export root");

            foreach (var rmvMesh in lodLevel)
            {
                var material = TextureHelper.BuildMaterial(_packFileService, rmvMesh, file);
                var mesh = model.CreateMesh(MeshExport.CreateMesh(rmvMesh, material));
                scene.CreateNode(rmvMesh.Material.ModelName).WithMesh(mesh);
            }
            model.SaveGLTF("C:/franz/" + Path.GetFileNameWithoutExtension(file.Name) + ".gltf");
        }
    }
}
