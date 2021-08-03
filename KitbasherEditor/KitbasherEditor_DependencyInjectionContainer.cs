using Common;
using CommonControls.Services;
using KitbasherEditor.ViewModels;
using KitbasherEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace KitbasherEditor
{
    public class KitbasherEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<KitbasherView>();
            serviceCollection.AddTransient<KitbasherViewModel>();
            serviceCollection.AddTransient<IKitBashEditor, KitbasherViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<KitbasherViewModel, KitbasherView>(new ExtentionToTool(".rigid_model_v2"));
        }
    }


    public static class KitbashEditor_Debug
    {
        public static void CreateSlayerHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\head\dwf_slayers_head_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }
        public static void CreateSlayerBody(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu3\dwf\dwf_slayers\body\dwf_slayers_body_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }
        public static void CreateLoremasterHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1d\hef\hef_loremaster_of_hoeth\hef_loremaster_of_hoeth_head_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }
        public static void CreateSkavenSlaveHead(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu17\skv\skv_clan_rats\head\skv_clan_rats_head_04.rigid_model_v2");
            creator.OpenFile(packFile);
        }

        public static void CreatePrincessBody(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes/wh_variantmodels/hu1b/hef/hef_princess/hef_princess_body_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }

        public static void CreateOgre(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu13\ogr\ogr_maneater\ogr_maneater_body_01.rigid_model_v2");
            creator.OpenFile(packFile);
        }
    }
}
