using System.Linq;
using Editors.Shared.DevConfig.Base;
using KitbasherEditor.ViewModels.UiCommands;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.EmbeddedResources;
using Shared.Ui.Events.UiCommands;

namespace Editors.Shared.DevConfig.Configs
{
    internal class Kitbash_Karl : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;

        public Kitbash_Karl(PackFileService packFileService, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");
            _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Karl_and_celestialgeneral.pack";
            _packFileService.Load(packFile, false, true);
        }
    }

    internal class Kitbash_Ox : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;

        public Kitbash_Ox(PackFileService packFileService, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\ox1\chd\chd_taurus\chd_taurus_cinderbreath_02.rigid_model_v2");
            _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\cinderbreath.pack";
            _packFileService.Load(packFile, false, true);
        }
    }

    internal class Kitbash_Rat : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly ScopeRepository _scopeRepositor;

        public Kitbash_Rat(PackFileService packFileService, IUiCommandFactory uiCommandFactory, ScopeRepository scopeRepositor)
        {
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
            _scopeRepositor = scopeRepositor;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu17\skv\skv_throt\skv_throt_body_01.rigid_model_v2");
            _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);

            var kitbashToolScope = _scopeRepositor.Scopes.First().Value;
            var localCommandFactory = kitbashToolScope.ServiceProvider.GetRequiredService<IUiCommandFactory>();
            localCommandFactory.Create<ImportReferenceMeshCommand>().Execute("variantmeshes\\wh_variantmodels\\hu17\\skv\\skv_throt\\skv_throt_head_01.wsmodel");
            localCommandFactory.Create<ImportReferenceMeshCommand>().Execute("variantmeshes\\wh_variantmodels\\hu17\\skv\\skv_props\\skv_throt_ratcatcher_1h_rigid_01.wsmodel");
            localCommandFactory.Create<ImportReferenceMeshCommand>().Execute("variantmeshes\\wh_variantmodels\\hu17\\skv\\skv_props\\skv_warpstone_1h_01.rigid_model_v2");
            localCommandFactory.Create<ImportReferenceMeshCommand>().Execute("variantmeshes\\wh_variantmodels\\hu17\\skv\\skv_props\\skv_warpstone_1h_02.rigid_model_v2");
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Throt.pack";
            _packFileService.Load(packFile, false, true);
        }
    }

    internal class Kitbash_RomeShield : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;

        public Kitbash_RomeShield(PackFileService packFileService, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"variantmeshes\_variantmodels\man\shield\celtic_oval_shield_a.rigid_model_v2");
            _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file!);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.LoadCaPacksByDefault = false;
            currentSettings.CurrentGame = GameTypeEnum.Rome_2;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Rome_Man_And_Shield_Pack";
            _packFileService.LoadFolderContainer(packFile);
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
