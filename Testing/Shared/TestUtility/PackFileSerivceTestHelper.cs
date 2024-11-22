using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Shared.TestUtility
{
    public static class PackFileSerivceTestHelper
    {
        public static PackFileService Create(string path, GameTypeEnum gameTypeEnum = GameTypeEnum.Warhammer3)
        {
            var pfs = new PackFileService(new ApplicationSettingsService(gameTypeEnum), new GameInformationFactory(), null);
            pfs.LoadSystemFolderAsPackFileContainer(path);
            return pfs;
        }
    }
}
