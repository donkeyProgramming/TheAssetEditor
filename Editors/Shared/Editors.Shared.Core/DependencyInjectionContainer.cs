using AnimationEditor.Common.BaseControl;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editors.Shared.Core
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<SceneObjectEditor>();
            serviceCollection.AddTransient<SceneObject>();
            serviceCollection.AddScoped<AnimationPlayerViewModel>();
            serviceCollection.AddScoped<SceneObjectViewModelBuilder>();
            serviceCollection.AddScoped<EditorHostView>();

            serviceCollection.AddScoped<IEditorHostParameters, EditorHostParameters>();


            serviceCollection.AddSingleton<SkeletonAnimationLookUpHelper>();;
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
        }
    }
}
