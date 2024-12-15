using System.IO;
using Shared.Core.Events;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.External;
using TreeNode = Shared.Ui.BaseDialogs.PackFileTree.TreeNode;

namespace Editors.ImportExport.Importing
{
    public class ImportFileContextMenuHelper : IImportFileContextMenuHelper
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly ApplicationSettingsService _applicationSettings;

        public ImportFileContextMenuHelper(IUiCommandFactory uiCommandFactory, ApplicationSettingsService applicationSettings)
        {
            _uiCommandFactory = uiCommandFactory;
            _applicationSettings = applicationSettings;
        }

        public bool CanImportFile(string filePath)
        {
            if (Path.GetExtension(filePath.ToUpperInvariant()) == new string(".gltf").ToUpperInvariant()) // mess to make sure the extension is case insensitive
            {
                return true;
            }

            return false;
        }

        public void ShowDialog(TreeNode clickedNode) =>
                _uiCommandFactory.Create<DisplayImportFileToolCommand>().Execute(clickedNode);
    }
}
