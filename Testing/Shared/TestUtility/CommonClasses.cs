using System.Reflection;
using Moq;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Utility;
using Shared.Core.ToolCreation;

namespace Test.TestingUtility.TestUtility
{
    public static class MockScopedLogger
    {
        public static IScopedLogger Create()
        {
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>())).Returns(loggerMock.Object);

            var scopedMock = new Mock<IScopedLogger>();
            scopedMock.Setup(x => x.ForContext<It.IsAnyType>()).Returns(loggerMock.Object);
            return scopedMock.Object;
        }
    }

    public record BaseEvent();
    public record ExampleEventNoBase;

    public record ExampleEvent : BaseEvent;

    public class ScopedClass : IDisposable
    {
        public bool IsDisposed { get; private set; } = false;
        public void Dispose() => IsDisposed = true;
    }

    public class SimpleEditor(string name) : IEditorInterface
    {
        public bool IsClosed { get; private set; } = false;
        public string DisplayName { get; set; } = name;
        public void Close() => IsClosed = true;
    }


    public class SimpleSystemFolderContainerFactory : ISystemFolderContainerFactory
    {
        public IPackFileContainer Create(string packFileSystemPath)
        {
            if (Directory.Exists(packFileSystemPath) == false)
            {
                var location = Assembly.GetEntryAssembly()!.Location;
                var loactionDir = Path.GetDirectoryName(location);
                throw new Exception($"Unable to find folder {packFileSystemPath}. Curret systempath is {loactionDir}");
            }

            var containerName = Path.GetFileName(packFileSystemPath);
            var container = PackFileContainer.CreatePackFile(containerName, packFileSystemPath);
            AddFolderContentToPackFile(container, packFileSystemPath, packFileSystemPath.ToLower() + "\\");
            return container;
        }

        private static void AddFolderContentToPackFile(PackFileContainer container, string folderPath, string rootPath)
        {
            var files = Directory.GetFiles(folderPath);
            foreach (var filePath in files)
            {
                var sanatizedFilePath = filePath.ToLower();
                var relativePath = sanatizedFilePath.Replace(rootPath, "");
                var fileName = Path.GetFileName(sanatizedFilePath);

                container.AddOrUpdateFile(relativePath, PackFile.CreateFromFileSystem(fileName, sanatizedFilePath));
            }

            var folders = Directory.GetDirectories(folderPath);
            foreach (var folder in folders)
                AddFolderContentToPackFile(container, folder, rootPath);
        }

    }

}
