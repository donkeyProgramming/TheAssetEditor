using System.Diagnostics;
using Editors.Twui.Editor;
using Editors.Twui.Editor.ComponentEditor;
using Editors.Twui.Editor.Presentation;
using Editors.Twui.Editor.Rendering;
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

            serviceCollection.AddScoped<ComponentManger>();
            serviceCollection.AddScoped<TwuiRenderComponent>();
            serviceCollection.AddScoped<TwuiPreviewBuilder>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase editorDatabase)
        {
            if (Debugger.IsAttached)
            {
                EditorInfoBuilder
                    .Create<TwuiEditor, TwuiMainView>(EditorEnums.Twui_Editor)
                    .AddExtention(".twui.xml", EditorPriorites.High)
                    .Build(editorDatabase);
            }
        }
    }
}
