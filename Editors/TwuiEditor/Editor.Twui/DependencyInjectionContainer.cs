using Editors.Twui.Editor;
using Editors.Twui.Editor.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;

namespace Editors.Twui
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<TwuiEditor>();
            serviceCollection.AddScoped<TwuiMainView>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase editorDatabase)
        {
            EditorInfoBuilder
                .Create<TwuiEditor, TwuiMainView>(EditorEnums.Twui_Editor)
                .AddExtention(".twui.xml", EditorPriorites.High)
                .Build(editorDatabase);
        }
    }
}
