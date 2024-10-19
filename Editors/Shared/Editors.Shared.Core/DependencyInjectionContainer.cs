using Editors.Shared.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.Shared.Core
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            //serviceCollection.AddTransient<TexturePreviewView>();
            //serviceCollection.AddTransient<TextureEditorViewModel>();
            //serviceCollection.AddTransient<IEditorViewModel, TextureEditorViewModel>();

            serviceCollection.AddSingleton<IAnimationFileDiscovered, SkeletonAnimationLookUpHelper>();
            serviceCollection.AddSingleton<SkeletonAnimationLookUpHelper>((x) => x.GetService<IAnimationFileDiscovered>() as SkeletonAnimationLookUpHelper);
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
          // factory.RegisterTool<TextureEditorViewModel, TexturePreviewView>(new ExtensionToTool(EditorEnums.Texture_Editor, new[] { ".dds", ".png", ".jpeg" }));
        }
    }
}
