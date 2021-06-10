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
        Dictionary<string, ObservableCollection<AnimationReference>> _skeletonNameToAnimationMap = new Dictionary<string, ObservableCollection<AnimationReference>>();

        public ObservableCollection<string> SkeletonFileNames = new ObservableCollection<string>();

        public SkeletonAnimationLookUpHelper()
        {
        }

        public void LoadFromPackFileContainer(PackFileService pfs, PackFileContainer packFileContainer)
        {
            var allAnimations = pfs.FindAllWithExtentionIncludePaths(".anim", packFileContainer);
            foreach (var animation in allAnimations)
            {
                try
                {
                    var animationSkeletonName = AnimationFile.GetAnimationHeader(animation.Item2).SkeletonName;
                    if (_skeletonNameToAnimationMap.ContainsKey(animationSkeletonName) == false)
                        _skeletonNameToAnimationMap.Add(animationSkeletonName, new ObservableCollection<AnimationReference>());

                    _skeletonNameToAnimationMap[animationSkeletonName].Add(new AnimationReference(pfs.GetFullPath(animation.Item2, packFileContainer), packFileContainer));
                }
                catch (Exception e)
                {
                    _logger.Here().Error("Parsing failed for " + pfs.GetFullPath(animation.Item2, packFileContainer) + "\n" + e.ToString());
                }
            }

            var allNormalSkeletons = allAnimations.Where(x => x.Item1.Contains("animations\\skeletons", StringComparison.InvariantCultureIgnoreCase));
            foreach (var item in allNormalSkeletons)
                SkeletonFileNames.Add(item.Item1);

            var techSkeletons = allAnimations.Where(x => x.Item1.Contains("tech", StringComparison.InvariantCultureIgnoreCase));
            foreach (var item in techSkeletons)
                SkeletonFileNames.Add(item.Item1);

            _logger.Here().Information("Animations found =" + allAnimations.Count());
            _logger.Here().Information("Skeletons found =" + SkeletonFileNames.Count());
        }

        public void UnloadAnimationFromContainer(PackFileService pfs, PackFileContainer packFileContainer)
        {
            int itemsRemoved = 0;
            var s = _skeletonNameToAnimationMap.Select(skeleton => (skeleton.Key, skeleton.Value.Where(animations => animations.Container == packFileContainer))).ToList();
            foreach (var key in s)
            {
                var copy = key.Item2.Select(x => x).ToList();
                foreach (var toRemove in copy)
                {
                    _skeletonNameToAnimationMap[key.Key].Remove(toRemove);
                    itemsRemoved++;
                }
            }
        }

        public List<string> GetAllSkeletonNames()
        {
            return SkeletonFileNames.Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
        }

        public ObservableCollection<AnimationReference> GetAnimationsForSkeleton(string skeletonName)
        {
            if (_skeletonNameToAnimationMap.ContainsKey(skeletonName) == false)
                _skeletonNameToAnimationMap.Add(skeletonName, new ObservableCollection<AnimationReference>());
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

        public AnimationReference FindAnimationRefFromPackFile(PackFile animation, PackFileService pfs)
        {
            var fullPath = pfs.GetFullPath(animation);
            foreach (var entry in _skeletonNameToAnimationMap.Values)
            {
                foreach (var s in entry)
                {
                    var res = String.Compare(s.AnimationFile, fullPath, StringComparison.InvariantCultureIgnoreCase);
                    if (res == 0)
                        return s;
                }
            }

            var f = pfs.FindFile(fullPath);
            if (f != null)
            {
                var pf = pfs.GetPackFileContainer(animation);
                return new AnimationReference(fullPath, pf);  
            }
            return null;
        }


        // Delete this pice of shit
        asdasd
        public class AnimationReference
        {
            public AnimationReference(string animationFile, PackFileContainer container)
            {
                AnimationFile = animationFile;
                Container = container;
            }
            public string AnimationFile { get; set; }
            public PackFileContainer Container { get; set; }

            public override string ToString()
            {
                return $"[{Container.Name}] {AnimationFile}";
             }
        }


    }
}
