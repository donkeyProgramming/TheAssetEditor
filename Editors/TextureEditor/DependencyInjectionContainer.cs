using Editors.TextureEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
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
            serviceCollection.AddTransient<IEditorViewModel, TextureEditorViewModel>();
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            var editorInfo = EditorInfo.Create<TextureEditorViewModel, TexturePreviewView>(EditorEnums.Texture_Editor, new ExtensionToTool([".dds", ".png", ".jpeg"]));
            factory.Register(editorInfo);
        }
    }
}
