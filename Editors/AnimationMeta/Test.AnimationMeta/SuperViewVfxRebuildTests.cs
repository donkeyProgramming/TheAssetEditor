using System.IO;
using Editors.AnimationMeta.SuperView;
using Editors.AnimationMeta.SuperView.Visualisation;
using Editors.Shared.Core.Common.BaseControl;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationPack;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    public class SuperViewVfxRebuildTests
    {
        AssetEditorTestRunner _runner = null!;
        SuperViewViewModel _superView = null!;

        [SetUp]
        public void Setup()
        {
            _runner = new AssetEditorTestRunner();
            _runner.CreateCaContainer();
            _runner.LoadPackFile(PathHelper.GetDataFile("Throt.pack"), true);

            _superView = (SuperViewViewModel)_runner.ServiceProvider.GetRequiredService<IEditorManager>().Create(EditorEnums.SuperView_Editor)!;
            _superView.Load(new AnimationToolInput
            {
                Mesh = _runner.PackFileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition")!,
                FragmentName = @"animations/database/battle/bin/hu17_dlc16_throt.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5"),
            });
        }

        // A vfx file added or saved in any pack (the VFX editor's save, a replaced texture) rebuilds the scene's effects, so
        // SuperView shows it without the tab being reopened, and so does a model, material or animation a rigid particle may
        // draw, wherever it lives; any other file leaves the scene alone.
        [TestCase(@"vfx\test_smoke.xml", true)]
        [TestCase(@"variantmeshes\wh_variantmodels\x\x.rigid_model_v2", true)]
        [TestCase(@"variantmeshes\wh_variantmodels\x\x.wsmodel", true)]
        [TestCase(@"variantmeshes\wh_variantmodels\x\x.xml.material", true)]
        [TestCase(@"animations\battle\x\x.anim", true)]
        [TestCase(@"text\notes.txt", false)]
        public void AChangedVfxFile_RebuildsTheScene(string path, bool rebuilds)
        {
            var data = _superView.SceneObjects[0].Data;
            var pack = _runner.CreateOutputPack();

            var before = data.MetaDataItems;
            _runner.PackFileService.AddFilesToPack(pack, [new NewPackFileEntry(Path.GetDirectoryName(path)!, PackFile.CreateFromBytes(Path.GetFileName(path), [1]))]);
            Assert.That(data.MetaDataItems, rebuilds ? Is.Not.SameAs(before) : Is.SameAs(before), "on add");

            before = data.MetaDataItems;
            _runner.PackFileService.SaveFile(_runner.PackFileService.FindFile(path, pack)!, [2]);
            Assert.That(data.MetaDataItems, rebuilds ? Is.Not.SameAs(before) : Is.SameAs(before), "on save");
        }

        // ...and builds every tag again from what the packs now hold, rather than keeping what it built from the old files.
        [Test]
        public void AChangedVfxFile_BuildsEveryTagAgain()
        {
            var data = _superView.SceneObjects[0].Data;
            var before = data.MetaDataItems.ToList();
            Assert.That(before, Is.Not.Empty);
            _runner.PackFileService.AddFilesToPack(_runner.CreateOutputPack(), [new NewPackFileEntry("vfx", PackFile.CreateFromBytes("test_smoke.xml", [1]))]);
            Assert.That(data.MetaDataItems.Intersect(before), Is.Empty);
        }

        [Test]
        public void TheShowVfxBox_ReachesTheBuilder()
        {
            var builder = _runner.GetRequiredServiceInCurrentEditorScope<IMetaDataBuilder>();
            _superView.ShowVfx = false;
            Assert.That(builder.ShowVfx, Is.False);
            _superView.ShowVfx = true;
            Assert.That(builder.ShowVfx, Is.True);
        }
    }
}
