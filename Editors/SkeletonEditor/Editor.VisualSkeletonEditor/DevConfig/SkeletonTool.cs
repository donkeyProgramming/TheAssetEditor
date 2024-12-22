using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;

namespace Editor.VisualSkeletonEditor.DevConfig
{
    internal class SkeletonTool : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IPackFileService _packFileService;

        public SkeletonTool(IEditorCreator editorCreator, IPackFileContainerLoader packFileContainerLoader, IPackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileContainerLoader = packFileContainerLoader;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.ShowCAWemFiles = false;
            currentSettings.LoadCaPacksByDefault = false;

            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Karl_and_celestialgeneral.pack";
            var container = _packFileContainerLoader.Load(packFile);
            container.IsCaPackFile = true;
            _packFileService.AddContainer(container);
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile("animations\\skeletons\\humanoid01.anim");
            _editorCreator.CreateFromFile(file, EditorEnums.VisualSkeletonEditor);
        }
    }
}
