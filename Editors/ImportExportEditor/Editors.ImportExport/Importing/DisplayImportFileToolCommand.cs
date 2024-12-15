using System.Windows.Forms;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Shared.Core.Events;
using TreeNode = Shared.Ui.BaseDialogs.PackFileTree.TreeNode;

namespace Editors.ImportExport.Importing
{
    public class DisplayImportFileToolCommand : IUiCommand
    {   
        // TODO: ?
   
        private readonly GltfImporter _importer;     


        public DisplayImportFileToolCommand(GltfImporter importer)
        {            
            // TODO: ?
            //_ImportWindowFactory = importWindowFactory;
            _importer = importer;
        }

        public void Execute(TreeNode clickedNode)
        {
            var glftFilePath = GetFileFromDiskDialog();
            if (string.IsNullOrEmpty(glftFilePath))
                return;

            var settings = new GltfImporterSettings(glftFilePath, clickedNode.GetFullPath(), clickedNode.FileOwner, true, true, true);
            _importer.Import(settings);
        }

        private static string GetFileFromDiskDialog()
        {
            string filePath = "";
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "All files (*.*)|*.*|GLTF model files (*.gltf)|*.gltf";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Get the path of specified file
                    filePath = openFileDialog.FileName;
                }
                else
                {
                    filePath = "";
                }
            }

            return filePath;
        }
    }
}
