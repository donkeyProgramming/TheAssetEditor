using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Ui.BaseDialogs.PackFileTree;

namespace Shared.UiTest.BaseDialogs.PackFileTree
{
    [TestFixture]
    internal class SearchFilterTests : PackFileTreeTestBase
    {
        private IPackFileContainer AddPackFiles(string containerName, string systemPath, params string[] files)
            => AddPackFiles(false, containerName, systemPath, files);

        private PackFileBrowserViewModel CreateBrowser()
        {
            var vm = PackFileBrowser();
            vm.Filter.UseDebounce = false;
            return vm;
        }

        [Test]
        public void Filter_MatchingFilesVisible_NonMatchingHidden()
        {
            // Arrange
            var container = AddPackFiles("mod", "c:\\mod.pack",
                "textures\\hero.dds",
                "textures\\villain.dds",
                "models\\hero.mesh",
                "sounds\\music.wem");

            var vm = CreateBrowser();

            // Act
            vm.Filter.FilterText = "hero";

            // Assert
            var root = vm.Files.First();
            var heroTexture = FindFileNode(root, "hero.dds");
            var villainTexture = FindFileNode(root, "villain.dds");
            var heroMesh = FindFileNode(root, "hero.mesh");
            var music = FindFileNode(root, "music.wem");

            Assert.That(heroTexture?.IsVisible, Is.True);
            Assert.That(heroMesh?.IsVisible, Is.True);
            Assert.That(villainTexture?.IsVisible, Is.False);
            Assert.That(music?.IsVisible, Is.False);
        }

        [Test]
        public void Filter_FewResults_AllNodesExpanded()
        {
            // Arrange
            var container = AddPackFiles("mod", "c:\\mod.pack",
                "textures\\hero.dds",
                "models\\hero.mesh");

            var vm = CreateBrowser();
            vm.Filter.AutoExapandResultsAfterLimitedCount = 25;

            // Act
            vm.Filter.FilterText = "hero";

            // Assert
            var root = vm.Files.First();
            Assert.That(root.IsNodeExpanded, Is.True);
            var texturesFolder = root.Children.First(x => x.Name == "textures");
            var modelsFolder = root.Children.First(x => x.Name == "models");
            Assert.That(texturesFolder.IsNodeExpanded, Is.True);
            Assert.That(modelsFolder.IsNodeExpanded, Is.True);
        }

        [Test]
        public void Filter_ManyResults_NodesNotExpanded()
        {
            // Arrange - create enough files to exceed the limit
            var files = Enumerable.Range(0, 30).Select(i => $"folder{i}\\file{i}.txt").ToArray();
            var container = AddPackFiles("mod", "c:\\mod.pack", files);

            var vm = CreateBrowser();
            vm.Filter.AutoExapandResultsAfterLimitedCount = 25;

            // Collapse all nodes first
            var root = vm.Files.First();
            CollapseAll(root);

            // Act - filter that matches all files
            vm.Filter.FilterText = "file";

            // Assert - root should be expanded (ExpandForFilter always expands root-level)
            // but inner folders should NOT be auto-expanded since count > limit
            var anyInnerExpanded = root.Children.Any(c => c.IsNodeExpanded);
            Assert.That(anyInnerExpanded, Is.False);
        }

        [Test]
        public void Filter_PreExpandedFolder_RemainsExpandedAfterFilter()
        {
            // Arrange
            var container = AddPackFiles("mod", "c:\\mod.pack",
                "animations\\combat\\attack.anim",
                "animations\\idle\\stand.anim",
                "textures\\hero.dds");

            var vm = CreateBrowser();
            var root = vm.Files.First();

            // Expand the animations folder before filtering
            var animFolder = root.Children.First(x => x.Name == "animations");
            animFolder.IsNodeExpanded = true;

            // Act - apply a filter that includes animations\combat\attack.anim
            vm.Filter.FilterText = "attack";

            // Assert - animations folder is visible and still expanded
            Assert.That(animFolder.IsVisible, Is.True);
            Assert.That(animFolder.IsNodeExpanded, Is.True);
        }

        [Test]
        public void Filter_ApplyThenExpandThenClear_UserExpandedNodesStayExpanded()
        {
            // Arrange
            var container = AddPackFiles("mod", "c:\\mod.pack",
                "animations\\combat\\attack.anim",
                "animations\\idle\\stand.anim",
                "textures\\hero.dds");

            var vm = CreateBrowser();
            var root = vm.Files.First();
            CollapseAll(root);

            // Apply filter
            vm.Filter.FilterText = "attack";

            // Manually expand a node while filter is active
            var animFolder = root.Children.First(x => x.Name == "animations");
            animFolder.IsNodeExpanded = true;

            // Act - clear the filter
            vm.Filter.FilterText = "";

            // Assert - user-expanded nodes should remain expanded
            Assert.That(animFolder.IsNodeExpanded, Is.True);
            // All nodes should be visible again
            var heroTexture = FindFileNode(root, "hero.dds");
            Assert.That(heroTexture?.IsVisible, Is.True);
        }

