using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Schema2;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using System.IO;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileBrowser;


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

            var importedFileName = GetImporedtPackFilePath(settings);

            var rmv2File = RmvMeshBuilder.Build(settings, _modelRoot);
            var bytesRmv2 = ModelFactory.Create().Save(rmv2File);
            var packFileImported = new PackFile(importedFileName, new MemorySource(bytesRmv2));

            _packFileService.AddFileToPack(settings.destinationPackFolder.FileOwner, settings.destinationPackFolder.GetFullPath(), packFileImported);
        }

        private static string GetImporedtPackFilePath(GltfImporterSettings settings)
        {
            var nodePath = ""; 
            var fileName = Path.GetFileNameWithoutExtension(settings.InputGltfFile);
            string importedFileName = $@"{nodePath}/{fileName}.rigid_model_v2";

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
