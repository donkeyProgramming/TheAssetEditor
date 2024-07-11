using CommonControls.Editors.BoneMapping.View;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.BaseDialogs.ToolSelector;
using Shared.Ui.BaseDialogs.WindowHandling;
using Shared.Ui.Editors.BoneMapping;
using Shared.Ui.Editors.TextEditor;
using Shared.Ui.Editors.VariantMeshDefinition;
using Shared.Ui.Editors.Wtui;
using Shared.Ui.Events.UiCommands;

namespace Shared.Ui
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection services)
        {
            services.AddTransient<ImportAssetCommand>();
            services.AddTransient<DuplicateCommand>();

            services.AddTransient<IWindowFactory, WindowFactory>();
            services.AddScoped<BoneMappingView>();
            services.AddScoped<BoneMappingViewModel>();

            // Implement required interfaces
            services.AddTransient<IPackFileUiProvider, PackFileUiProvider>();
            services.AddTransient<IToolSelectorUiProvider, ToolSelectorUiProvider>();

            // Editors that should be moved into their own projects
            TextEditor_DependencyInjectionContainer.Register(services);
            VariantMeshDefinition_DependencyInjectionContainer.Register(services);
            TwUi_DependencyInjectionContainer.Register(services);
        }

        public override void RegisterTools(IToolFactory factory)
        {
            TextEditor_DependencyInjectionContainer.RegisterTools(factory);
            VariantMeshDefinition_DependencyInjectionContainer.RegisterTools(factory);
            TwUi_DependencyInjectionContainer.RegisterTools(factory);
        }
    }
}
