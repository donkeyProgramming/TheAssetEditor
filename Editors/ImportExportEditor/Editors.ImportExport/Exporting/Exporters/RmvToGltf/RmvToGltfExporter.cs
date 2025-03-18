using System.Windows;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Misc;
using GameWorld.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;
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
        private readonly IGltfSceneSaver _gltfSaver;
        private readonly GltfMeshBuilder _gltfMeshBuilder;
        private readonly IGltfTextureHandler _gltfTextureHandler;
        private readonly GltfSkeletonBuilder _gltfSkeletonBuilder;
        private readonly GltfAnimationBuilder _gltfAnimationBuilder;
        private readonly ISkeletonAnimationLookUpHelper _skeletonLookUpHelper;

        public RmvToGltfExporter(IGltfSceneSaver gltfSaver, GltfMeshBuilder gltfMeshBuilder, IGltfTextureHandler gltfTextureHandler, GltfSkeletonBuilder gltfSkeletonsBuilder, GltfAnimationBuilder gltfAnimationCreator, ISkeletonAnimationLookUpHelper skeletonLookUpHelper)
        {
            _gltfSaver = gltfSaver;
            _gltfMeshBuilder = gltfMeshBuilder;
            _gltfTextureHandler = gltfTextureHandler;
            _gltfSkeletonBuilder = gltfSkeletonsBuilder;
            _gltfAnimationBuilder = gltfAnimationCreator;
            _skeletonLookUpHelper = skeletonLookUpHelper;
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsRmvFile(file.Name))
                return ExportSupportEnum.HighPriority;
            if (FileExtensionHelper.IsWsModelFile(file.Name))
                return ExportSupportEnum.NotSupported;  // This should be supported in the future
            return ExportSupportEnum.NotSupported;
        }

        public void Export(RmvToGltfExporterSettings settings)
        {
            LogSettings(settings);

            var rmv2 = new ModelFactory().Load(settings.InputModelFile.DataSource.ReadData());
            var outputScene = ModelRoot.CreateModel();

            ProcessedGltfSkeleton? gltfSkeleton = null;
            if (settings.ExportAnimations && !string.IsNullOrEmpty(rmv2.Header.SkeletonName))
            {
                var skeletonAnimFile = _skeletonLookUpHelper.GetSkeletonFileFromName(rmv2.Header.SkeletonName);
                if (skeletonAnimFile == null)
                {
                    if (MessageBox.Show(
                        "Skeleton file not found, \n(Have you loaded all CA pakcs for the right game?)\n Do you want to continue exporting without skeleton/animations?",
                        "Warning!",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                        return;
                }
                else                
                {
                    gltfSkeleton = _gltfSkeletonBuilder.CreateSkeleton(skeletonAnimFile, outputScene, settings);
                    _gltfAnimationBuilder.Build(skeletonAnimFile, settings, gltfSkeleton, outputScene);
                }
            }

            var textures = _gltfTextureHandler.HandleTextures(rmv2, settings);
            var meshes = _gltfMeshBuilder.Build(rmv2, textures, settings);

            _logger.Here().Information($"MeshCount={meshes.Count()} TextureCount={textures.Count()} Skeleton={gltfSkeleton?.Data.Count}");
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
