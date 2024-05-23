using Microsoft.Extensions.DependencyInjection;
using SharedCore.Misc;
using SharedCore.ToolCreation;

namespace Editors.Shared.Core
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
           //serviceCollection.AddTransient<TexturePreviewView>();
           //serviceCollection.AddTransient<TextureEditorViewModel>();
           //serviceCollection.AddTransient<IEditorViewModel, TextureEditorViewModel>();
        }

        public override void RegisterTools(IToolFactory factory)
        {
          // factory.RegisterTool<TextureEditorViewModel, TexturePreviewView>(new ExtensionToTool(EditorEnums.Texture_Editor, new[] { ".dds", ".png", ".jpeg" }));
        }
    }
}
