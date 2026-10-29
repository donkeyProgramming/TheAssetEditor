using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Misc;
using GameWorld.Core.Services;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public class RmvToGltfStaticExporter
    {
        private readonly ILogger _logger = Logging.Create<RmvToGltfStaticExporter>();
        private readonly IGltfSceneSaver _gltfSaver;
        private readonly GltfStaticMeshBuilder _gltfMeshBuilder;
        private readonly IGltfTextureHandler _gltfTextureHandler;
        private readonly IModelAssetResolver _modelAssetResolver;

        public RmvToGltfStaticExporter(
            IGltfSceneSaver gltfSaver,
            GltfStaticMeshBuilder gltfMeshBuilder,
            IGltfTextureHandler gltfTextureHandler,
            IModelAssetResolver? modelAssetResolver = null)
        {
            _gltfSaver = gltfSaver;
            _gltfMeshBuilder = gltfMeshBuilder;
            _gltfTextureHandler = gltfTextureHandler;
            _modelAssetResolver = modelAssetResolver ?? new ModelAssetResolver();
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsRmvFile(file.Name))
                return ExportSupportEnum.Supported;
            if (FileExtensionHelper.IsWsModelFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        public void Export(RmvToGltfExporterSettings settings)
        {
            LogSettings(settings);

            var resolvedAsset = _modelAssetResolver.Resolve(settings.InputModelFile);
            foreach (var diagnostic in resolvedAsset.Diagnostics)
                _logger.Here().Warning(diagnostic);

            var outputScene = ModelRoot.CreateModel();

            var textureSession = new GltfTextureExportSession(collisionSafe: false);
            var textures = _gltfTextureHandler.HandleTextures(resolvedAsset, settings, textureSession);
            var meshes = _gltfMeshBuilder.Build(resolvedAsset, textures, settings);

            _logger.Here().Information($"Static Export - MeshCount={meshes.Count()} TextureCount={textures.Count()}");
            BuildGltfScene(
                meshes,
                settings,
                outputScene,
                textures.Select(x => x.SystemFilePath).ToArray());
        }

        void BuildGltfScene(
            List<IMeshBuilder<MaterialBuilder>> meshBuilders,
            RmvToGltfExporterSettings settings,
            ModelRoot outputScene,
            IReadOnlyCollection<string>? generatedTexturePaths = null)
        {
            var scene = outputScene.UseScene("default");
            foreach (var meshBuilder in meshBuilders)
            {
                var mesh = outputScene.CreateMesh(meshBuilder);
                scene.CreateNode(mesh.Name).WithMesh(mesh);
            }

            _gltfSaver.Save(outputScene, settings.OutputPath, generatedTexturePaths ?? Array.Empty<string>());
        }

        void LogSettings(RmvToGltfExporterSettings settings)
        {
            var str = $"Exporting using {nameof(RmvToGltfStaticExporter)} (Static Mesh Export)\n";
            str += $"\tInputModelFile:{settings.InputModelFile?.Name}\n";
            str += $"\tOutputPath:{settings.OutputPath}\n";
            str += $"\tConvertMaterialTextureToBlender:{settings.ConvertMaterialTextureToBlender}\n";
            str += $"\tConvertNormalTextureToBlue:{settings.ConvertNormalTextureToBlue}\n";
            str += $"\tMirrorMesh:{settings.MirrorMesh}\n";

            _logger.Here().Information(str);
        }
    }
}
