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

namespace Editors.BmdEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Views
            serviceCollection.AddTransient<BmdEditorView>();
            serviceCollection.AddTransient<BmdSceneView>();
            serviceCollection.AddTransient<Bmd3DSceneViewer>();

            // ViewModels
            serviceCollection.AddScoped<BmdEditorViewModel>();
            serviceCollection.AddScoped<BmdSceneViewModel>();
            serviceCollection.AddScoped<IEditorInterface, BmdEditorViewModel>();

            // Services
            serviceCollection.AddScoped<BmdSceneCreator>();
            serviceCollection.AddScoped<SelectionManager>();
            serviceCollection.AddScoped<BmdElementLoader>();

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
}
