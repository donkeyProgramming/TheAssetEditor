using AnimationEditor.Common.BaseControl;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.DevConfig.Base;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;

namespace Editors.Shared.DevConfig
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<SceneObjectBuilder>();
            serviceCollection.AddTransient<SceneObject>();
            serviceCollection.AddScoped<AnimationPlayerViewModel>();
            serviceCollection.AddScoped<SceneObjectViewModelBuilder>();
            serviceCollection.AddScoped<EditorHostView>();

            serviceCollection.AddTransient<DevelopmentConfigurationManager>();
            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }
    }
}
