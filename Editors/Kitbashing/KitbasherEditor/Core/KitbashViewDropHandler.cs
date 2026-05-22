using System.IO;
using Editors.KitbasherEditor.UiCommands;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace Editors.KitbasherEditor.ViewModels
{
    public class KitbashViewDropHandler
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public KitbashViewDropHandler(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public bool AllowDrop(PackFile file, PackFile targeNode = null)
        {
            if (file != null)
            {
                var extension = Path.GetExtension(file.Name).ToLower();
                if (extension == ".rigid_model_v2" || extension == ".wsmodel" || extension == ".variantmeshdefinition")
                    return true;
            }
            return false;
        }

        public bool Drop(PackFile file)
        {
            if (file == null)
                return false;

            _uiCommandFactory.Create<ImportReferenceMeshCommand>(x => x.Configure(file)).Execute();
            return true;
        }
    }
}
