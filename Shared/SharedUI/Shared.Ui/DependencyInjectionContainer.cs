using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Shared.Ui.BaseDialogs.StandardDialog;
using Shared.Ui.BaseDialogs.ToolSelector;
using Shared.Ui.Common.MenuSystem;

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
            services.AddScoped<IWindowsKeyboard, WindowKeyboard>();
            services.AddScoped<IStandardDialogs, StandardDialogs>();
            services.AddSingleton<IFileSystemAccess, FileSystemAccess>();
            services.AddTransient<IToolSelectorUiProvider, ToolSelectorUiProvider>();

            services.AddScoped<PackFileTreeViewFactory>();

            // Context menu
            services.AddSingleton(provider =>
            {
                var registry = new PackFileContextMenuRegistry();
                RegisterPackFileContextMenuItems(registry);

                foreach (var registration in provider.GetServices<IPackFileContextMenuRegistration>())
                    registration.Register(registry);

                return registry;
            });
            services.AddScoped<PackFileContextMenuComposer>();

            // TODO: Should all be transient?
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
            services.AddScoped<RenameNodeCommand>();
            services.AddScoped<OpenNodeInNotepadCommand>();
            services.AddScoped<OpenNodeInHxDCommand>();
            services.AddScoped<OpenPackInFileExplorerCommand>();
            services.AddScoped<SaveAsPackFileContainerCommand>();
            services.AddScoped<SavePackFileContainerCommand>();
            services.AddScoped<SetAsEditablePackCommand>();
        }

        private static void RegisterPackFileContextMenuItems(PackFileContextMenuRegistry registry)
        {
            // MainApplication context menu
            registry.RegisterPackFileContextMenuItem<ClosePackContainerFileCommand>(ContextMenuType.MainApplication, path: "", priority: 0, ContextMenuCluster.PackFileOperation);
            registry.RegisterPackFileContextMenuItem<SetAsEditablePackCommand>(ContextMenuType.MainApplication, path: "", priority: 10, ContextMenuCluster.PackFileOperation);
            registry.RegisterPackFileContextMenuItem<SavePackFileContainerCommand>(ContextMenuType.MainApplication, path: "", priority: 20, ContextMenuCluster.PackFileOperation);
            registry.RegisterPackFileContextMenuItem<SaveAsPackFileContainerCommand>(ContextMenuType.MainApplication, path: "", priority: 30, ContextMenuCluster.PackFileOperation);
            registry.RegisterPackFileContextMenuItem<CopyToEditablePackCommand>(ContextMenuType.MainApplication, path: "", priority: 40, ContextMenuCluster.PackFileOperation);

            registry.RegisterPackFileContextMenuItem<ImportFileCommand>(ContextMenuType.MainApplication, path: "Import", priority: 0, ContextMenuCluster.FolderOperation);
            registry.RegisterPackFileContextMenuItem<ImportDirectoryCommand>(ContextMenuType.MainApplication, path: "Import", priority: 10, ContextMenuCluster.FolderOperation);
            registry.RegisterPackFileContextMenuItem<CreateFolderCommand>(ContextMenuType.MainApplication, path: "Create", priority: 30, ContextMenuCluster.FolderOperation);
            registry.RegisterPackFileContextMenuItem<RenameNodeCommand>(ContextMenuType.MainApplication, path: "", priority: 40, ContextMenuCluster.FolderOperation);
            registry.RegisterPackFileContextMenuItem<DeleteNodeCommand>(ContextMenuType.MainApplication, path: "", priority: 50, ContextMenuCluster.FolderOperation);

            registry.RegisterPackFileContextMenuItem<DuplicateFileCommand>(ContextMenuType.MainApplication, path: "", priority: 0, ContextMenuCluster.FileOperation);
            registry.RegisterPackFileContextMenuItem<RenameNodeCommand>(ContextMenuType.MainApplication, path: "", priority: 10, ContextMenuCluster.FileOperation);
            registry.RegisterPackFileContextMenuItem<DeleteNodeCommand>(ContextMenuType.MainApplication, path: "", priority: 20, ContextMenuCluster.FileOperation);
            registry.RegisterPackFileContextMenuItem<CopyNodePathCommand>(ContextMenuType.MainApplication, path: "", priority: 30, ContextMenuCluster.FileOperation);

            registry.RegisterPackFileContextMenuItem<ExportToDirectoryCommand>(ContextMenuType.MainApplication, path: "Export", priority: 0, ContextMenuCluster.Export);

            registry.RegisterPackFileContextMenuItem<ExpandNodeCommand>(ContextMenuType.MainApplication, path: "", priority: 0, ContextMenuCluster.Misc);
            registry.RegisterPackFileContextMenuItem<CollapseNodeCommand>(ContextMenuType.MainApplication, path: "", priority: 10, ContextMenuCluster.Misc);
            registry.RegisterPackFileContextMenuItem<OpenPackInFileExplorerCommand>(ContextMenuType.MainApplication, path: "", priority: 20, ContextMenuCluster.Misc);
            registry.RegisterPackFileContextMenuItem<OpenNodeInHxDCommand>(ContextMenuType.MainApplication, path: "Open", priority: 30, ContextMenuCluster.Misc);
            registry.RegisterPackFileContextMenuItem<OpenNodeInNotepadCommand>(ContextMenuType.MainApplication, path: "Open", priority: 40, ContextMenuCluster.Misc);

            // Simple context menu
            registry.RegisterPackFileContextMenuItem<ExpandNodeCommand>(ContextMenuType.Simple, path: "", priority: 0, ContextMenuCluster.FolderOperation);
            registry.RegisterPackFileContextMenuItem<CollapseNodeCommand>(ContextMenuType.Simple, path: "", priority: 10, ContextMenuCluster.FolderOperation);
            registry.RegisterPackFileContextMenuItem<CreateFolderCommand>(ContextMenuType.Simple, path: "", priority: 20, ContextMenuCluster.FolderOperation);
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
        }
    }
}
