using E2EVerification.Shared;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Events.UiCommands;

namespace E2EVerification
{
    internal class UiCommand_DuplicateFile_Tests
    {
        [Test]
        [TestCase("testFile",           "testFile_copy")]            // No extension
        [TestCase("testFile.anm",       "testFile_copy.anm")]        // Single extension 
        [TestCase("testFile.anm.meta",  "testFile_copy.anm.meta")]   // Double extension 
        public void DuplicateFileCommand(string fileName, string result)
        {
            var runner = new AssetEditorTestRunner();
            var sourcePackFile = runner.CreateEmptyPackFile("SourcePack", false);
            var outputPackFile = runner.CreateEmptyPackFile("OutputPack", true);
            runner.PackFileService.AddFileToPack(sourcePackFile, "Animation\\Meta", PackFile.CreateFromASCII(fileName, "DummyContent"));
            var fileToCopy = runner.PackFileService.FindFile("Animation\\Meta\\" + fileName, sourcePackFile);

            runner.CommandFactory.Create<DuplicateFileCommand>().Execute(fileToCopy);
            var foundFile = runner.PackFileService.FindFile("Animation\\Meta\\" + result, outputPackFile);
            Assert.That(foundFile, Is.Not.Null);
        }
    }
}
