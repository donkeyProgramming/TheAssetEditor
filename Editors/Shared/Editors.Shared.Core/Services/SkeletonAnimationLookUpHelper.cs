using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Serilog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;

namespace Editors.Shared.Core.Services
{
    public class SkeletonAnimationLookUpHelper : IDisposable
    {
        private readonly ILogger _logger = Logging.Create<SkeletonAnimationLookUpHelper>();
        private readonly object _threadLock = new object();

        private readonly IPackFileService _packFileService;
        private readonly IGlobalEventHub _globalEventHub;

        private readonly Dictionary<string, ObservableCollection<AnimationReference>> _skeletonNameToAnimationMap = [];
        private readonly ObservableCollection<string> _skeletonFileNames = [];

        public SkeletonAnimationLookUpHelper(IPackFileService packFileService, IGlobalEventHub globalEventHub)
        {
            _packFileService = packFileService;
            _globalEventHub = globalEventHub;

            _globalEventHub.Register<PackFileContainerAddedEvent>(this, x => PackfileContainerRefresh(x.Container));
            _globalEventHub.Register<PackFileContainerFilesAddedEvent>(this, x => PackfileContainerRefresh(x.Container));
            _globalEventHub.Register<PackFileContainerFolderRenamedEvent>(this, x => PackfileContainerRefresh(x.Container));
           
            _globalEventHub.Register<PackFileContainerRemovedEvent>(this, x => PackfileContainerRemove(x.Container));
            _globalEventHub.Register<PackFileContainerFilesRemovedEvent>(this, x => PackfileContainerRemove(x.Container));
            _globalEventHub.Register<PackFileContainerFolderRemovedEvent>(this, x => PackfileContainerRemove(x.Container));

            // Initialize
            var containers = packFileService.GetAllPackfileContainers();
            foreach(var container in containers)
                LoadFromPackFileContainer(container);
        }

        public void Dispose()
        {
            _globalEventHub.UnRegister(this);
        }

        void PackfileContainerRefresh(PackFileContainer packFileContainer)
        {
            UnloadAnimationFromContainer( packFileContainer);
            LoadFromPackFileContainer( packFileContainer);
        }

        void PackfileContainerRemove(PackFileContainer packFileContainer)
        {
            UnloadAnimationFromContainer( packFileContainer);
        }

        void LoadFromPackFileContainer(PackFileContainer packFileContainer)
        {
            List<string> skeletonFileNameList = [];
            Dictionary<string, List<AnimationReference>> animationList = [];

            var allAnimations = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_packFileService, ".anim", packFileContainer);

            // Split animations in to two categories.
            var allAnimsInSavedPackedFiles = new List<(string FullPath, PackedFileSource DataSource)>();
            var allAnimsOtherFiles = new List<(string FullPath, IDataSource DataSource)>();
            for (var i = 0; i < allAnimations.Count; i++)
            {
                var currentAnimFile = allAnimations[i].Pack.DataSource as PackedFileSource;
                if (currentAnimFile != null)
                    allAnimsInSavedPackedFiles.Add((allAnimations[i].FileName, currentAnimFile));
                else
                    allAnimsOtherFiles.Add((allAnimations[i].FileName, allAnimations[i].Pack.DataSource));
            }

            // Handle packfile which are stored in a saved file.
            // This is done for performance reasons. Opening all the animations files from disk is very slow
            // creating stream which is reused goes a lot faster!
            var groupedAnims = allAnimsInSavedPackedFiles
                .GroupBy(x => x.DataSource.Parent.FilePath)
                .ToList();

            Parallel.For(0, groupedAnims.Count, index =>
            {
                using var handle = File.OpenHandle(groupedAnims[index].Key);

                //https://www.jacksondunstan.com/articles/3568
                var fileStream = File.OpenRead(groupedAnims[index].Key);
                var buffer = new byte[100];

                foreach (var file in groupedAnims[index])
                {
                    RandomAccess.Read(handle, buffer, file.DataSource.Offset);
                    FileDiscovered(buffer, packFileContainer, file.FullPath, ref skeletonFileNameList, ref animationList);
                }
            });

            // Handle all others
            Parallel.For(0, allAnimsOtherFiles.Count, index =>
            {
                var animation = allAnimations[index];
                FileDiscovered(animation.Pack, packFileContainer, animation.FileName, ref skeletonFileNameList, ref animationList);
            });

            foreach(var skeleton in  skeletonFileNameList)
                _skeletonFileNames.Add(skeleton);

            foreach (var animation in animationList)
            {
                if (_skeletonNameToAnimationMap.ContainsKey(animation.Key) == false)
                    _skeletonNameToAnimationMap[animation.Key] = [];
                foreach(var animationReference in animation.Value)
                    _skeletonNameToAnimationMap[animation.Key].Add(animationReference);   
            }
        }

