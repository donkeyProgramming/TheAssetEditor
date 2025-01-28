using System.IO;
using CommonControls.BaseDialogs.ErrorListDialog;
using Editors.ImportExport.Common;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using Editors.ImportExport.Misc;
using Editors.Shared.Core.Services;
using GameWorld.Core.SceneNodes;
using Octokit;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Schema2;


namespace Editors.ImportExport.Importing.Importers.GltfToRmv
{
    public class GltfImporter
    {
        private const string Value = "//skeleton//";
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _exceptionService;
        private readonly ILogger _logger = Logging.Create<GltfImporter>();
        private readonly SkeletonAnimationLookUpHelper _skeletonLookUpHelper;
        private readonly RmvMaterialBuilder _materialBuilder;

        public GltfImporter(IPackFileService packFileSerivce, IStandardDialogs exceptionService, SkeletonAnimationLookUpHelper skeletonLookUpHelper, RmvMaterialBuilder materialBuilder)
        {
            _packFileService = packFileSerivce;
            _exceptionService = exceptionService;
            _skeletonLookUpHelper = skeletonLookUpHelper;
            _materialBuilder = materialBuilder;
        }

        public ImportExportSupportEnum CanImportFile(PackFile file)
        {
            if (FileExtensionHelper.IsGltfFile(file.Name))
                return ImportExportSupportEnum.HighPriority;

            return ImportExportSupportEnum.NotSupported;
        }

        private RmvFile? ImportMeshes(GltfImporterSettings settings, ModelRoot modelRoot, AnimationFile? skeletonAnimFile, string skeletonName)
        {
            var importedFileName = GetImportedPackFileName(settings);

            var rmv2File = RmvMeshBuilder.Build(settings, modelRoot, skeletonAnimFile, skeletonName);
            if (rmv2File == null)
                return null;

            return rmv2File;
        }

        private void SaveRmvFileToPack(GltfImporterSettings settings, string importedFileName, RmvFile rmv2File)
        {
            var bytesRmv2 = ModelFactory.Create().Save(rmv2File);

            var packFileImported = new PackFile(importedFileName, new MemorySource(bytesRmv2));
            var newFile = new NewPackFileEntry(settings.DestinationPackPath, packFileImported);

            _packFileService.AddFilesToPack(settings.DestinationPackFileContainer, [newFile]);
        }

        private void ImportMaterials(GltfImporterSettings settings, ModelRoot modelRoot, RmvFile rmv2File)
        {
            _materialBuilder.BuildRmvFileMaterials(settings, modelRoot, rmv2File);
        }

        private void ImportAnimations(GltfImporterSettings settings, ModelRoot modelRoot, AnimationFile? skeletonAnimFile, string skeletonName)
        {
            var fileName = Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            string importedFileName = $@"{fileName}.anim";

            var animFile = AnimationBuilder.Build(new AnimationBuilderSettings(modelRoot, skeletonName, settings.AnimationKeysPerSecond, settings.DestinationPackFileContainer, settings.DestinationPackPath), skeletonAnimFile);

            if (animFile != null)
            {
                var animBytes = AnimationFile.ConvertToBytes(animFile);
                var packFileImported = new PackFile(importedFileName, new MemorySource(animBytes));
                var newFile = new NewPackFileEntry(settings.DestinationPackPath, packFileImported);
                _packFileService.AddFilesToPack(settings.DestinationPackFileContainer, [newFile]);
            }
        }

        public void Import(GltfImporterSettings settings)
        {
            ModelRoot? modelRoot = null;
            try
            {
                modelRoot = ModelRoot.Load(settings.InputGltfFile);
            }
            catch (Exception ex)
            {
                _exceptionService.ShowExceptionWindow(ex);
                return;
            }

            var skeletonData = GetSkeletonData(modelRoot);

            var rmv2File = ImportMeshes(settings, modelRoot, skeletonData.skeletonAnimFile, skeletonData.skeletonName ?? "");
            if (rmv2File == null)
                throw new Exception($"Failed to import mesh, rmv2File == {rmv2File}");

            if (settings.ImportMaterials)
                ImportMaterials(settings, modelRoot, rmv2File);

            if (settings.ImportAnimations)
                ImportAnimations(settings, modelRoot, skeletonData.skeletonAnimFile, skeletonData.skeletonName ?? "");

            if (settings.ImportMeshes)
                SaveRmvFileToPack(settings, GetImportedPackFileName(settings), rmv2File);

        }

        private (string? skeletonName, AnimationFile? skeletonAnimFile) GetSkeletonData(ModelRoot? modelRoot)
        {
            if (modelRoot == null)
                throw new ArgumentException($"Invalid Input: {nameof(modelRoot)} == {modelRoot}");

            var skeletonName = FetchSkeletonIdStringFromScene(modelRoot);

            if (skeletonName == null)
            {
                _logger.Information("Skeleton ID not found in scene, if the model loading is not rigged, then ignore this.");
                return ("", null);
            }

            AnimationFile? skeletonAnimFile = null;
            if (skeletonName != null && skeletonName.Any())
            {
                skeletonAnimFile = _skeletonLookUpHelper.GetSkeletonFileFromName(skeletonName);

                if (skeletonAnimFile == null)
                {
                    var errorList = new ErrorList();
                    errorList.Error("Skeleton Not Found", $"Skeleton named '{skeletonName}' could not be found\nHave you selected the correct game AND loaded all CA Pack Files?");

                    ErrorListWindow.ShowDialog("Skeleton Error", errorList);

                    return (skeletonName = null, skeletonAnimFile = null);
                }
            }

            return (skeletonName, skeletonAnimFile);
        }

        private static string GetImportedPackFileName(GltfImporterSettings settings)
        {
            var fileName = Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            string importedFileName = $@"{fileName}.rigid_model_v2";

            return importedFileName;
        }

        private static string FetchSkeletonIdStringFromScene(ModelRoot modelRoot)
        {
            var nodeSearchResult = modelRoot.LogicalNodes.Where(node => node.Name.StartsWith(Value));

            if (nodeSearchResult == null || !nodeSearchResult.Any())
                return "";

            var skeletonName = nodeSearchResult.First().Name.TrimStart(Value.ToCharArray());

            return skeletonName.ToLower();
        }
    }
}
