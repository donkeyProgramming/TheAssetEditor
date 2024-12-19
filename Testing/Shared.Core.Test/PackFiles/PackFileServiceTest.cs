using Moq;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Test.Shared.Core.PackFiles
{



    internal class PackFileServiceTest
    {
        [Test]
        public void AddContainer_CaPackFile()
        {
            // Arrenge
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = new PackFileService(eventHub.Object);
            var container = new PackFileContainer("MyTest");
            container.SystemFilePath = "SystemPath";
            container.IsCaPackFile = true;

            // Act
            pfs.AddContainer(container);

            // Assert
            var containers = pfs.GetAllPackfileContainers();

            Assert.That(containers.First(), Is.EqualTo(container));
            Assert.That(pfs.GetEditablePack(), Is.Null);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(container)), Times.Once);
        }

        [Test]
        public void AddContainer_CaPackFileNotSet()
        {
            // Arrenge
            var eventHub = new Mock<IGlobalEventHub>();
            var dialogProvider = new Mock<ISimpleMessageBox>();
            var pfs = new PackFileService(eventHub.Object);
            pfs.MessageBoxProvider = dialogProvider.Object;
            var container = new PackFileContainer("MyTest");
            container.SystemFilePath = "SystemPath";
            container.IsCaPackFile = false;

            // Act
            pfs.AddContainer(container);

            // Assert
            var containers = pfs.GetAllPackfileContainers();

            Assert.That(containers.Count, Is.EqualTo(0));
            Assert.That(pfs.GetEditablePack(), Is.Null);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(container)), Times.Never);
            dialogProvider.Verify(m => m.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void AddContainer_CaPackFileSet()
        {
            // Arrenge
            var eventHub = new Mock<IGlobalEventHub>();

            var pfs = new PackFileService(eventHub.Object);
            var caContainer = new PackFileContainer("MyTest");
            caContainer.SystemFilePath = "SystemPath";
            caContainer.IsCaPackFile = true;

            var customContainer = new PackFileContainer("MyTest");
            customContainer.SystemFilePath = "SystemPath2";
            customContainer.IsCaPackFile = false;

            // Act
            pfs.AddContainer(caContainer);
            pfs.AddContainer(customContainer, true);

            // Assert
            var containers = pfs.GetAllPackfileContainers();

            Assert.That(containers.Count, Is.EqualTo(2));
            Assert.That(pfs.GetEditablePack(), Is.EqualTo(customContainer));
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(caContainer)), Times.Once);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(customContainer)), Times.Once);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerSetAsMainEditableEvent(customContainer)), Times.Once);
        }

        [Test]
        public void AddContainers_Duplicate()
        {
            // Arrenge
            var eventHub = new Mock<IGlobalEventHub>();
            var dialogProvider = new Mock<ISimpleMessageBox>();
            var pfs = new PackFileService( eventHub.Object);
            var caContainer = new PackFileContainer("MyTest");
            pfs.MessageBoxProvider = dialogProvider.Object;
            caContainer.SystemFilePath = "SystemPath";
            caContainer.IsCaPackFile = true;

            var customContainer = new PackFileContainer("MyTest");
            customContainer.SystemFilePath = "SystemPath2";
            customContainer.IsCaPackFile = false;

            // Act
            pfs.AddContainer(caContainer);
            pfs.AddContainer(customContainer, true);
            pfs.AddContainer(customContainer);

            // Assert
            var containers = pfs.GetAllPackfileContainers();

            Assert.That(containers.Count, Is.EqualTo(2));
            Assert.That(pfs.GetEditablePack(), Is.EqualTo(customContainer));
            dialogProvider.Verify(m => m.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void CreateNewPackFileContainer()
        {
            // Arrenge
            var eventHub = new Mock<IGlobalEventHub>();
            var pfs = new PackFileService(eventHub.Object);
            var container = new PackFileContainer("MyTest");
            container.SystemFilePath = "SystemPath";
            container.IsCaPackFile = true;

            // Act
            pfs.AddContainer(container);
            var emptyPackFileContainer = pfs.CreateNewPackFileContainer("Custom", PackFileCAType.MOD, true);

            // Assert
            var containers = pfs.GetAllPackfileContainers();

            Assert.That(containers.Count(), Is.EqualTo(2));
            Assert.That(pfs.GetEditablePack(), Is.EqualTo(emptyPackFileContainer));
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(container)), Times.Once);
            eventHub.Verify(m => m.PublishGlobalEvent(new PackFileContainerAddedEvent(emptyPackFileContainer)), Times.Once);
        }




        //AddFilesToPack


        // AddFilesToPack - Ensure valid path!
        // CopyFileFromOtherPackFile
        // SetEditablePack - Ca file, null, valid
        // UnloadPackContainer
        // SaveFile
        // SavePackContainer
    }
}
