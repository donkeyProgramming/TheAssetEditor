using AssetEditor.Services;
using Editors.AnimationMeta.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Commands;
using Shared.Core.ToolCreation;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    // Closing a pack that has a file open in an editor asks first; answering No used to keep the pack loaded but close its
    // editors anyway.
    public class ClosingAPackWithOpenEditorsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void TheAnswer_DecidesWhetherThePackAndItsEditorsClose(bool answer)
        {
            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(PathHelper.GetDataFile("Throt.pack"), false);
            var file = runner.PackFileService.FindFile(@"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta")!;
            var pack = runner.PackFileService.GetPackFileContainer(file)!;
            var editor = runner.CommandFactory.Create<OpenEditorCommand>().Execute<MetaDataEditorViewModel>(file, EditorEnums.Meta_Editor);

            var editors = (EditorManager)runner.ServiceProvider.GetRequiredService<IEditorManager>();
            var asked = 0;
            editors.AskToCloseOpenFiles = _ => { asked++; return answer; };
            runner.PackFileService.UnloadPackContainer(pack);

            Assert.That(asked, Is.EqualTo(1));
            Assert.That(runner.PackFileService.GetAllPackfileContainers().Contains(pack), Is.EqualTo(!answer));
            Assert.That(editors.CurrentEditorsList.Contains(editor), Is.EqualTo(!answer));
        }
    }
}
