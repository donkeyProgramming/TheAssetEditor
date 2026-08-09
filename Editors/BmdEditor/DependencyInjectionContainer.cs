using Editors.BmdEditor.ContextMenu;
using Editors.BmdEditor.ViewModels;
using Editors.BmdEditor.Views;
using Editors.BmdEditor.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;
using Shared.Core.ErrorHandling;
using Shared.Core.Services;
using Shared.Core.Events;
using GameWorld.Core.Services;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.WpfWindow;
using GameWorld.Core.Components.Selection;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;

namespace Editors.BmdEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Views
            serviceCollection.AddTransient<BmdEditorView>();

            // ViewModels
            serviceCollection.AddScoped<BmdEditorViewModel>();
            serviceCollection.AddScoped<IEditorInterface, BmdEditorViewModel>();

            // Services
            serviceCollection.AddScoped<BmdSceneCreator>();
            serviceCollection.AddScoped<SelectionManager>();
            serviceCollection.AddScoped<BmdElementLoader>();

            // Game components (picked up by IComponentInserter)
            RegisterGameComponent<BmdGizmoComponent>(serviceCollection);

            // Context menu
            serviceCollection.AddScoped<ExportBmdAsTerryProjectCommand>();
            serviceCollection.AddSingleton<IPackFileContextMenuRegistration, BmdPackFileContextMenuRegistration>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase editorDatabase)
        {
            EditorInfoBuilder
                .Create<BmdEditorViewModel, BmdEditorView>(EditorEnums.BMD_Editor)
                .AddExtention(".bmd", EditorPriorites.Default)
                //.AddExtention(".bin", EditorPriorites.Default) // TODO: Re-enable when BMD parser is complete
                .Build(editorDatabase);
        }
    }

    public class BmdPackFileContextMenuRegistration : IPackFileContextMenuRegistration
    {
        public void Register(PackFileContextMenuRegistry registry)
        {
            registry.RegisterPackFileContextMenuItem<ExportBmdAsTerryProjectCommand>(ContextMenuType.MainApplication, path: "Export", priority: 40, ContextMenuCluster.Export);
        }
    }
}
