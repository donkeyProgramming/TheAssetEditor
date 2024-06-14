using System;
using GameWorld.Core.Services;
using GameWorld.Core.Services.SceneSaving;
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
using Shared.Core.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor
{
    public class DependencyInjectionContainer : DependencyContainer
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
            serviceCollection.AddTransient<SaveDialogView>();
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
}
