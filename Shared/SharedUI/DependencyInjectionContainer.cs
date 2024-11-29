using CommonControls.Editors.BoneMapping.View;
using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileBrowser;
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileBrowser.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.ToolSelector;
using Shared.Ui.BaseDialogs.WindowHandling;
using Shared.Ui.Common.Exceptions;
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
            services.AddTransient<IWindowFactory, WindowFactory>();
            services.AddScoped<BoneMappingView>();
            services.AddScoped<BoneMappingViewModel>();

            // Implement required interfaces
            services.AddScoped<IPackFileUiProvider, PackFileUiProvider>();
            services.AddTransient<IToolSelectorUiProvider, ToolSelectorUiProvider>();
            services.AddTransient<ICustomExceptionWindowProvider, CustomExceptionWindowProvider>();
            
            services.AddTransient<VariantMeshToXmlConverter>();
            services.AddTransient<TextEditorViewModel<VariantMeshToXmlConverter>>();

            services.AddTransient<TextEditorView>();
            services.AddTransient<DefaultTextConverter>();
            services.AddTransient<TextEditorViewModel<DefaultTextConverter>>();


            services.AddScoped<PackFileTreeViewBuilder>();

            // Context menu
            services.AddScoped<ContextMenuFactory>();
            services.AddScoped<IContextMenuBuilder, MainApplicationContextMenuBuilder>();
            services.AddScoped<IContextMenuBuilder, SimpleContextMenuBuilder>();
            services.AddScoped<IContextMenuBuilder, NoContextMenuBuilder>();

            services.AddScoped<AdvancedExportCommand>();
            services.AddScoped<AdvancedImportCommand>();
            services.AddScoped<CopyNodePathCommand>();
            services.AddScoped<ClosePackContainerFileCommand>();
            services.AddScoped<CopyToEditablePackCommand>();
            services.AddScoped<CreateFolderCommand>();
            services.AddScoped<CollapseNodeCommand>();
            services.AddScoped<DuplicateFileCommand>();
            services.AddScoped<DeleteNodeCommand>();
            services.AddScoped<ExportToDirectoryCommand>();
            services.AddScoped<ExpandNodeCommand>();
            services.AddScoped<ImportDirectoryCommand>();
            services.AddScoped<ImportFileCommand>();
            services.AddScoped<OnRenameNodeCommand>();
            services.AddScoped<OpenNodeInNotepadCommand>();
            services.AddScoped<OpenNodeInHxDCommand>();
            services.AddScoped<SaveAsPackFileContainerCommand>();
            services.AddScoped<SavePackFileContainerCommand>();
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
            EditorInfoBuilder
                .Create<TextEditorViewModel<VariantMeshToXmlConverter>, TextEditorView>(EditorEnums.XML_VariantMesh_Editor)
                .AddExtention(".variantmeshdefinition", EditorPriorites.High)
                .Build(factory);

            EditorInfoBuilder
                .Create<TextEditorViewModel<DefaultTextConverter>, TextEditorView>(EditorEnums.XML_Editor)
                .AddExtention(".json", EditorPriorites.Default)
                .AddExtention(".xml", EditorPriorites.Default)
                .AddExtention(".txt", EditorPriorites.Default)
                .AddExtention(".wsmodel", EditorPriorites.Default)
                .AddExtention(".xml.material", EditorPriorites.Default)
                .AddExtention(".anm.meta.xml", EditorPriorites.Default)
                .AddExtention(".bmd.xml", EditorPriorites.Default)
                .AddExtention(".csv", EditorPriorites.Default)
                .AddExtention(".bnk.xml", EditorPriorites.Default)
                .Build(factory);
        }
    }
}
