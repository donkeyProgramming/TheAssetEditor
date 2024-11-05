using System.IO;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public record RmvToGltfExporterSettings(

        PackFile InputModelFile,
        List<PackFile> InputAnimationFiles,
        PackFile InputSkeletonFile,
        string OutputPath,
        bool ConvertMaterialTextureToBlender,
        bool ConvertNormalTextureToBlue,
        bool ExportAnimations        
    );

    public class RmvToGltfExporter
    {
        private readonly PackFileService _packFileService;
        private readonly GltfMeshBuilder _gltfMeshBuilder;
        private readonly GltfSkeletonCreator _gltfSkeletonCreator;

        public RmvToGltfExporter(PackFileService packFileSerivce,  GltfMeshBuilder gltfMeshBuilder, GltfSkeletonCreator gltfSkeletonCreator)
        {
            _packFileService = packFileSerivce;
            _gltfMeshBuilder = gltfMeshBuilder;
            _gltfSkeletonCreator = gltfSkeletonCreator;
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsRmvFile(file.Name))
                return ExportSupportEnum.HighPriority;
            if (FileExtensionHelper.IsWsModelFile(file.Name))
                return ExportSupportEnum.HighPriority;
            return ExportSupportEnum.NotSupported;
        }

        public void Export(RmvToGltfExporterSettings settings)
        {
            const bool doMirror = true; // TODO: put in view (CheckBox) -> settomgs

            var rmv2 = new ModelFactory().Load(settings.InputModelFile.DataSource.ReadData());
            var hasSkeleton = string.IsNullOrWhiteSpace(rmv2.Header.SkeletonName) == false;
            ProcessedGltfSkeleton? gltfSkeleton = null;
            
            var outputScene = ModelRoot.CreateModel();

            if (hasSkeleton)
            {
                var animSkeletonFile = FetchAnimSkeleton(rmv2);
                gltfSkeleton = _gltfSkeletonCreator.Create(outputScene, animSkeletonFile, doMirror);

                if (settings.ExportAnimations && settings.InputAnimationFiles.Count != 0)
                    GenerateAnimations(settings, gltfSkeleton, outputScene, animSkeletonFile, doMirror);
            }

            var meshes = _gltfMeshBuilder.Build(rmv2, settings, doMirror);

            BuildGltfScene(meshes, gltfSkeleton, settings, outputScene);
        }

        void BuildGltfScene(List<IMeshBuilder<MaterialBuilder>> meshBuilders, ProcessedGltfSkeleton? gltfSkeleton, RmvToGltfExporterSettings settings, ModelRoot outputScene)
        {
            var scene = outputScene.UseScene("default");
            foreach (var meshBuilder in meshBuilders)
            {
                var mesh = outputScene.CreateMesh(meshBuilder);

                if (gltfSkeleton != null)
                    scene.CreateNode(mesh.Name).WithSkinnedMesh(mesh, gltfSkeleton.Data.ToArray());
                else
                    scene.CreateNode(mesh.Name).WithMesh(mesh);
            }
            outputScene.SaveGLTF(settings.OutputPath + Path.GetFileNameWithoutExtension(settings.InputModelFile.Name) + ".gltf");
        }


        // MOve this into skeleton/animation builder
        void GenerateAnimations(RmvToGltfExporterSettings settings, ProcessedGltfSkeleton gltfSkeleton, ModelRoot outputScene, AnimationFile animSkeletonFile, bool doMirror)
        {
            //for (int iAnim = 0; iAnim < settings.InputAnimationFiles.Count; iAnim++)
            {
                var animAnimationFile = AnimationFile.Create(settings.InputAnimationFiles[0]);

                var animBuilder = new GltfAnimationCreator(gltfSkeleton, animSkeletonFile);
                animBuilder.CreateFromTWAnim(outputScene, animAnimationFile, doMirror);
            }
        }

        AnimationFile FetchAnimSkeleton(RmvFile rmv2)
        {
            var skeletonName = rmv2.Header.SkeletonName + ".anim";
            var skeletonSearchList = _packFileService.SearchForFile(skeletonName);
            var skeletonPath = _packFileService.GetFullPath(_packFileService.FindFile(skeletonSearchList[0]));
            var skeletonPackFile = _packFileService.FindFile(skeletonPath);

            var animSkeletonFile = AnimationFile.Create(skeletonPackFile);
            return animSkeletonFile;
        }

    }
}
