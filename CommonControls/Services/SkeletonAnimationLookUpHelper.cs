using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CommonControls.Services
{

    public class SkeletonAnimationLookUpHelper : IAnimationFileDiscovered
    {
        ILogger _logger = Logging.Create<SkeletonAnimationLookUpHelper>();
        ConcurrentDictionary<string, ObservableCollection<AnimationReference>> _skeletonNameToAnimationMap = new();
        object _threadLock = new object();
        public ObservableCollection<string> SkeletonFileNames { get; private set; } = new();

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
            lock (_threadLock)
            {
                try
                {
                    var brokenAnims = new string[] { "rigidmodels\\buildings\\roman_aqueduct_straight\\roman_aqueduct_straight_piece01_destruct01_anim.anim" };
           
                    if (brokenAnims.Contains(fullPath))
                    {
                        _logger.Here().Warning("Skipping loading of known broken file - " + fullPath);
                        return;
                    }

                    var newEntry = new ObservableCollection<AnimationReference>() { new AnimationReference(fullPath, container) };
                    var animationSkeletonName = AnimationFile.GetAnimationHeader(file).SkeletonName;
                    _skeletonNameToAnimationMap.AddOrUpdate(
                        animationSkeletonName,
                        newEntry,
                        (sanimationSkeletonName, animationMap) =>
                        {
                            animationMap.Add(new AnimationReference(fullPath, container));
                            return animationMap;
                        }
                    );

                    if (fullPath.Contains("animations\\skeletons", StringComparison.InvariantCultureIgnoreCase))
                        SkeletonFileNames.Add(fullPath);
                    else if (fullPath.Contains("tech", StringComparison.InvariantCultureIgnoreCase))
                        SkeletonFileNames.Add(fullPath);
                }
                catch (Exception e)
                {
                    _logger.Here().Error("Parsing failed for " + fullPath + "\n" + e.ToString());
                }
            }
        }


        public void UnloadAnimationFromContainer(PackFileService pfs, PackFileContainer packFileContainer)
        {
            lock (_threadLock)
            {
                int itemsRemoved = 0;
                var s = _skeletonNameToAnimationMap
                    .Select(skeleton =>
                        new
                        {
                            SkeletonName = skeleton.Key,
                            Animations = skeleton.Value.Where(animations => animations.Container == packFileContainer).ToList()
                        })
                    .ToList();

                foreach (var key in s)
                {
                    var copy = key.Animations.Select(x => x).ToList();
                    foreach (var toRemove in copy)
                    {
                        _skeletonNameToAnimationMap[key.SkeletonName].Remove(toRemove);
                        itemsRemoved++;
                    }
                }
            }
        }

        public List<string> GetAllSkeletonNames()
        {
            lock (_threadLock)
            {
                return SkeletonFileNames.Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
            }
        }

        public ObservableCollection<AnimationReference> GetAnimationsForSkeleton(string skeletonName)
        {
            lock (_threadLock)
            {
                return _skeletonNameToAnimationMap.GetOrAdd(
                skeletonName,
                    new ObservableCollection<AnimationReference>()
                );
            }
        }

        public AnimationFile GetSkeletonFileFromName(PackFileService pfs, string skeletonName)
        {
            lock (_threadLock)
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
                            if (pfs.GetFullPath(file).Contains("tech") == false)
                                return AnimationFile.Create(file);
                        }
                    }
                }

                // Try loading from path as a backup in case loading failed. Looking at you wh3...
                var path = $"animations\\skeletons\\{skeletonName}.anim";
                var animationFile = pfs.FindFile(path);
                if (animationFile != null)
                    return AnimationFile.Create(animationFile);
                return null;
            }
        }

        public AnimationReference FindAnimationRefFromPackFile(PackFile animation, PackFileService pfs)
        {
            lock (_threadLock)
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
