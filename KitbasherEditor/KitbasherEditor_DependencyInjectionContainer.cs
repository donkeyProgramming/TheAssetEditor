using _componentManager.ViewModels.MenuBarViews;
using CommonControls.Common;
using CommonControls.Common.MenuSystem;
using CommonControls.Events;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using KitbasherEditor.ViewModels.VertexDebugger;
using KitbasherEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using MonoGame.Framework.WpfInterop;
using View3D;
using View3D.Components.Component;
using View3D.Components.Component.Selection;

namespace KitbasherEditor
{
    public class KitbasherEditor_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Events
            RegisterNotificationHandler<SceneInitializedEvent, KitbasherViewModel>(serviceCollection);
            RegisterNotificationHandler<FileSavedEvent, KitbasherViewModel>(serviceCollection);
            RegisterNotificationHandler<CommandStackChangedEvent, KitbasherViewModel>(serviceCollection);
            RegisterNotificationHandler<SelectionChangedEvent, MenuBarViewModel>(serviceCollection);
            RegisterNotificationHandler<SelectionChangedEvent, VertexDebuggerViewModel>(serviceCollection);
            RegisterNotificationHandler<SelectionChangedEvent, SceneExplorerViewModel>(serviceCollection);
            RegisterNotificationHandler<SelectionChangedEvent, TransformToolViewModel>(serviceCollection);

            // Creators
            serviceCollection.AddScoped<KitbashSceneCreator>();
            serviceCollection.AddScoped<SceneNodeViewFactory>();

            // View models
            serviceCollection.AddScoped<VertexDebuggerViewModel>();
            serviceCollection.AddScoped<KitbasherView>();
            serviceCollection.AddScoped<KitbasherViewModel>();
            serviceCollection.AddScoped<IEditorViewModel, KitbasherViewModel>();
            serviceCollection.AddScoped<SceneExplorerViewModel>();
            serviceCollection.AddScoped<AnimationControllerViewModel>();

            // Menubar 
            serviceCollection.AddScoped<TransformToolViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();
            serviceCollection.AddScoped<GizmoActions>();
            serviceCollection.AddScoped<VisibilityHandler>();
            serviceCollection.AddScoped<GeneralActions>();
            serviceCollection.AddScoped<ToolActions>();

            // Misc
            serviceCollection.AddScoped<WindowKeyboard>();
            serviceCollection.AddScoped<KitbashViewDropHandler>();  
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<KitbasherViewModel, KitbasherView>(new ExtentionToTool(EditorEnums.Kitbash_Editor, new[] { ".rigid_model_v2", ".wsmodel.rigid_model_v2" }/*, new[] { ".wsmodel", ".variantmeshdefinition" }*/));
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
