using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Misc;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public class RmvToGltfExporter
    {
        private readonly ILogger _logger = Logging.Create<RmvToGltfExporter>();
        private readonly PackFileService _packFileService;
        private readonly IGltfSceneSaver _gltfSaver;
        private readonly GltfMeshBuilder _gltfMeshBuilder;
        private readonly IGltfTextureHandler _gltfTextureHandler;

        public RmvToGltfExporter(PackFileService packFileSerivce, IGltfSceneSaver gltfSaver, GltfMeshBuilder gltfMeshBuilder, IGltfTextureHandler gltfTextureHandler)
        {
            _packFileService = packFileSerivce;
            _gltfSaver = gltfSaver;
            _gltfMeshBuilder = gltfMeshBuilder;
            _gltfTextureHandler = gltfTextureHandler;
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
            LogSettings(settings);

            var rmv2 = new ModelFactory().Load(settings.InputModelFile.DataSource.ReadData());
            var hasSkeleton = string.IsNullOrWhiteSpace(rmv2.Header.SkeletonName) == false;
            ProcessedGltfSkeleton? gltfSkeleton = null;

            var outputScene = ModelRoot.CreateModel();

            if (hasSkeleton)
            {
                var animSkeletonFile = FetchAnimSkeleton(rmv2);
                gltfSkeleton = GltfSkeletonCreator.Create(outputScene, animSkeletonFile, settings.MirrorMesh);

                if (settings.ExportAnimations && settings.InputAnimationFiles.Any())
                    GenerateAnimations(settings, gltfSkeleton, outputScene, animSkeletonFile);
            }

            var textures = _gltfTextureHandler.HandleTextures(rmv2, settings);
            var meshes = _gltfMeshBuilder.Build(rmv2, textures, settings);

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

            _gltfSaver.Save(outputScene, settings.OutputPath);
        }

        // MOve this into skeleton/animation builder
        void GenerateAnimations(RmvToGltfExporterSettings settings, ProcessedGltfSkeleton gltfSkeleton, ModelRoot outputScene, AnimationFile animSkeletonFile)
        {
            //for (int iAnim = 0; iAnim < settings.InputAnimationFiles.Count; iAnim++)
            {
                var animAnimationFile = AnimationFile.Create(settings.InputAnimationFiles[0]);

                var animBuilder = new GltfAnimationCreator(gltfSkeleton, animSkeletonFile);
                animBuilder.CreateFromTWAnim(outputScene, animAnimationFile, settings.MirrorMesh);
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

        void LogSettings(RmvToGltfExporterSettings settings)
        {
            var str = $"Exporting using {nameof(RmvToGltfExporter)}\n";
            str += $"\tInputModelFile:{settings.InputModelFile?.Name}\n";
            str += $"\tInputAnimationFiles:{settings.InputAnimationFiles?.Count()}\n";
            str += $"\tOutputPath:{settings.OutputPath}\n";
            str += $"\tConvertMaterialTextureToBlender:{settings.ConvertMaterialTextureToBlender}\n";
            str += $"\tConvertNormalTextureToBlue:{settings.ConvertNormalTextureToBlue}\n";
            str += $"\tExportAnimations:{settings.ExportAnimations}\n";
            str += $"\tMirrorMesh:{settings.MirrorMesh}\n";
            

            _logger.Here().Information(str);
        }
    }
}
