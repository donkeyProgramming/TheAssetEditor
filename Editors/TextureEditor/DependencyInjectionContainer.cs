using Editors.TextureEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;
using TextureEditor.Views;

namespace Editors.TextureEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<TexturePreviewView>();
            serviceCollection.AddTransient<TextureEditorViewModel>();
            serviceCollection.AddTransient<TextureBuilder>();  
            serviceCollection.AddTransient<IEditorInterface, TextureEditorViewModel>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<TextureEditorViewModel, TexturePreviewView>(EditorEnums.Texture_Editor)
                .AddExtention(".dds", EditorPriorites.Default)
                .AddExtention(".png", EditorPriorites.Default)
                .AddExtention(".jpeg", EditorPriorites.Default)
                .Build(factory);
        }
    }
}
