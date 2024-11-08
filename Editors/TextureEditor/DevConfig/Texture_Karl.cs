using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.EmbeddedResources;
using Shared.Ui.Events.UiCommands;

namespace Editors.TextureEditor.DevConfig
{
    internal class Texture_Karl : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;

        public Texture_Karl(PackFileService packFileService, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\tex\emp_karl_franz_body_01_base_colour.dds");
            //_uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);
            _uiCommandFactory.Create<OpenEditorCommand>().ExecuteAsWindow(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\tex\emp_karl_franz_body_01_base_colour.dds", 800, 500);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Karl_and_celestialgeneral.pack";
            _packFileService.Load(packFile, false, true);
        }
    }
}
