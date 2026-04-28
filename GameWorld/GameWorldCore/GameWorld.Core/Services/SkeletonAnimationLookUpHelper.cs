using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.Animation;

namespace GameWorld.Core.Services
{
    public interface ISkeletonAnimationLookUpHelper
    {
        void Dispose();
        AnimationReference? FindAnimationRefFromPackFile(PackFile animation);
        ObservableCollection<string> GetAllSkeletonFileNames();
        ObservableCollection<AnimationReference> GetAnimationsForSkeleton(string skeletonName);
        AnimationFile? GetSkeletonFileFromName(string skeletonName);
    }
    public class SkeletonAnimationLookUpHelper : IDisposable, ISkeletonAnimationLookUpHelper
    {
        private readonly ILogger _logger = Logging.Create<SkeletonAnimationLookUpHelper>();
        private readonly object _threadLock = new object();

        private readonly IPackFileService _packFileService;
        private readonly IGlobalEventHub _globalEventHub;
        private readonly Task _initialIndexTask;
        private volatile bool _isDisposed;

        private readonly Dictionary<string, ObservableCollection<AnimationReference>> _skeletonNameToAnimationMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> _skeletonNameToSkeletonPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _animationPathToSkeletonName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AnimationReference> _animationPathToReference = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<IPackFileContainer, HashSet<string>> _containerToAnimationPaths = [];
        private readonly Dictionary<IPackFileContainer, HashSet<string>> _containerToSkeletonPaths = [];
        private readonly ObservableCollection<string> _skeletonFileNames = [];

        private static readonly HashSet<string> BrokenFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "rigidmodels\\buildings\\roman_aqueduct_straight\\roman_aqueduct_straight_piece01_destruct01_anim.anim",
            "animations\\battle\\raptor02\\subset\\colossal_squig\\deaths\\rp2_colossalsquig_death_01.anim",
            "animations\\battle\\humanoid13b\\golgfag\\docking\\hu13b_golgfag_docking_armed_02.anim",
            "animations\\battle\\humanoid13\\ogre\\rider\\hq3b_stonehorn_wb\\sword_and_crossbow\\missile_action\\crossbow\\hu13_hq3b_swc_rider1_shoot_back_crossbow_01.anim",
            "animations\\battle\\humanoid13\\ogre\\rider\\hq3b_stonehorn_wb\\sword_and_crossbow\\missile_action\\crossbow\\hu13_hq3b_swc_rider1_reload_crossbow_01.anim",
            "animations\\battle\\humanoid13\\ogre\\rider\\hq3b_stonehorn_wb\\sword_and_crossbow\\missile_action\\crossbow\\hu13_hq3b_sp_rider1_shoot_ready_crossbow_01.anim",
            "animations\\battle\\humanoid01c\\sayl_staff_and_skull\\stand\\props\\hu1c_sayl_staff_and_skull_staff_stand_idle_02.anim"
        };

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

