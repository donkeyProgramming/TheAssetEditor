using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using Editors.KitbasherEditor.UiCommands;
using Editors.KitbasherEditor.ViewModels;
using AssetEditor.Services;
using Shared.Core.DependencyInjection;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.Ui.Events.UiCommands;

namespace AssetEditor.Services.Ipc
{
    public class ExternalFileOpenExecutor : IExternalFileOpenExecutor
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly IScopeRepository _scopeRepository;
        private readonly IEditorManager _editorManager;

        public ExternalFileOpenExecutor(IUiCommandFactory uiCommandFactory, IScopeRepository scopeRepository, IEditorManager editorManager)
        {
            _uiCommandFactory = uiCommandFactory;
            _scopeRepository = scopeRepository;
            _editorManager = editorManager;
        }

        public async Task OpenAsync(PackFile file, bool bringToFront, bool openInExistingKitbashTab, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var app = Application.Current;
            if (app?.Dispatcher == null)
            {
                OpenOnUiThread(file, bringToFront, openInExistingKitbashTab);
                return;
            }

            if (app.Dispatcher.CheckAccess())
            {
                OpenOnUiThread(file, bringToFront, openInExistingKitbashTab);
                return;
            }

            await app.Dispatcher.InvokeAsync(() => OpenOnUiThread(file, bringToFront, openInExistingKitbashTab));
        }

        private void OpenOnUiThread(PackFile file, bool bringToFront, bool openInExistingKitbashTab)
        {
            if (openInExistingKitbashTab && CanImportIntoKitbash(file) && TryImportIntoExistingKitbash(file))
            {
                if (bringToFront)
                    BringMainWindowToFront();
                return;
            }

            var command = _uiCommandFactory.Create<OpenEditorCommand>();
            var forceKitbash = ShouldForceKitbash(file);

            if (forceKitbash)
                command.Execute(file, EditorEnums.Kitbash_Editor);
            else
                command.Execute(file);

            if (bringToFront == false)
                return;

            BringMainWindowToFront();
        }

        private void BringMainWindowToFront()
        {
            var app = Application.Current;
            var window = app?.MainWindow ?? app?.Windows.OfType<Window>().FirstOrDefault();
            if (window == null)
                return;

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;

            window.Activate();
            _ = window.Focus();

            if (window.Topmost == false)
            {
                window.Topmost = true;
                window.Topmost = false;
            }

            window.Activate();
        }

        private bool TryImportIntoExistingKitbash(PackFile file)
        {
            var existingKitbash = _editorManager.GetAllEditors().OfType<KitbasherViewModel>().LastOrDefault();
            if (existingKitbash == null)
                return false;

            var localCommandFactory = _scopeRepository.GetRequiredService<IUiCommandFactory>(existingKitbash);
            localCommandFactory.Create<ImportReferenceMeshCommand>().Execute(file);

            SelectEditor(existingKitbash);
            return true;
        }

        private void SelectEditor(IEditorInterface editor)
        {
            if (_editorManager is not EditorManager concreteEditorManager)
                return;

            var index = concreteEditorManager.CurrentEditorsList.IndexOf(editor);
            if (index >= 0)
                concreteEditorManager.SelectedEditorIndex = index;
        }

        public static bool ShouldForceKitbash(PackFile file)
        {
            return string.Equals(file.Extension, ".wsmodel", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(file.Extension, ".variantmeshdefinition", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool CanImportIntoKitbash(PackFile file)
        {
            return string.Equals(file.Extension, ".rigid_model_v2", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(file.Extension, ".wsmodel", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(file.Extension, ".variantmeshdefinition", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
