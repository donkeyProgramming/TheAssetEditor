using Moq;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Serialization.CacheDatabase;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;
using Test.TestingUtility.TestUtility;

namespace Shared.TestUtility
{
    public static class PackFileSerivceTestHelper
    {
        public static IPackFileService Create(string path, GameTypeEnum gameTypeEnum = GameTypeEnum.Warhammer3)
        {
            var pfs = new PackFileService(null);
            var loader = new PackFileContainerLoader(new ApplicationSettingsService(gameTypeEnum), new Mock<IStandardDialogs>().Object, new LocalizationManager(), new PackFileContainerCacheHelper());
            var container = loader.CreateFromSystemFolder(path);
            container.IsCaPackFile = true;
            pfs.AddContainer(container);
            
            return pfs;
        }

        public static IPackFileService CreateFromFolder(GameTypeEnum selectedGame, string path )
        {
            var pfs = new PackFileService(null);
            var loader = new PackFileContainerLoader(new ApplicationSettingsService(selectedGame), new Mock<IStandardDialogs>().Object, new LocalizationManager(), new PackFileContainerCacheHelper());

            var container = loader.CreateFromSystemFolder(PathHelper.GetDataFolder(path));
            container.IsCaPackFile = true;
            pfs.AddContainer(container);
            return pfs;
        }
    }
}
