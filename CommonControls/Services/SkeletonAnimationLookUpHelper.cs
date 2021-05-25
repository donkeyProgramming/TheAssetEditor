using Common;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CommonControls.Services
{

    public class SkeletonAnimationLookUpHelper 
    {
        ILogger _logger = Logging.Create<SkeletonAnimationLookUpHelper>();
        Dictionary<string, ObservableCollection<string>> _skeletonNameToAnimationMap = new Dictionary<string, ObservableCollection<string>>();

        public ObservableCollection<string> SkeletonFileNames = new ObservableCollection<string>();

        public SkeletonAnimationLookUpHelper()
        {
        }

        public void LoadFromPackFileContainer(PackFileService pfs, PackFileContainer packFileContainer)
        {
            _logger.Here().Information("Finding all animations");

            var AllAnimations = pfs.FindAllWithExtention(".anim", packFileContainer);
            _logger.Here().Information("Animations found =" + AllAnimations.Count());

            foreach (var animation in AllAnimations)
            {
                try
                {
                    var animationSkeletonName = AnimationFile.GetAnimationHeader(animation).SkeletonName;
                    if (_skeletonNameToAnimationMap.ContainsKey(animationSkeletonName) == false)
                        _skeletonNameToAnimationMap.Add(animationSkeletonName, new ObservableCollection<string>());

                    _skeletonNameToAnimationMap[animationSkeletonName].Add(pfs.GetFullPath(animation, packFileContainer));
                }
                catch (Exception e)
                {
                    _logger.Here().Error("Parsing failed for " + pfs.GetFullPath(animation, packFileContainer) + "\n" + e.ToString());
                }
            }


            _logger.Here().Information("Finding all skeletons");

            var allFilesInFolder = pfs.FindAllFilesInDirectory("animations\\skeletons", packFileContainer);
            var newItems = allFilesInFolder.Where(x => Path.GetExtension(x.Name) == ".anim").Select(x => pfs.GetFullPath(x, packFileContainer)).ToList();
            foreach (var item in newItems)
                SkeletonFileNames.Add(item);

            _logger.Here().Information("Skeletons found =" + SkeletonFileNames.Count());

            _logger.Here().Information("Finding all done");
        }

        public List<string> GetAllSkeletonNames()
        {
            return SkeletonFileNames.Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
        }

        public ObservableCollection<string> GetAnimationsForSkeleton(string skeletonName)
        {
            if (_skeletonNameToAnimationMap.ContainsKey(skeletonName) == false)
                _skeletonNameToAnimationMap.Add(skeletonName, new ObservableCollection<string>());
            return _skeletonNameToAnimationMap[skeletonName];
        }

        public AnimationFile GetSkeletonFileFromName(PackFileService pfs, string skeletonName)
        {
            foreach (var name in SkeletonFileNames)
            {
                if (name.Contains(skeletonName))
                {
                    var file = pfs.FindFile(name);
                    if (file != null)
                        return AnimationFile.Create(file as PackFile);
                }
            }
            return null;
        }

        public class AnimationReference
        { 
            public string AnimationFile { get; set; }
            public PackFileContainer Container { get; set; }
        }
    }
}
