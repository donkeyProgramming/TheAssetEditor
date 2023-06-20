using System.IO;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;

namespace CommonControls.ModelImportExport
{
    public class AssimpDiskService
    {
        public static void ImportAssimpDiskFileToPack(PackFileService pfs, PackFileContainer container, string parentPackPath, string filePath)
        {
            var fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            var rigidModelExtension = ".rigid_model_v2";
            var outFileName = fileNameNoExt + rigidModelExtension;

            var assimpImporterService = new AssimpImporter(pfs);
            assimpImporterService.ImportScene(filePath);

            var rmv2File = assimpImporterService.MakeRMV2File();
            var factory = ModelFactory.Create();
            var buffer = factory.Save(rmv2File);

            var packFile = new PackFile(outFileName, new MemorySource(buffer));
            pfs.AddFileToPack(container, parentPackPath, packFile);
        }

    }
}
