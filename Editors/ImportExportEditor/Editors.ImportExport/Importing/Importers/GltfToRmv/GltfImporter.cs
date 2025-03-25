using System.IO;
using CommonControls.BaseDialogs.ErrorListDialog;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using GameWorld.Core.Services;
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
        private readonly IPackFileService _packFileService;
        private readonly IStandardDialogs _exceptionService;
        private readonly ISkeletonAnimationLookUpHelper _skeletonLookUpHelper;

        public GltfImporter(IPackFileService packFileSerivce, IStandardDialogs exceptionService, ISkeletonAnimationLookUpHelper skeletonLookUpHelper)
        {
            _packFileService = packFileSerivce;
            _exceptionService = exceptionService;
            _skeletonLookUpHelper = skeletonLookUpHelper;
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

            var importedFileName = GetImportedPackFileName(settings);
            
            var skeletonName = FetchSkeletonIdStringFromScene(modelRoot);
            if (skeletonName == null)
                throw new ArgumentNullException(nameof(skeletonName), "Fatal eroro: This shouldn't be null");

            AnimationFile? skeletonAnimFile = null;
            if (skeletonName.Any())
            {
                skeletonAnimFile = _skeletonLookUpHelper.GetSkeletonFileFromName(skeletonName);
                                
                if (skeletonAnimFile == null)
                {
                    var errorList = new ErrorList();                 
                    errorList.Error("Skeleton Not Found", $"Skeleton named '{skeletonName}' could not be found\nHave you selected the correct game AND loaded all CA Pack Files?");                   
                    
                    ErrorListWindow.ShowDialog("Skeleton Error", errorList);

                    return;
                }
            }

            var rmv2File = RmvMeshBuilder.Build(settings, modelRoot, skeletonAnimFile, skeletonName);
            var bytesRmv2 = ModelFactory.Create().Save(rmv2File);

            var packFileImported = new PackFile(importedFileName, new MemorySource(bytesRmv2));

            var newFile = new NewPackFileEntry(settings.DestinationPackPath, packFileImported);
            _packFileService.AddFilesToPack(settings.DestinationPackFileContainer, [newFile]);
            
        }

        private static string GetImportedPackFileName(GltfImporterSettings settings)
        {
            var fileName = Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            string importedFileName = $@"{fileName}.rigid_model_v2";

            return importedFileName;
        }

        private static string FetchSkeletonIdStringFromScene(ModelRoot modelRoot)
        {
            var nodeSearchResult = modelRoot.LogicalNodes.Where(node => node.Name.StartsWith("//skeleton//"));

            if (nodeSearchResult == null || !nodeSearchResult.Any())
                return "";

            var skeletonName = nodeSearchResult.First().Name.TrimStart("//skeleton//".ToCharArray());

            return skeletonName.ToLower();
        }
    }
}
