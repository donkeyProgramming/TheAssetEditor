using E2EVerification.Shared;
using Shared.Ui.Events.UiCommands;

namespace E2EVerification
{


    internal class UIEvent_Tests
    {
        private readonly string _packFile = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Total War WARHAMMER III\\data\\meta_test_issue_124.pack";
        private readonly string _anmMetaFile = @"animations\battle\humanoid01\halberd\attacks\hu1_noctilus_attack.anm.meta";

        [SetUp]
        public void Setup()
        {
        }
        [Test]
        public void DuplicateTest()
        {
            var runner = new AssetEditorTestRunner();
            var pack = runner.LoadPackFile(_packFile);
            var item = runner.PackFileService.FindFile(_anmMetaFile);

            runner.CommandFactory.Create<DuplicateCommand>().Execute(item);
            runner.PackFileService.FindFile("animations\\battle\\humanoid01\\halberd\\attacks\\hu1_noctilus_attack_copy.anm.meta");
        }
    }
}
