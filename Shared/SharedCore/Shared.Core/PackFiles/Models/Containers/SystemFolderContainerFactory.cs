using Shared.Core.Events;
using Shared.Core.Services;

namespace Shared.Core.PackFiles.Models.Containers
{
    public interface ISystemFolderContainerFactory
    {
        SystemFolderContainer Create(string folderPath);
    }

    public class SystemFolderContainerFactory : ISystemFolderContainerFactory
    {
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IGlobalEventHub _globalEventHub;
        private readonly Func<IFileSystemWatcher> _watcherFactory;

        public SystemFolderContainerFactory(IFileSystemAccess fileSystemAccess, IGlobalEventHub globalEventHub, Func<IFileSystemWatcher> watcherFactory)
        {
            _fileSystemAccess = fileSystemAccess;
            _globalEventHub = globalEventHub;
            _watcherFactory = watcherFactory;
        }

        public SystemFolderContainer Create(string folderPath)
        {
            return new SystemFolderContainer(folderPath, _fileSystemAccess, _watcherFactory(), _globalEventHub);
        }
    }
}
