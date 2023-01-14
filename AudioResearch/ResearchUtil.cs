using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioResearch
{
    public static class ResearchUtil
    {
        public static PackFileService GetPackFileService(bool skipLoadingWemFiles = true)
        {
            var appSettings = new ApplicationSettingsService();
            appSettings.CurrentSettings.SkipLoadingWemFiles = skipLoadingWemFiles;
            var gamePath = appSettings.CurrentSettings.GameDirectories.First(x => x.Game == GameTypeEnum.Warhammer3);
            PackFileService pfs = new PackFileService(new PackFileDataBase(), new SkeletonAnimationLookUpHelper(), appSettings);
            pfs.LoadAllCaFiles(gamePath.Path, GameInformationFactory.GetGameById(GameTypeEnum.Warhammer3).DisplayName);

            return pfs;
        }
    }
}
