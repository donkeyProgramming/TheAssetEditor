using Editors.AnimationMeta.Presentation;
using Shared.Core.Events.Global;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimationMeta
{
    public class Tests
    {

        [Test]
        public void MetaDataEditor_OpenAndVerify()
        {
            var packFile = PathHelper.GetDataFile("Throt.pack");


            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            var outputPackFile = runner.LoadPackFile(packFile, true);


            var metaPackFile = runner.PackFileService.FindFile(@"animations/battle/humanoid17/throt_whip_catcher/attacks/hu17_whip_catcher_attack_05.anm.meta");
            var editor = runner.CommandFactory
                .Create<OpenEditorCommand>()
                .Execute<MetaDataEditorViewModel>(metaPackFile!, Shared.Core.ToolCreation.EditorEnums.Meta_Editor);


            //editor.ParsedFile 


           // editor.SaveActionCommand.Execute(null);
        }


        // OpenAndSave
        // OpenModifyAndSave (Check for modifiesd flag)
        // DeleteAndSave
        // MoveUpAndSave
    }
}
