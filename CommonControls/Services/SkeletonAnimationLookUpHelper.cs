using Common;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CommonControls.Services
{

    public class SkeletonAnimationLookUpHelper : IGameComponent, IAnimationFileDiscovered
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
                FileDiscovered(animation.Item2, packFileContainer, pfs.GetFullPath(animation.Item2, packFileContainer));
        }

        public void FileDiscovered(PackFile file, PackFileContainer container, string fullPath)
        {
            try
            {
                var animationSkeletonName = AnimationFile.GetAnimationHeader(file).SkeletonName;
                if (_skeletonNameToAnimationMap.ContainsKey(animationSkeletonName) == false)
                    _skeletonNameToAnimationMap.Add(animationSkeletonName, new ObservableCollection<AnimationReference>());

                _skeletonNameToAnimationMap[animationSkeletonName].Add(new AnimationReference(fullPath, container));
            }
            catch (Exception e)
            {
                _logger.Here().Error("Parsing failed for " + fullPath + "\n" + e.ToString());
            }

            if (fullPath.Contains("animations\\skeletons", StringComparison.InvariantCultureIgnoreCase))
                SkeletonFileNames.Add(fullPath);

            else if (fullPath.Contains("tech", StringComparison.InvariantCultureIgnoreCase))
                SkeletonFileNames.Add(fullPath);
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
                    var fullName = Path.GetFileNameWithoutExtension(name);
                    var lookUpFullName = Path.GetFileNameWithoutExtension(skeletonName); 

                    var file = pfs.FindFile(name);
                    if (file != null && fullName == lookUpFullName)
                    {
                        // Make sure its not a tech skeleton
                        if(pfs.GetFullPath(file).Contains("tech") == false)
                            return AnimationFile.Create(file as PackFile);
                    }
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

        public void Initialize()
        {
        }

        


        // Delete this pice of shit

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
                return $"[{Container?.Name}] {AnimationFile}";
             }
        }


    }
}