        [Test]
        public void Filter_Active_NewFilesAdded_OnlyMatchingVisible()
        {
            // Arrange
            var container = AddPackFiles("mod", "c:\\mod.pack",
                "textures\\hero.dds",
                "textures\\villain.dds");

            var vm = CreateBrowser();
            vm.Filter.FilterText = "hero";

            // Act - add more files to the container
            var newFile = PackFile.CreateFromASCII("dragon.dds", "data");
            _packFileService.AddFilesToPack(container, [new NewPackFileEntry("textures", newFile)]);

            // Assert - "dragon.dds" should not be visible because filter is "hero"
            var root = vm.Files.First();
            var dragonNode = FindFileNode(root, "dragon.dds");
            Assert.That(dragonNode, Is.Not.Null);
            Assert.That(dragonNode.IsVisible, Is.False);

            // Original match should still be visible
            var heroNode = FindFileNode(root, "hero.dds");
            Assert.That(heroNode?.IsVisible, Is.True);
        }

        [Test]
        public void Filter_Active_NewContainerAdded_FilterAppliedToIt()
        {
            // Arrange
            var container1 = AddPackFiles("mod1", "c:\\mod1.pack",
                "textures\\hero.dds",
                "models\\villain.mesh");

            var vm = CreateBrowser();
            vm.Filter.FilterText = "hero";

            // Act - add a new container
            var container2 = PackFileContainer.CreatePackFile("mod2", "c:\\mod2.pack");
            var file1 = PackFile.CreateFromASCII("hero_shield.dds", "data");
            var file2 = PackFile.CreateFromASCII("enemy.dds", "data");
            container2.AddOrUpdateFile("textures\\hero_shield.dds", file1);
            container2.AddOrUpdateFile("textures\\enemy.dds", file2);
            _packFileService.AddContainer(container2);

            // Assert - the new container's tree should have the filter applied
            var root2 = vm.Files.First(x => x.Owner == container2);
            var heroShield = FindFileNode(root2, "hero_shield.dds");
            var enemy = FindFileNode(root2, "enemy.dds");
            Assert.That(heroShield?.IsVisible, Is.True);
            Assert.That(enemy?.IsVisible, Is.False);
        }

        private static TreeNode? FindFileNode(TreeNode root, string fileName)
        {
            if (root.NodeType == NodeType.File && root.Name == fileName)
                return root;

            foreach (var child in root.Children)
            {
                var found = FindFileNode(child, fileName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void CollapseAll(TreeNode node)
        {
            node.IsNodeExpanded = false;
            foreach (var child in node.Children)
                CollapseAll(child);
        }

        [Test]
        public void Filter_Active_FileRemoved_RemainingMatchesStillVisible()
        {
            // Arrange
            var container = AddPackFiles("mod", "c:\\mod.pack",
                "textures\\hero.dds",
                "textures\\hero_alt.dds",
                "textures\\villain.dds");

            var vm = CreateBrowser();
            vm.Filter.FilterText = "hero";

            // Verify both hero files are visible
            var root = vm.Files.First();
            Assert.That(FindFileNode(root, "hero.dds")?.IsVisible, Is.True);
            Assert.That(FindFileNode(root, "hero_alt.dds")?.IsVisible, Is.True);

            // Act - delete one of the matching files
            var fileToDelete = container.FindFile("textures\\hero_alt.dds");
            _packFileService.DeleteFile(container, fileToDelete!);

            // Assert - remaining match still visible, villain still hidden
            root = vm.Files.First();
            Assert.That(FindFileNode(root, "hero.dds")?.IsVisible, Is.True);
            Assert.That(FindFileNode(root, "hero_alt.dds"), Is.Null);
            Assert.That(FindFileNode(root, "villain.dds")?.IsVisible, Is.False);
        }

        [Test]
        public void Filter_Active_FileRenamed_VisibilityUpdatesAccordingly()
        {
            // Arrange
            var container = AddPackFiles("mod", "c:\\mod.pack",
                "textures\\hero.dds",
                "textures\\villain.dds");

            var vm = CreateBrowser();
            vm.Filter.FilterText = "hero";

            var root = vm.Files.First();
            Assert.That(FindFileNode(root, "villain.dds")?.IsVisible, Is.False);

            // Act - rename "villain.dds" to "hero_shield.dds"
            var villainFile = container.FindFile("textures\\villain.dds");
            _packFileService.RenameFile(container, villainFile!, "hero_shield.dds");

            // Assert - renamed file now matches filter and is visible
            root = vm.Files.First();
            Assert.That(FindFileNode(root, "hero_shield.dds")?.IsVisible, Is.True);
            Assert.That(FindFileNode(root, "hero.dds")?.IsVisible, Is.True);
        }
    }
}
