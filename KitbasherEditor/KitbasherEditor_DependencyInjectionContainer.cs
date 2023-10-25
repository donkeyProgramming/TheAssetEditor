using CommonControls;
using CommonControls.Common;
using CommonControls.Common.MenuSystem;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using KitbasherEditor.EventHandlers;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using KitbasherEditor.ViewModels.UiCommands;
using KitbasherEditor.ViewModels.VertexDebugger;
using KitbasherEditor.Views;
using KitbasherEditor.Views.EditorViews.VertexDebugger;
using Microsoft.Extensions.DependencyInjection;
using System;
using View3D.Services;

namespace KitbasherEditor
{
    public class KitbasherEditor_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // TODO: DEBUGGING BEGIN
            serviceCollection.AddTransient<SaveCommand>();
            serviceCollection.AddTransient<SaveAsCommand>();

            serviceCollection.AddTransient<GenerateWh2WsModelCommand>();
            serviceCollection.AddTransient<GenerateWh3WsModelCommand>();

            serviceCollection.AddTransient<BrowseForReferenceCommand>();
            serviceCollection.AddTransient<ImportPaladinReferenceCommand>();
            serviceCollection.AddTransient<ImportGoblinReferenceCommand>();
            serviceCollection.AddTransient<ImportSlayerReferenceCommand>();

            serviceCollection.AddTransient<DeleteLodsCommand>();
            serviceCollection.AddTransient<ClearConsoleCommand>();
            serviceCollection.AddTransient<UndoCommand>();
            serviceCollection.AddTransient<SortMeshesCommand>();

            serviceCollection.AddTransient<GroupItemsCommand>();
            serviceCollection.AddTransient<ScaleGizmoUpCommand>();
            serviceCollection.AddTransient<ScaleGizmoDownCommand>();
            serviceCollection.AddTransient<SelectGizmoModeCommand>();
            serviceCollection.AddTransient<MoveGizmoModeCommand>();
            serviceCollection.AddTransient<RotateGizmoModeCommand>();
            serviceCollection.AddTransient<ScaleGizmoModeCommand>();

            serviceCollection.AddTransient<ObjectSelectionModeCommand>();
            serviceCollection.AddTransient<FaceSelectionModeCommand>();
            serviceCollection.AddTransient<VertexSelectionModeCommand>();

            serviceCollection.AddTransient<ToggleViewSelectedCommand>();
            serviceCollection.AddTransient<ResetCameraCommand>();
            serviceCollection.AddTransient<FocusCameraCommand>();
            serviceCollection.AddTransient<ToggleBackFaceRenderingCommand>();
            serviceCollection.AddTransient<ToggleLargeSceneRenderingCommand>();

            serviceCollection.AddTransient<DivideSubMeshCommand>();
            serviceCollection.AddTransient<MergeObjectsCommand>();
            serviceCollection.AddTransient<DuplicateObjectCommand>();
            serviceCollection.AddTransient<DeleteObjectCommand>();
            serviceCollection.AddTransient<CreateStaticMeshCommand>();

            serviceCollection.AddTransient<ReduceMeshCommand>();
            serviceCollection.AddTransient<CreateLodCommand>();
            serviceCollection.AddTransient<OpenBmiToolCommand>();
            serviceCollection.AddTransient<OpenSkeletonReshaperToolCommand>();
            serviceCollection.AddTransient<OpenReriggingToolCommand>();
            serviceCollection.AddTransient<OpenPinToolCommand>();
            serviceCollection.AddTransient<CopyRootLodCommand>();
            //CreateActionItem<UpdateWh2TexturesCommand>(x => x.Technique = View3D.Services.Rmv2UpdaterService.BaseColourGenerationTechniqueEnum.AdditiveBlending);
            //CreateActionItem<UpdateWh2TexturesCommand>(x => x.Technique = View3D.Services.Rmv2UpdaterService.BaseColourGenerationTechniqueEnum.ComparativeBlending);

            serviceCollection.AddTransient<ExpandFaceSelectionCommand>();
            serviceCollection.AddTransient<ConvertFaceToVertexCommand>();
            serviceCollection.AddTransient<OpenVertexDebuggerCommand>();

            // TODO: DEBUGGIN END

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
            factory.RegisterTool<KitbasherViewModel, KitbasherView>(new ExtensionToTool(EditorEnums.Kitbash_Editor, new[] { ".rigid_model_v2"/*, ".wsmodel.rigid_model_v2" */}/*, new[] { ".wsmodel", ".variantmeshdefinition" }*/));
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