            // Initialize in background so startup is not blocked.
            _initialIndexTask = Task.Run(LoadAllContainersInBackground);
        }

        public void Dispose()
        {
            _isDisposed = true;
            _globalEventHub.UnRegister(this);
        }

        void PackfileContainerRefresh(IPackFileContainer packFileContainer)
        {
            RunContainerUpdateInBackground(() =>
            {
                UnloadAnimationFromContainer(packFileContainer);
                LoadFromPackFileContainer(packFileContainer);
            });
        }

        void PackfileContainerRemove(IPackFileContainer packFileContainer)
        {
            RunContainerUpdateInBackground(() => UnloadAnimationFromContainer(packFileContainer));
        }

        void LoadAllContainersInBackground()
        {
            var stopwatch = Stopwatch.StartNew();
            var containers = _packFileService.GetAllPackfileContainers();
            foreach (var container in containers)
            {
                if (_isDisposed)
                    return;
                LoadFromPackFileContainer(container);
            }

            stopwatch.Stop();
            _logger.Here().Information(
                "Skeleton animation initial load completed in {ElapsedMs}ms for {ContainerCount} containers",
                stopwatch.ElapsedMilliseconds,
                containers.Count);
        }

        void RunContainerUpdateInBackground(Action action)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    WaitForInitialLoad();
                    if (_isDisposed)
                        return;
                    action();
                }
                catch (Exception e)
                {
                    _logger.Here().Error("Skeleton animation lookup update failed\n" + e);
                }
            });
        }

        void WaitForInitialLoad(string callerName = "Unknown")
        {
            if (_initialIndexTask.IsCompleted)
                return;

            var stopwatch = Stopwatch.StartNew();
            _initialIndexTask.GetAwaiter().GetResult();
            stopwatch.Stop();

            _logger.Here().Information(
                "Skeleton animation lookup waited {ElapsedMs}ms in {CallerName}",
                stopwatch.ElapsedMilliseconds,
                callerName);
        }

        void LoadFromPackFileContainer(IPackFileContainer packFileContainer)
        {
            var discovered = DiscoverFromPackFileContainer(packFileContainer);

            lock (_threadLock)
            {
                if (_containerToAnimationPaths.ContainsKey(packFileContainer) == false)
                    _containerToAnimationPaths[packFileContainer] = [];
                if (_containerToSkeletonPaths.ContainsKey(packFileContainer) == false)
                    _containerToSkeletonPaths[packFileContainer] = [];

                foreach (var skeletonPath in discovered.SkeletonFileNames)
                {
                    _skeletonFileNames.Add(skeletonPath);
                    _containerToSkeletonPaths[packFileContainer].Add(skeletonPath);

                    var skeletonLookupName = Path.GetFileNameWithoutExtension(skeletonPath);
                    if (_skeletonNameToSkeletonPaths.ContainsKey(skeletonLookupName) == false)
                        _skeletonNameToSkeletonPaths[skeletonLookupName] = [];
                    _skeletonNameToSkeletonPaths[skeletonLookupName].Add(skeletonPath);
                }

                foreach (var animation in discovered.AnimationsBySkeletonName)
                {
                    if (_skeletonNameToAnimationMap.ContainsKey(animation.Key) == false)
                        _skeletonNameToAnimationMap[animation.Key] = [];

                    foreach (var animationReference in animation.Value)
                    {
                        _skeletonNameToAnimationMap[animation.Key].Add(animationReference);
                        _containerToAnimationPaths[packFileContainer].Add(animationReference.AnimationFile);
                        _animationPathToSkeletonName[animationReference.AnimationFile] = animation.Key;
                        _animationPathToReference[animationReference.AnimationFile] = animationReference;
                    }
                }
            }
        }

        (List<string> SkeletonFileNames, Dictionary<string, List<AnimationReference>> AnimationsBySkeletonName) DiscoverFromPackFileContainer(IPackFileContainer packFileContainer)
        {
            var stopwatch = Stopwatch.StartNew();
            var skeletonFileNameList = new ConcurrentBag<string>();
            var animationList = new ConcurrentDictionary<string, ConcurrentBag<AnimationReference>>(StringComparer.OrdinalIgnoreCase);

            var allAnimations = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_packFileService, ".anim", packFileContainer);

            // Split animations in to two categories.
            // One for packfiles which are saved to disk, and one for in memory. 
            // Disk is the slow version, so we handle them specially 
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
            // https://www.jacksondunstan.com/articles/3568
            var groupedAnims = allAnimsInSavedPackedFiles
                .GroupBy(x => x.DataSource.Parent.FilePath)
                .ToList();

            Parallel.ForEach(groupedAnims, group =>
            {
                using var stream = new FileStream(group.Key, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                foreach (var file in group)
                {
                    var bytes = file.DataSource.ReadData(stream);
                    if (bytes.Length > 100)
                        Array.Resize(ref bytes, 100);

                    FileDiscovered(bytes, packFileContainer, file.FullPath, skeletonFileNameList, animationList);
                }
            });

            // Handle all in memory files 
            Parallel.ForEach(allAnimsOtherFiles, animation =>
            {
                FileDiscovered(animation.DataSource.PeekData(100), packFileContainer, animation.FullPath, skeletonFileNameList, animationList);
            });

            var resultAnimations = animationList.ToDictionary(
                x => x.Key,
                x => x.Value.ToList(),
                StringComparer.OrdinalIgnoreCase);

            stopwatch.Stop();
            _logger.Here().Information(
                "Skeleton animation container discovery completed in {ElapsedMs}ms for [{ContainerName}] with {AnimationCount} animation refs and {SkeletonCount} skeleton files",
                stopwatch.ElapsedMilliseconds,
                packFileContainer.Name,
                resultAnimations.Values.Sum(x => x.Count),
                skeletonFileNameList.Count);

            return (skeletonFileNameList.ToList(), resultAnimations);
        }

        void FileDiscovered(
            byte[] byteChunk,
            IPackFileContainer container,
            string fullPath,
            ConcurrentBag<string> skeletonFileNameList,
            ConcurrentDictionary<string, ConcurrentBag<AnimationReference>> animationList)
        {
            if (BrokenFiles.Contains(fullPath))
            {
                _logger.Here().Warning("Skipping loading of known broken file - " + fullPath);
                return;
            }

            try
            {
                if (byteChunk.Length == 0)
                {
                    throw new Exception("File empty.");
                }

                var animationSkeletonName = AnimationFile.GetAnimationName(byteChunk);
                var animationReference = new AnimationReference(fullPath, container);
                animationList.GetOrAdd(animationSkeletonName, _ => []).Add(animationReference);

                if (fullPath.Contains("animations\\skeletons", StringComparison.OrdinalIgnoreCase)
                    || fullPath.Contains("tech", StringComparison.OrdinalIgnoreCase))
                    skeletonFileNameList.Add(fullPath);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Parsing failed for " + fullPath + "\n" + e.ToString());
            }
        }

        void UnloadAnimationFromContainer(IPackFileContainer packFileContainer)
        {
            lock (_threadLock)
            {
                if (_containerToAnimationPaths.TryGetValue(packFileContainer, out var animationPaths))
                {
                    foreach (var animationPath in animationPaths)
                    {
                        if (_animationPathToSkeletonName.TryGetValue(animationPath, out var skeletonName)
                            && _skeletonNameToAnimationMap.TryGetValue(skeletonName, out var entries)
                            && _animationPathToReference.TryGetValue(animationPath, out var reference))
                        {
                            entries.Remove(reference);
                        }

                        _animationPathToSkeletonName.Remove(animationPath);
                        _animationPathToReference.Remove(animationPath);
                    }

                    _containerToAnimationPaths.Remove(packFileContainer);
                }

                if (_containerToSkeletonPaths.TryGetValue(packFileContainer, out var skeletonPaths))
                {
                    foreach (var skeletonPath in skeletonPaths)
                    {
                        _skeletonFileNames.Remove(skeletonPath);

                        var skeletonLookupName = Path.GetFileNameWithoutExtension(skeletonPath);
                        if (_skeletonNameToSkeletonPaths.TryGetValue(skeletonLookupName, out var mappedPaths))
                        {
                            mappedPaths.RemoveAll(x => string.Equals(x, skeletonPath, StringComparison.OrdinalIgnoreCase));
                            if (mappedPaths.Count == 0)
                                _skeletonNameToSkeletonPaths.Remove(skeletonLookupName);
                        }
                    }

                    _containerToSkeletonPaths.Remove(packFileContainer);
                }
            }
        }

        public ObservableCollection<AnimationReference> GetAnimationsForSkeleton(string skeletonName)
        {
            lock (_threadLock)
            {
                if (_skeletonNameToAnimationMap.TryGetValue(skeletonName, out var existingAnimations))
                    return existingAnimations;
            }

            WaitForInitialLoad(nameof(GetAnimationsForSkeleton));

            lock (_threadLock)
            {
                if (_skeletonNameToAnimationMap.TryGetValue(skeletonName, out var loadedAnimations))
                    return loadedAnimations;

                _skeletonNameToAnimationMap[skeletonName] = [];
                return _skeletonNameToAnimationMap[skeletonName];
            }
        }

        public ObservableCollection<string> GetAllSkeletonFileNames()
        {
            WaitForInitialLoad(nameof(GetAllSkeletonFileNames));
            return _skeletonFileNames;
        }

        public AnimationFile? GetSkeletonFileFromName(string skeletonName)
        {
            WaitForInitialLoad(nameof(GetSkeletonFileFromName));

            lock (_threadLock)
            {
                var lookUpFullName = Path.GetFileNameWithoutExtension(skeletonName);
                if (_skeletonNameToSkeletonPaths.TryGetValue(lookUpFullName, out var skeletonPaths))
                {
                    foreach (var name in skeletonPaths)
                    {
                        var fullName = Path.GetFileNameWithoutExtension(name);
                        var file = _packFileService.FindFile(name);
                        if (file != null && fullName == lookUpFullName)
                        {
                            // Make sure its not a tech skeleton
                            if (_packFileService.GetFullPath(file).Contains("tech", StringComparison.OrdinalIgnoreCase) == false)
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
            WaitForInitialLoad(nameof(FindAnimationRefFromPackFile));

            lock (_threadLock)
            {
                var fullPath = _packFileService.GetFullPath(animation);
                if (_animationPathToReference.TryGetValue(fullPath, out var existingReference))
                    return existingReference;

                var f = _packFileService.FindFile(fullPath);
                if (f != null)
                {
                    var pf = _packFileService.GetPackFileContainer(animation);
                    if (pf != null)
                        return new AnimationReference(fullPath, pf);
                }
                return null;
            }
        }
    }

    // Delete this piece of shit
    public class AnimationReference
    {
        public AnimationReference(string animationFile, IPackFileContainer container)
        {
            AnimationFile = animationFile;
            Container = container;
        }
        public string AnimationFile { get; set; }
        public IPackFileContainer Container { get; set; }

        public override string ToString()
        {
            return $"[{Container?.Name}] {AnimationFile}";
        }
    }
}
