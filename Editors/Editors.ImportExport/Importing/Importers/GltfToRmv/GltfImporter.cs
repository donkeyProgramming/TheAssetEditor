using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Schema2;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using System.IO;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using static Shared.Core.PackFiles.PackFileService;


namespace Editors.ImportExport.Importing.Importers.GltfToRmv
{
    public record GltfImporterSettings
    (
        string InputGltfFile,
        bool ConvertNormalTextureToOrnge,
        TreeNode destinationPackFolder
     );

    public class GltfImporter
    {
        private readonly PackFileService _packFileService;

        public GltfImporter(PackFileService packFileSerivce)
        {
            _packFileService = packFileSerivce;
        }

        private ModelRoot? _modelRoot;
        public void Import(GltfImporterSettings settings)
        {
            _modelRoot = ModelRoot.Load(settings.InputGltfFile);

            var importedFileName = GetImportedPackFileName(settings);

            var rmv2File = RmvMeshBuilder.Build(settings, _modelRoot);
            var bytesRmv2 = ModelFactory.Create().Save(rmv2File);
            var packFileImported = new PackFile(importedFileName, new MemorySource(bytesRmv2));

            var newFile = new NewFileEntry(settings.destinationPackFolder.GetFullPath(), packFileImported);
            _packFileService.AddFilesToPack(settings.destinationPackFolder.FileOwner, [newFile]);
        }

        private static string GetImportedPackFileName(GltfImporterSettings settings)
        {            
            var fileName = Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            string importedFileName = $@"{fileName}.rigid_model_v2";

            return importedFileName;
        }

        private static void TESTING_SaveTestFileToDisk(GltfImporterSettings settings, byte[] bytes)
        {
            var docsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var fileNameOnly = Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            var modelStream = File.Open($@"{docsFolder}\{fileNameOnly}.rigid_model_v2", FileMode.Create);
            var binaryWriter = new BinaryWriter(modelStream);
            binaryWriter.Write(bytes);
        }
    }
}
