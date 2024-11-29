using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Schema2;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using System.IO;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using static Shared.Core.PackFiles.IPackFileService;
using Shared.Core.ErrorHandling.Exceptions;


namespace Editors.ImportExport.Importing.Importers.GltfToRmv
{
    public record GltfImporterSettings
    (
        string InputGltfFile,
        bool ConvertNormalTextureToOrnge,
        TreeNode destinationPackNode
     );

    public class GltfImporter
    {
        private readonly IPackFileService _packFileService;
        private readonly IExceptionService _exceptionService;

        public GltfImporter(IPackFileService packFileSerivce, IExceptionService exceptionService)
        {
            _packFileService = packFileSerivce;
            _exceptionService = exceptionService;
        }

        private ModelRoot? _modelRoot;

        public void Import(GltfImporterSettings settings)
        {
            try
            {
                _modelRoot = ModelRoot.Load(settings.InputGltfFile);
            }
            catch (Exception ex)
            {
                _exceptionService.CreateDialog(ex);
                return;
            }

            var importedFileName = GetImportedPackFileName(settings);

            var rmv2File = RmvMeshBuilder.Build(settings, _modelRoot);
            var bytesRmv2 = ModelFactory.Create().Save(rmv2File);

            var packFileImported = new PackFile(importedFileName, new MemorySource(bytesRmv2));

            var newFile = new NewPackFileEntry(settings.destinationPackNode.GetFullPath(), packFileImported);
            _packFileService.AddFilesToPack(settings.destinationPackNode.FileOwner, [newFile]);
        }

        private static string GetImportedPackFileName(GltfImporterSettings settings)
        {
            var fileName = Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            string importedFileName = $@"{fileName}.rigid_model_v2";

            return importedFileName;
        }
    }
}
