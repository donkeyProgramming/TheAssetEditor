using System.IO;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileBrowser;

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

        public void ShowDialog(Shared.Ui.BaseDialogs.PackFileBrowser.TreeNode clickedNode) =>
                _uiCommandFactory.Create<DisplayImportFileToolCommand>().Execute(clickedNode);
    }
}
