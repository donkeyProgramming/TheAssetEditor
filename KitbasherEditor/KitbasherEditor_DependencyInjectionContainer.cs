using System;
using CommonControls;
using CommonControls.Common;
using CommonControls.Common.MenuSystem;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using KitbasherEditor.EventHandlers;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.MeshFitter;
using KitbasherEditor.ViewModels.PinTool;
using KitbasherEditor.ViewModels.SaveDialog;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using KitbasherEditor.ViewModels.VertexDebugger;
using KitbasherEditor.Views;
using KitbasherEditor.Views.EditorViews;
using KitbasherEditor.Views.EditorViews.PinTool;
using KitbasherEditor.Views.EditorViews.VertexDebugger;
using Microsoft.Extensions.DependencyInjection;
using View3D.Services;
using View3D.Services.SceneSaving;

namespace KitbasherEditor
{
    public class KitbasherEditor_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Creators
            serviceCollection.AddScoped<KitbashSceneCreator>();
            serviceCollection.AddScoped<SceneNodeViewFactory>();

            // View models 
            serviceCollection.AddScoped<KitbasherView>();
            serviceCollection.AddScoped<KitbasherViewModel>();
            serviceCollection.AddScoped<IEditorViewModel, KitbasherViewModel>();
            serviceCollection.AddScoped<SceneExplorerViewModel>();
            serviceCollection.AddScoped<AnimationControllerViewModel>();

            // Sub tools
            serviceCollection.AddScoped<VertexDebuggerViewModel>();
            serviceCollection.AddScoped<VertexDebuggerView>();
            serviceCollection.AddScoped<MeshFitterViewModel>();
            serviceCollection.AddScoped<ReRiggingViewModel>();
            serviceCollection.AddScoped<PinToolView>();
            serviceCollection.AddScoped<PinToolViewModel>();

            // Save dialog
            serviceCollection.AddScoped<SaveDialogViewModel>();
            serviceCollection.AddScoped<SaveDialogView>();
            serviceCollection.AddScoped<SaveSettings>();

            // Menubar 
            serviceCollection.AddScoped<TransformToolViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();
            serviceCollection.AddScoped<MenuItemVisibilityRuleEngine>();

            // Misc
            serviceCollection.AddScoped<WindowKeyboard>();
            serviceCollection.AddScoped<KitbashViewDropHandler>();
            serviceCollection.AddScoped<KitbasherRootScene>();
            serviceCollection.AddScoped<IActiveFileResolver, KitbasherRootScene>(x=>x.GetRequiredService<KitbasherRootScene>());

            // Event handlers
            serviceCollection.AddScoped<SceneInitializedHandler>();
            serviceCollection.AddScoped<SkeletonChangedHandler>();
            
            serviceCollection.AddScoped<IScopeHelper<KitbasherViewModel>, KitbasherScopeHelper>();

            RegisterAllAsOriginalType<IKitbasherUiCommand>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<KitbasherViewModel, KitbasherView>(new ExtensionToTool(EditorEnums.Kitbash_Editor, new[] { ".rigid_model_v2", ".wsmodel.rigid_model_v2" }/*, new[] { ".wsmodel", ".variantmeshdefinition" }*/));
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

        public static void CreatePaladin(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\brt\brt_paladin\head\brt_paladin_head_01.rigid_model_v2");
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
