using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.StandardDialog;
using Shared.Ui.BaseDialogs.ToolSelector;

namespace Shared.Ui
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection services)
        {
            // Implement required interfaces
            services.AddScoped<IStandardDialogs, StandardDialogs>();
            services.AddTransient<IToolSelectorUiProvider, ToolSelectorUiProvider>();

            services.AddScoped<PackFileTreeViewFactory>();

            // Context menu
            services.AddScoped<ContextMenuFactory>();
            services.AddScoped<IContextMenuBuilder, MainApplicationContextMenuBuilder>();
            services.AddScoped<IContextMenuBuilder, SimpleContextMenuBuilder>();
            services.AddScoped<IContextMenuBuilder, NoContextMenuBuilder>();

            // TODO: Should all be transient?
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
            services.AddScoped<OpenPackInFileExplorerCommand>();
            services.AddScoped<SaveAsPackFileContainerCommand>();
            services.AddScoped<SavePackFileContainerCommand>();
            services.AddScoped<SetAsEditablePackCommand>();
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
        }
    }
}
