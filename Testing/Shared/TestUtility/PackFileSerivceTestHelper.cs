using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Test.TestingUtility.TestUtility;

namespace Shared.TestUtility
{
    public static class PackFileSerivceTestHelper
    {
        public static IPackFileService Create(string path, GameTypeEnum gameTypeEnum = GameTypeEnum.Warhammer3)
        {
            var pfs = new PackFileService(null);
            var loader = new PackFileContainerLoader(new ApplicationSettingsService(gameTypeEnum));
            var container = loader.LoadSystemFolderAsPackFileContainer(path);
            container.IsCaPackFile = true;
            pfs.AddContainer(container);
            
            return pfs;
        }

        public static IPackFileService CreateFromFolder(GameTypeEnum selectedGame, string path )
        {
            var pfs = new PackFileService(null);
            var loader = new PackFileContainerLoader(new ApplicationSettingsService(selectedGame));

            var container = loader.LoadSystemFolderAsPackFileContainer(PathHelper.GetDataFolder(path));
            container.IsCaPackFile = true;
            pfs.AddContainer(container);
            return pfs;
        }
    }
}
