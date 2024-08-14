using CommonControls.Editors.BoneMapping.View;
using CommonControls.Editors.TextEditor;
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
            services.AddTransient<DuplicateFileCommand>();

            services.AddTransient<IWindowFactory, WindowFactory>();
            services.AddScoped<BoneMappingView>();
            services.AddScoped<BoneMappingViewModel>();

            // Implement required interfaces
            services.AddTransient<IPackFileUiProvider, PackFileUiProvider>();
            services.AddTransient<IToolSelectorUiProvider, ToolSelectorUiProvider>();


            services.AddTransient<VariantMeshToXmlConverter>();
            services.AddTransient<TextEditorViewModel<VariantMeshToXmlConverter>>();

            services.AddTransient<TextEditorView>();
            services.AddTransient<DefaultTextConverter>();
            services.AddTransient<TextEditorViewModel<DefaultTextConverter>>();
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<TextEditorViewModel<VariantMeshToXmlConverter>, TextEditorView>(new ExtensionToTool(EditorEnums.XML_Editor, new[] { ".variantmeshdefinition" }));
            factory.RegisterTool<TextEditorViewModel<DefaultTextConverter>, TextEditorView>(
                new ExtensionToTool(
                    EditorEnums.XML_Editor,
                    [".json", ".xml", ".txt", ".wsmodel", ".xml.material", ".anim.meta.xml", ".anm.meta.xml", ".snd.meta.xml", ".bmd.xml", ".csv", ".bnk.xml"]));
        }
    }
}
