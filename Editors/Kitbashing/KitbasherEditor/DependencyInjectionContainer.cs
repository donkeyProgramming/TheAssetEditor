using Editors.KitbasherEditor.ChildEditors.MeshFitter;
using Editors.KitbasherEditor.ChildEditors.PinTool;
using Editors.KitbasherEditor.ChildEditors.PinTool.Commands;
using Editors.KitbasherEditor.ChildEditors.ReRiggingTool;
using Editors.KitbasherEditor.ChildEditors.VertexDebugger;
using Editors.KitbasherEditor.Core;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Editors.KitbasherEditor.EventHandlers;
using Editors.KitbasherEditor.Services;
using Editors.KitbasherEditor.UiCommands;
using Editors.KitbasherEditor.ViewModels;
using Editors.KitbasherEditor.ViewModels.PinTool;
using Editors.KitbasherEditor.ViewModels.SaveDialog;
using Editors.KitbasherEditor.ViewModels.SceneExplorer;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2;
using Editors.KitbasherEditor.ViewModels.SceneNodeEditor;
using KitbasherEditor.ViewModels.MenuBarViews;
using KitbasherEditor.ViewModels.SaveDialog;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using KitbasherEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Node views
            serviceCollection.AddTransient<MainEditableNodeViewModel>();
            serviceCollection.AddTransient<MeshEditorViewModel>();
            serviceCollection.AddTransient<SkeletonSceneNodeViewModel>();
            serviceCollection.AddTransient<GroupNodeViewModel>();

            // Creators
            serviceCollection.AddScoped<KitbashSceneCreator>();

            // View models 
            serviceCollection.AddScoped<KitbasherView>();
            serviceCollection.AddScoped<KitbasherViewModel>();
            serviceCollection.AddScoped<IEditorInterface, KitbasherViewModel>();
            serviceCollection.AddScoped<SceneExplorerViewModel>();
            serviceCollection.AddTransient<SceneExplorerContextMenuHandler>();
            serviceCollection.AddScoped<AnimationControllerViewModel>();

            // View models - scene node editors
            serviceCollection.AddTransient<SceneNodeEditorViewModel>();
            serviceCollection.AddTransient<MeshViewModel>();
            serviceCollection.AddTransient<AnimationViewModel>();
            serviceCollection.AddTransient<WeightedMaterialViewModel>();
            serviceCollection.AddTransient<WsMaterialViewModel>();

            // Mesh fitter
            RegisterWindow<MeshFitterWindow>(serviceCollection);
            serviceCollection.AddTransient<MeshFitterViewModel>();

            // Re-Rigging
            serviceCollection.AddTransient<ReRiggingViewModel>();
            RegisterWindow<ReRiggingWindow>(serviceCollection);
            
            // Vertex debugger
            serviceCollection.AddScoped<VertexDebuggerViewModel>();
            RegisterWindow<VertexDebuggerWindow>(serviceCollection);

            // Pin tool
            serviceCollection.AddTransient<PinToolViewModel>();
            RegisterWindow<PinToolWindow>(serviceCollection);
            serviceCollection.AddTransient<PinMeshToVertexCommand>();
            serviceCollection.AddTransient<SkinWrapRiggingCommand>();

            // Save dialog
            serviceCollection.AddTransient<SaveDialogViewModel>();
            RegisterWindow<SaveDialogWindow>(serviceCollection);

            // Menubar 
            serviceCollection.AddScoped<TransformToolViewModel>();
            serviceCollection.AddScoped<MenuBarViewModel>();
            serviceCollection.AddScoped<MenuItemVisibilityRuleEngine>();

            // Misc
            serviceCollection.AddScoped<WindowKeyboard>();
            serviceCollection.AddScoped<KitbashViewDropHandler>();
            serviceCollection.AddScoped<KitbasherRootScene>();

            // Event handlers
            serviceCollection.AddScoped<SkeletonChangedHandler>();

            // Commands
            RegisterAllAsOriginalType<ITransientKitbasherUiCommand>(serviceCollection, ServiceLifetime.Transient);
            RegisterAllAsOriginalType<IScopedKitbasherUiCommand>(serviceCollection, ServiceLifetime.Scoped);
            serviceCollection.AddTransient<CopyTexturesToPackCommand>();
            serviceCollection.AddTransient<ImportReferenceMeshCommand>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);

            // Commands
            serviceCollection.AddTransient<RemapBoneIndexesCommand>();
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<KitbasherViewModel, KitbasherView>(EditorEnums.Kitbash_Editor)
                .AddExtention(".rigid_model_v2", EditorPriorites.High)
                //.AddExtention(".variantmeshdefinition", 0)
                .AddExtention(".wsmodel", EditorPriorites.High)
                .Build(factory);
        }
    }
}
