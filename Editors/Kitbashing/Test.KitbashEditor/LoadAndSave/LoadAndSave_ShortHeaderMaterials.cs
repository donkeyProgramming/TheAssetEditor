using GameWorld.Core.SceneNodes;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Test.TestingUtility.Shared;

namespace Test.KitbashEditor.LoadAndSave
{
    internal class LoadAndSave_ShortHeaderMaterials
    {
        // The scene can not show a projected decal (no vertices, not a weighted material). Opening one
        // must not take the editor down; the mesh is skipped.
        [Test]
        public void Warhammer3Decal_OpensWithoutMeshes()
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var pack = runner.CreateOutputPack();
            var file = PackFile.CreateFromBytes("chd_coal_decal_01.rigid_model_v2", RmvTestFiles.BuildWarhammer3Decal("rigidmodels/decals/chd/chd_coal_decal_01"));
            runner.PackFileService.AddFilesToPack(pack, [new NewPackFileEntry("rigidmodels/decals/chd", file)]);

            var editor = runner.CommandFactory.Create<OpenEditorCommand>().Execute(file) as Editors.KitbasherEditor.ViewModels.KitbasherViewModel;

            Assert.That(editor, Is.Not.Null);
            Assert.That(SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(editor!.SceneExplorer.SceneManager.RootNode), Is.Empty);
        }
    }
}
