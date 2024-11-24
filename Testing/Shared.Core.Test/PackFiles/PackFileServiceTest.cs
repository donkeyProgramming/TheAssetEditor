using Moq;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.Test.PackFiles
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

        }

        // public void AddContainer_CaPackNotSetFile()
        // public void AddContainers_Duplicate()
        // public void AddContainers_NotDuplicate()



        //CreateNewPackFileContainer

        // AddFilesToPack

        // CopyFileFromOtherPackFile

        // SetEditablePack

        // UnloadPackContainer

        // SaveFile

        // SavePackContainer
    }
}
