using Moq;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Test.Shared.Core.PackFiles
{
    internal class FileSaveServiceTests
    {
        IPackFileService _pfs;
        PackFileContainer _container;
        Mock<IPackFileUiProvider> _uiProvider = new();
        Mock<IGlobalEventHub> _eventHub = new();
        PackFile _fileHandle;

        [SetUp]
        public void Setup()
        {
            var dialogProvider = new Mock<IStandardDialogProvider>();
            _pfs = new PackFileService(dialogProvider.Object, _eventHub.Object);
            var container = new PackFileContainer("MyTest");
            container.SystemFilePath = "SystemPath";
            container.IsCaPackFile = true;

            _pfs.AddContainer(container);
            _container = _pfs.CreateNewPackFileContainer("Output", PackFileCAType.MOD, true);

            List<NewPackFileEntry> files = [
                new NewPackFileEntry("folder\\subfolder", PackFile.CreateFromBytes("File0.test", [0])),
                new NewPackFileEntry("folder\\subfolder", PackFile.CreateFromBytes("File1.test", [1]))];
            _pfs.AddFilesToPack(_container, files);

            _fileHandle = _pfs.FindFile("folder\\subfolder\\File1.test");
            _eventHub.Invocations.Clear();
            _uiProvider.Invocations.Clear();
        }

        [Test]
        public void Save_NoExistingFile_DontPromptOnConflict()
        {
            // Arrange
            var saveService = new FileSaveService(_pfs, _uiProvider.Object);

            // Act
            var result = saveService.Save("folder\\subfolder2\\File1.test", [3], false);

            // Assert
            Assert.That(_container.FileList.Count, Is.EqualTo(3));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.DataSource.ReadData(), Is.EqualTo(new byte[] { 3 }));

            _eventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesAddedEvent>()), Times.Once);
            _uiProvider.Verify(x => x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>()), Times.Never);
        }

        [Test]
        public void Save_ExistingFile_DontPromptOnConflict()
        {
            // Arrange
            var saveService = new FileSaveService(_pfs, _uiProvider.Object);

            // Act
            var result = saveService.Save("folder\\subfolder\\File1.test", [3], false);

            // Assert
            Assert.That(_container.FileList.Count, Is.EqualTo(2));
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(_fileHandle));
            Assert.That(result!.DataSource.ReadData(), Is.EqualTo(new byte[] { 3 }));

            _eventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesUpdatedEvent>()), Times.Once);
            _eventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileSavedEvent>()), Times.Once);
            _uiProvider.Verify(x => x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>()), Times.Never);
        }

        [Test]
        public void Save_NoExistingFile_PromptOnConflict()
        {
            // Arrange
            var saveService = new FileSaveService(_pfs, _uiProvider.Object);

            // Act
            var result = saveService.Save("folder\\subfolder2\\File1.test", [3], true);

            // Assert
            Assert.That(_container.FileList.Count, Is.EqualTo(3));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.DataSource.ReadData(), Is.EqualTo(new byte[] { 3 }));

            _eventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesAddedEvent>()), Times.Once);
            _uiProvider.Verify(x=>x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>()), Times.Never);
        }

        [Test]
        public void Save_ExistingFile_PromptOnConflict_NewPath()
        {
            // Arrange
            var saveService = new FileSaveService(_pfs, _uiProvider.Object);
            _uiProvider.Setup(x => x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>())).Returns(new SaveDialogResult(true, null, "folder\\subfolder3\\File1.test"));

            // Act
            var result = saveService.Save("folder\\subfolder\\File1.test", [3], true);

            // Assert
            Assert.That(_container.FileList.Count, Is.EqualTo(3));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.DataSource.ReadData(), Is.EqualTo(new byte[] { 3 }));
            var lookup = _pfs.FindFile("folder\\subfolder3\\File1.test");
            Assert.That(lookup, Is.Not.Null);

            _eventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileContainerFilesAddedEvent>()), Times.Once);
            _uiProvider.Verify(x => x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>()), Times.Once);
        }


        [Test]
        public void Save_ExistingFile_PromptOnConflict_ExistingPath()
        {
            // Arrange
            var saveService = new FileSaveService(_pfs, _uiProvider.Object);
            _uiProvider.Setup(x => x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>())).Returns(new SaveDialogResult(true, null, "folder\\subfolder\\File0.test"));

            // Act
            var result = saveService.Save("folder\\subfolder\\File1.test", [3], true);

            // Assert
            Assert.That(_container.FileList.Count, Is.EqualTo(2));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.DataSource.ReadData(), Is.EqualTo(new byte[] { 3 }));

            _eventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileSavedEvent>()), Times.Once);
            _uiProvider.Verify(x => x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>()), Times.Once);
        }

        [Test]
        public void Save_ExistingFile_PromptOnConflict_Exit()
        {
            // Arrange
            var saveService = new FileSaveService(_pfs, _uiProvider.Object);
            _uiProvider.Setup(x => x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>())).Returns(new SaveDialogResult(false, null, null));

            // Act
            var result = saveService.Save("folder\\subfolder\\File1.test", [3], true);

            // Assert
            Assert.That(_container.FileList.Count, Is.EqualTo(2));
            Assert.That(result, Is.Null);

            _eventHub.Verify(x => x.PublishGlobalEvent(It.IsAny<PackFileSavedEvent>()), Times.Never);
            _uiProvider.Verify(x => x.DisplaySaveDialog(_pfs, It.IsAny<List<string>>()), Times.Once);
        }

        // SaveAs
    }
}
