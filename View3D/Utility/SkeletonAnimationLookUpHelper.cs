using Common;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace View3D.Utility
{
    public class SkeletonAnimationLookUpHelper
    {
        ILogger _logger = Logging.Create<SkeletonAnimationLookUpHelper>();
        Dictionary<string, List<PackFile>> _skeletonNameToAnimationMap = new Dictionary<string, List<PackFile>>();

        public void FindAllAnimations(PackFileService packFileService)
        {
            _logger.Here().Information("Finding all animations");

            var AllAnimations = packFileService.FindAllWithExtention(".anim");

            _logger.Here().Information("Animations found =" + AllAnimations.Count());

            foreach (var animation in AllAnimations)
            {
                try
                {
                    var animationSkeletonName = AnimationFile.GetAnimationHeader(animation).SkeletonName;
                    if (_skeletonNameToAnimationMap.ContainsKey(animationSkeletonName) == false)
                        _skeletonNameToAnimationMap.Add(animationSkeletonName, new List<PackFile>());

                    _skeletonNameToAnimationMap[animationSkeletonName].Add(animation);
                }
                catch (Exception e)
                {
                    _logger.Here().Error("Parsing failed for " + packFileService.GetFullPath(animation)+ "\n" + e.ToString());
                }
            }

            _logger.Here().Information("Finding all done");
        }

        public List<PackFile> GetAnimationsForSkeleton(string skeletonName)
        {
            if (_skeletonNameToAnimationMap.ContainsKey(skeletonName) == false)
                return new List<PackFile>();
            return _skeletonNameToAnimationMap[skeletonName];
        }
    }
}
