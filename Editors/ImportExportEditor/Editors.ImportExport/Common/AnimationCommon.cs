using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using System.Text;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Services;
using Shared.Core.PackFiles.Models;
using Shared.Core.ByteParsing;
using System.Linq.Expressions;
using static Shared.GameFormats.Animation.AnimationFile;

namespace Editors.ImportExport.Common
{
    public class AnimationCommon
    {
        static private readonly ILogger logger = Logging.Create<AnimationCommon>();
        public static PackFile FetchSkeletonFileFromId(string skeletonNameFromRmv2, PackFileService pfs)
        {         
           var skeletonName = $@"animations\skeletons\{skeletonNameFromRmv2}.anim";

            var foundSkeletonPackFile = pfs.FindFile(skeletonName);

            if (foundSkeletonPackFile == null)
            {
                logger.Here().Warning($"Could not find skeleton .anim file: {skeletonName}");
                return null;
            }            

            return foundSkeletonPackFile;
        }

        public static List<PackFile> FetchAnimationsFromSkeletonId(string skeletonName, PackFileService pfs)
        {            
            var retList = new List<PackFile>();

            var animSearchList = PackFileServiceUtility.FindAllWithExtention(pfs, ".anim");

            if (animSearchList == null || !animSearchList.Any())
            {
                logger.Here().Warning($"PackFileServiceUtility.GetAllAnimPacks return invalid or empty list: {animSearchList?.Count}");
                return retList;
            }

            foreach (var packFile in animSearchList)
            {
                AnimationHeader header;
                                
                try
                {
                    header = AnimationFile.GetAnimationHeader(packFile);
                }
                 catch
                {
                    continue;
                }
                

                if (header.SkeletonName.ToLower() == skeletonName.ToLower())
                {                    
                    retList.Add(packFile);  
                }
            }

            return retList;
        }
    }
}