        void FileDiscovered(PackFile file, PackFileContainer container, string fullPath, ref List<string> skeletonFileNameList, ref Dictionary<string, List<AnimationReference>> animationList)
        {
            if(IsKnownBrokenAnimation(fullPath))
                return;

            try
            {
                var animationSkeletonName = AnimationFile.GetAnimationHeader(file).SkeletonName;
                AddAnimation(animationSkeletonName, fullPath, container, ref skeletonFileNameList, ref animationList);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Parsing failed for " + fullPath + "\n" + e.ToString());
            }
        }

        void FileDiscovered(byte[] byteChunk, PackFileContainer container, string fullPath, ref List<string> skeletonFileNameList, ref Dictionary<string, List<AnimationReference>> animationList)
        {
            if (IsKnownBrokenAnimation(fullPath))
                return;

            try
            {
                var animationSkeletonName = AnimationFile.GetAnimationName(byteChunk);
                AddAnimation(animationSkeletonName, fullPath, container, ref skeletonFileNameList, ref animationList);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Parsing failed for " + fullPath + "\n" + e.ToString());
            }
        }

        bool IsKnownBrokenAnimation(string fullPath)
        {
            if (Debugger.IsAttached)
            {
                var brokenAnims = new string[] { "rigidmodels\\buildings\\roman_aqueduct_straight\\roman_aqueduct_straight_piece01_destruct01_anim.anim" };
                if (brokenAnims.Contains(fullPath))
                {
                    _logger.Here().Warning("Skipping loading of known broken file - " + fullPath);
                    return true;
                }
            }
            return false;
        }

        void AddAnimation(string skeletonName, string fullPath, PackFileContainer container, ref List<string> skeletonFileNameList, ref Dictionary<string, List<AnimationReference>> animationList)
        {
            lock (_threadLock)
            {
               var newEntry = new ObservableCollection<AnimationReference>() { new AnimationReference(fullPath, container) };
               if (animationList.ContainsKey(skeletonName) == false)
                   animationList[skeletonName] = [];
               animationList[skeletonName].Add(new AnimationReference(fullPath, container));

                if (fullPath.Contains("animations\\skeletons", StringComparison.InvariantCultureIgnoreCase))
                    skeletonFileNameList.Add(fullPath);
                else if (fullPath.Contains("tech", StringComparison.InvariantCultureIgnoreCase))
                    skeletonFileNameList.Add(fullPath);
            }
        }

        void UnloadAnimationFromContainer(PackFileContainer packFileContainer)
        {
            lock (_threadLock)
            {
                var itemsRemoved = 0;
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


        public ObservableCollection<AnimationReference> GetAnimationsForSkeleton(string skeletonName)
        {
            if (_skeletonNameToAnimationMap.ContainsKey(skeletonName) == false)
                _skeletonNameToAnimationMap[skeletonName] = [];
            return _skeletonNameToAnimationMap[skeletonName];
        }

        public ObservableCollection<string> GetAllSkeletonFileNames() => _skeletonFileNames;

        public AnimationFile? GetSkeletonFileFromName(string skeletonName)
        {
            lock (_threadLock)
            {
                foreach (var name in _skeletonFileNames)
                {
                    if (name.Contains(skeletonName))
                    {
                        var fullName = Path.GetFileNameWithoutExtension(name);
                        var lookUpFullName = Path.GetFileNameWithoutExtension(skeletonName);

                        var file = _packFileService.FindFile(name);
                        if (file != null && fullName == lookUpFullName)
                        {
                            // Make sure its not a tech skeleton
                            if (_packFileService.GetFullPath(file).Contains("tech") == false)
                                return AnimationFile.Create(file);
                        }
                    }
                }

                // Try loading from path as a backup in case loading failed. Looking at you wh3...
                var path = $"animations\\skeletons\\{skeletonName}.anim";
                var animationFile = _packFileService.FindFile(path);
                if (animationFile != null)
                    return AnimationFile.Create(animationFile);
                return null;
            }
        }

        public AnimationReference? FindAnimationRefFromPackFile(PackFile animation)
        {
            lock (_threadLock)
            {
                var fullPath = _packFileService.GetFullPath(animation);
                foreach (var entry in _skeletonNameToAnimationMap.Values)
                {
                    foreach (var s in entry)
                    {
                        var res = string.Compare(s.AnimationFile, fullPath, StringComparison.InvariantCultureIgnoreCase);
                        if (res == 0)
                            return s;
                    }
                }

                var f = _packFileService.FindFile(fullPath);
                if (f != null)
                {
                    var pf = _packFileService.GetPackFileContainer(animation);
                    return new AnimationReference(fullPath, pf);
                }
                return null;
            }
        }


        // Delete this piece of shit

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
