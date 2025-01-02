using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;

namespace Editors.Twui.DevConfig
{
    internal class TwuiToolEditor : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IPackFileService _packFileService;

        public TwuiToolEditor(IEditorCreator editorCreator, IPackFileContainerLoader packFileContainerLoader, IPackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileContainerLoader = packFileContainerLoader;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.ShowCAWemFiles = false;
            currentSettings.LoadCaPacksByDefault = true;

            //var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\twui_pack";
            //var container = _packFileContainerLoader.LoadSystemFolderAsPackFileContainer(packFile);
            //container.IsCaPackFile = true;
            //_packFileService.AddContainer(container);
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"ui\campaign ui\dlc25_bog_main.twui.xml");
            //var file = _packFileService.FindFile(@"ui\campaign ui\dlc25_bog_legendary_grudges.twui.xml");
            _editorCreator.CreateFromFile(file, EditorEnums.Twui_Editor);
        }
    }
}
