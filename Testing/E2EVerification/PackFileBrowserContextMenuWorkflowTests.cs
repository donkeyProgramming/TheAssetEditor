using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.E2EVerification
{
    internal class PackFileBrowserContextMenuWorkflowTests
    {
        [Test]
        public void CopyRenameDeleteWorkflow_UsesContextMenuCommands()
        {
            var runner = new AssetEditorTestRunner();
            runner.PackFileService.EnforceGameFilesMustBeLoaded = false;
            var packFileService = runner.PackFileService;
            var settingsService = runner.ServiceProvider.GetRequiredService<ApplicationSettingsService>();

            runner.CreateCaContainer();

            var karlFolderPath = PathHelper.GetDataFolder("Data\\Karl_and_celestialgeneral_Pack");
            var sourceContainer = runner.LoadFolderPackFile(karlFolderPath);

            var editableContainer = runner.CreateOutputPack();
            
            runner.Dialogs.Setup(x => x.ShowTextInputDialog(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new TextInputDialogResult(true, "skeleton2"));
            runner.Dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(ShowMessageBoxResult.OK);

            var builder = runner.ServiceProvider.GetRequiredService<PackFileTreeViewFactory>();
            var treeView = builder.Create(ContextMenuType.None, true, false);

            var copyCommand = new CopyToEditablePackCommand(packFileService, runner.Dialogs.Object);
            var renameCommand = new OnRenameNodeCommand(packFileService, runner.Dialogs.Object);
            var deleteCommand = new DeleteNodeCommand(packFileService, runner.Dialogs.Object);

            var sourceRoot = treeView.Files.First(x => ReferenceEquals(x.FileOwner, sourceContainer));
            sourceRoot.IsNodeExpanded = true;

            var sourceAnimations = treeView.GetFromPath(sourceRoot, "animations");
            Assert.That(sourceAnimations, Is.Not.Null, "Could not find source animations folder in tree view");

            var sourceAnimationFileCount = CountFilesWithPrefix(sourceContainer, "animations\\");
            var editableAnimationFileCountBefore = CountFilesWithPrefix(editableContainer, "animations\\");

            copyCommand.Execute(sourceAnimations);
            //ExecuteOnSta(() => copyCommand.Execute(sourceAnimations));

            var editableAnimationFileCountAfterCopy = CountFilesWithPrefix(editableContainer, "animations\\");
            Assert.That(
                editableAnimationFileCountAfterCopy,
                Is.EqualTo(editableAnimationFileCountBefore + sourceAnimationFileCount),
                "Copy to editable pack did not move expected number of animation files");

            var editableRoot = treeView.Files.First(x => ReferenceEquals(x.FileOwner, editableContainer));
            editableRoot.IsNodeExpanded = true;

            var editableAnimations = treeView.GetFromPath(editableRoot, "animations");
            Assert.That(editableAnimations, Is.Not.Null, "Could not find editable animations folder after copy");
            editableAnimations.IsNodeExpanded = true;

            var skeletonFolder = treeView.GetFromPath(editableRoot, @"animations\skeletons");
            Assert.That(skeletonFolder, Is.Not.Null, "Could not find skeletons folder in editable pack");

            renameCommand.Execute(skeletonFolder);

            var renamedSkeletonFolder = treeView.GetFromPath(editableRoot, @"animations\skeleton2");
            Assert.That(renamedSkeletonFolder, Is.Not.Null, "Rename command did not rename skeletons folder to skeleton2 in tree view");
            renamedSkeletonFolder.IsNodeExpanded = true;

            var fileNode = treeView.GetFromPath(editableRoot, @"animations\skeleton2\humanoid01e.anim");
            Assert.That(fileNode, Is.Not.Null, "Could not locate humanoid01e.anim in renamed folder before delete");

            deleteCommand.Execute(fileNode);

            var fileInPackOldPath = packFileService.FindFile(@"animations\skeletons\humanoid01e.anim", editableContainer);
            var fileInPackNewPath = packFileService.FindFile(@"animations\skeleton2\humanoid01e.anim", editableContainer);
            Assert.That(fileInPackOldPath, Is.Null, "Deleted file still exists in editable pack at old path");
            Assert.That(fileInPackNewPath, Is.Null, "Deleted file still exists in editable pack at new path");

            // This assertion currently fails: the deleted file can remain visible in tree view.
            var fileInTree = treeView.GetFromPath(editableRoot, @"animations\skeleton2\humanoid01e.anim");
            Assert.That(fileInTree, Is.Null, "Deleted file still exists in tree view");
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

    }
}

