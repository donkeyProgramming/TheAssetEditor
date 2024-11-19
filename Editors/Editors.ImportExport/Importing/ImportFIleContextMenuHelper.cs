using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Editors.ImportExport.Exporting;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
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
