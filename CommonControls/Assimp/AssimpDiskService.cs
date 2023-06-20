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

        static public string GetDialogFilterStringSupportedFormats()
        {
            var unmangedLibrary = Assimp.Unmanaged.AssimpLibrary.Instance;
            var suportetFileExtensions = unmangedLibrary.GetExtensionList();

            var filter = "3d Models (ALL)|";
            // Example: \"Image files (*.bmp, *.jpg)|*.bmp;*.jpg|All files (*.*)|*.*\"'

            // All model formats in one
            foreach (var ext in suportetFileExtensions)
            {
                filter += "*" + ext + ";";
            }

            // ech model format separately
            foreach (var ext in suportetFileExtensions)
            {
                filter += "|" + ext.Remove(0, 1) + "(" + ext + ")|" + "*" + ext;
            }

            filter += "|All files(*.*) | *.*";

            return filter;
        }
    }
}
