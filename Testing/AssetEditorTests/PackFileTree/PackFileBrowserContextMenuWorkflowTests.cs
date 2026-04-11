using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace AssetEditorTests.PackFileTree
{
    [TestClass]
    public class PackFileBrowserContextMenuWorkflowTests
    {
        [TestMethod]
        public void CopyRenameDeleteWorkflow_UsesContextMenuCommands()
        {
            var eventHub = new FakeDualEventHub();
            var packFileService = CreatePackFileService(eventHub);
            var settingsService = new ApplicationSettingsService();
            var loader = new PackFileContainerLoader(settingsService);

            var caContainer = new PackFileContainer("CA")
            {
                IsCaPackFile = true,
                SystemFilePath = @"c:\\game\\ca.pack"
            };
            packFileService.AddContainer(caContainer, false);

            var karlFolderPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Data", "Karl_and_celestialgeneral_Pack"));
            var sourceContainer = loader.LoadSystemFolderAsPackFileContainer(karlFolderPath);
            packFileService.AddContainer(sourceContainer, false);

            var editableContainer = packFileService.CreateNewPackFileContainer("TestOutput", PackFileVersion.PFH5, PackFileCAType.MOD, true);

            var treeView = new PackFileBrowserViewModel(
                settingsService,
                new EmptyContextMenuBuilder(),
                packFileService,
                eventHub,
                null,
                showCaFiles: true,
                showFoldersOnly: false);

            var standardDialogs = new FakeStandardDialogs
            {
                RenameResult = "skeleton2"
            };

            var copyCommand = new CopyToEditablePackCommand(packFileService);
            var renameCommand = new OnRenameNodeCommand(packFileService, standardDialogs);
            var deleteCommand = new DeleteNodeCommand(packFileService, standardDialogs);

            var sourceRoot = treeView.Files.First(x => ReferenceEquals(x.FileOwner, sourceContainer));
            sourceRoot.IsNodeExpanded = true;

            var sourceAnimations = treeView.GetFromPath(sourceRoot, "animations");
            Assert.IsNotNull(sourceAnimations, "Could not find source animations folder in tree view");

            var sourceAnimationFileCount = CountFilesWithPrefix(sourceContainer, "animations\\");
            var editableAnimationFileCountBefore = CountFilesWithPrefix(editableContainer, "animations\\");

            ExecuteOnSta(() => copyCommand.Execute(sourceAnimations));

            var editableAnimationFileCountAfterCopy = CountFilesWithPrefix(editableContainer, "animations\\");
            Assert.AreEqual(
                editableAnimationFileCountBefore + sourceAnimationFileCount,
                editableAnimationFileCountAfterCopy,
                "Copy to editable pack did not move expected number of animation files");

            var editableRoot = treeView.Files.First(x => ReferenceEquals(x.FileOwner, editableContainer));
            editableRoot.IsNodeExpanded = true;

            var editableAnimations = treeView.GetFromPath(editableRoot, "animations");
            Assert.IsNotNull(editableAnimations, "Could not find editable animations folder after copy");
            editableAnimations.IsNodeExpanded = true;

            var skeletonFolder = treeView.GetFromPath(editableRoot, @"animations\skeletons");
            Assert.IsNotNull(skeletonFolder, "Could not find skeletons folder in editable pack");

            renameCommand.Execute(skeletonFolder);

            var renamedSkeletonFolder = treeView.GetFromPath(editableRoot, @"animations\skeleton2");
            Assert.IsNotNull(renamedSkeletonFolder, "Rename command did not rename skeletons folder to skeleton2 in tree view");
            renamedSkeletonFolder.IsNodeExpanded = true;

            var fileNode = treeView.GetFromPath(editableRoot, @"animations\skeleton2\humanoid01e.anim");
            Assert.IsNotNull(fileNode, "Could not locate humanoid01e.anim in renamed folder before delete");

            deleteCommand.Execute(fileNode);

            var fileInPackOldPath = packFileService.FindFile(@"animations\skeletons\humanoid01e.anim", editableContainer);
            var fileInPackNewPath = packFileService.FindFile(@"animations\skeleton2\humanoid01e.anim", editableContainer);
            Assert.IsNull(fileInPackOldPath, "Deleted file still exists in editable pack at old path");
            Assert.IsNull(fileInPackNewPath, "Deleted file still exists in editable pack at new path");

            // This assertion currently fails: the deleted file can remain visible in tree view.
            var fileInTree = treeView.GetFromPath(editableRoot, @"animations\skeleton2\humanoid01e.anim");
            Assert.IsNull(fileInTree, "Deleted file still exists in tree view");
        }

        private static IPackFileService CreatePackFileService(IGlobalEventHub globalEventHub)
        {
            var concreteType = typeof(IPackFileService).Assembly.GetType("Shared.Core.PackFiles.PackFileService");
            if (concreteType == null)
                throw new InvalidOperationException("Could not find Shared.Core.PackFiles.PackFileService type");

            var instance = Activator.CreateInstance(concreteType, globalEventHub) as IPackFileService;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate Shared.Core.PackFiles.PackFileService");

            return instance;
        }

        private static int CountFilesWithPrefix(PackFileContainer container, string prefix)
        {
            return container.FileList.Keys.Count(x => x.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase));
        }

        private static void ExecuteOnSta(Action action)
        {
            Exception? thrown = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    thrown = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (thrown != null)
                throw new Exception("Failed to run command on STA thread", thrown);
        }

        private sealed class FakeStandardDialogs : IStandardDialogs
        {
            public string RenameResult { get; set; } = string.Empty;

            public SaveDialogResult DisplaySaveDialog(IPackFileService pfs, List<string> extensions) => throw new NotImplementedException();
            public BrowseDialogResultFile DisplayBrowseDialog(List<string> extensions) => throw new NotImplementedException();
            public BrowseDialogResultFolder DisplayBrowseFolderDialog() => throw new NotImplementedException();
            public void ShowExceptionWindow(Exception e, string userInfo = "") => throw new NotImplementedException();
            public void ShowErrorViewDialog(string title, Shared.Core.ErrorHandling.ErrorList errorItems, bool modal = true) => throw new NotImplementedException();
            public TextInputDialogResult ShowTextInputDialog(string title, string initialText = "") => new(true, RenameResult);
            public void ShowDialogBox(string message, string title = "Error") { }
            public ShowMessageBoxResult ShowYesNoBox(string message, string title) => ShowMessageBoxResult.OK;
        }

        private sealed class EmptyContextMenuBuilder : IContextMenuBuilder
        {
            public ContextMenuType Type => ContextMenuType.None;
            public ObservableCollection<ContextMenuItem2> Build(TreeNode? node) => [];
        }

        private sealed class FakeDualEventHub : IEventHub, IGlobalEventHub
        {
            private readonly Dictionary<Type, List<Action<object>>> _handlers = new();

            public void PublishGlobalEvent<T>(T e)
            {
                if (_handlers.TryGetValue(typeof(T), out var handlers))
                {
                    foreach (var handler in handlers)
                        handler(e!);
                }
            }

            public void Publish<T>(T e) => PublishGlobalEvent(e);

            public void Register<T>(object owner, Action<T> action)
            {
                if (!_handlers.TryGetValue(typeof(T), out var list))
                {
                    list = [];
                    _handlers[typeof(T)] = list;
                }

                list.Add(e => action((T)e));
            }

            public void UnRegister(object owner)
            {
            }
        }
    }
}
