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
using Shared.GameFormats.Bmd;
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
            serviceCollection.AddTransient<BmdEditorView>();

            serviceCollection.AddScoped<BmdEditorViewModel>();
            serviceCollection.AddScoped<IEditorInterface, BmdEditorViewModel>();

            serviceCollection.AddScoped<BmdSceneCreator>();
            serviceCollection.AddScoped<SelectionManager>();
            serviceCollection.AddScoped<BmdElementLoader>();

            // Game components (picked up by IComponentInserter)
            RegisterGameComponent<BmdGizmoComponent>(serviceCollection);

            serviceCollection.AddScoped<ExportBmdAsTerryProjectCommand>();
            serviceCollection.AddSingleton<IPackFileContextMenuRegistration, BmdPackFileContextMenuRegistration>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase editorDatabase)
        {
            // Folder rules are path-substring checks that apply to every extension below, so ".bmd" keeps
            // prefabs and the name parts keep only the battle-map BMDs among .bin files (see BmdFileNames).
            var builder = EditorInfoBuilder
                .Create<BmdEditorViewModel, BmdEditorView>(EditorEnums.BMD_Editor)
                .AddExtention(BmdFileNames.BmdExtension, EditorPriorites.Default)
                .AddExtention(BmdFileNames.BinExtension, EditorPriorites.Default)
                .ValidForFoldersContaining(BmdFileNames.BmdExtension);
            foreach (var namePart in BmdFileNames.BinNameParts)
                builder.ValidForFoldersContaining(namePart);
            builder.Build(editorDatabase);
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
