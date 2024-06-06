using Editors.Shared.DevConfig.Base;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.EmbeddedResources;
using Shared.Ui.Events.UiCommands;

namespace Editors.Shared.DevConfig.Configs
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
            _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Karl_and_celestialgeneral.pack";
            _packFileService.Load(packFile, false, true);
        }
    }
}




//public static class KitbashEditor_Debug
//{
//    public static void CreateSlayerHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
//    {
//        var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\head\dwf_slayers_head_01.rigid_model_v2");
//        creator.OpenFile(packFile);
//    }
//    public static void CreateSlayerBody(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
//    {
//        var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\body\dwf_slayers_body_01.rigid_model_v2");
//        creator.OpenFile(packFile);
//    }
//    public static void CreateLoremasterHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
//    {
//        var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1d\hef\hef_loremaster_of_hoeth\hef_loremaster_of_hoeth_head_01.rigid_model_v2");
//        creator.OpenFile(packFile);
//    }
//
//    public static void CreatePaladin(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
//    {
//        var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\brt\brt_paladin\head\brt_paladin_head_01.rigid_model_v2");
//        creator.OpenFile(packFile);
//    }
//
//    public static void CreateSkavenSlaveHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
//    {
//        var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu17\skv\skv_clan_rats\head\skv_clan_rats_head_04.rigid_model_v2");
//        creator.OpenFile(packFile);
//    }
//
//    public static void CreatePrincessBody(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
//    {
//        var packFile = packfileService.FindFile(@"variantmeshes/wh_variantmodels/hu1b/hef/hef_princess/hef_princess_body_01.rigid_model_v2");
//        creator.OpenFile(packFile);
//    }
//
//    public static void CreateOgre(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
//    {
//        var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu13\ogr\ogr_maneater\ogr_maneater_body_01.rigid_model_v2");
//        creator.OpenFile(packFile);
//    }
//}
