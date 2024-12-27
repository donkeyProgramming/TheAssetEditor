using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.EmbeddedResources;
using Shared.Ui.Events.UiCommands;

namespace Editors.KitbasherEditor.DevConfig
{
    internal class Kitbash_Ox : IDeveloperConfiguration
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IUiCommandFactory _uiCommandFactory;

        public Kitbash_Ox(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _uiCommandFactory = uiCommandFactory;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\ox1\chd\chd_taurus\chd_taurus_cinderbreath_02.rigid_model_v2");
            _uiCommandFactory.Create<OpenEditorCommand>().Execute(file);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\cinderbreath.pack";

            var container = _packFileContainerLoader.Load(packFile);
            container.IsCaPackFile = true;
            _packFileService.AddContainer(container);
        }
    }
    /*
    internal class KitBash_Export : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly RmvToGltfExporter _exporter;

        public KitBash_Export(PackFileService packFileService, RmvToGltfExporter exporter)
        {
            _packFileService = packFileService;
            _exporter = exporter;
        }

        public void OpenFileOnLoad()
        {
            var meshPackFile = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");
           var animPackFile = _packFileService.FindFile(@"animations\battle\humanoid01\subset\skeleton_warriors\sword_and_shield\combat_idles\hu1_sk_sws_combat_idle_03.anim");
         
            // obtains user's document folder 
            var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var destPath = $"{documentPath}\\AE_Export_Handedness\\";

            // clear folder, if it exists
            DirectoryInfo dir = new DirectoryInfo(destPath);
            if (dir.Exists)
            {
                foreach (FileInfo file in dir.GetFiles())
                {
                    file.Delete();
                }
            }

            System.IO.Directory.CreateDirectory(destPath);

            var settings = new RmvToGltfExporterSettings(meshPackFile, new List<PackFile>() { animPackFile }, null,  destPath, true, true, true);
            _exporter.Export(settings);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.LoadCaPacksByDefault = true;
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;            
        }
    }*/
}

