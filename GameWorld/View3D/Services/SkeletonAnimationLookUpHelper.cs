using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
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
            foreach (var container in containers)
                LoadFromPackFileContainer(container);
        }

        public void Dispose()
        {
            _globalEventHub.UnRegister(this);
        }

        void PackfileContainerRefresh(PackFileContainer packFileContainer)
        {
            UnloadAnimationFromContainer(packFileContainer);
            LoadFromPackFileContainer(packFileContainer);
        }

        void PackfileContainerRemove(PackFileContainer packFileContainer)
        {
            UnloadAnimationFromContainer(packFileContainer);
        }

        void LoadFromPackFileContainer(PackFileContainer packFileContainer)
        {
            List<string> skeletonFileNameList = [];
            Dictionary<string, List<AnimationReference>> animationList = [];

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

            Parallel.For(0, groupedAnims.Count, index =>
            {
                using var stream = new FileStream(groupedAnims[index].Key, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                foreach (var file in groupedAnims[index])
                {
                    var bytes = file.DataSource.ReadData(stream);
                    if (bytes.Length > 100)
                        Array.Resize(ref bytes, 100);

                    FileDiscovered(bytes, packFileContainer, file.FullPath, ref skeletonFileNameList, ref animationList);
                }
            });

            // Handle all in memory files 
            Parallel.For(0, allAnimsOtherFiles.Count, index =>
            {
                var animation = allAnimations[index];
                FileDiscovered(animation.Pack.DataSource.ReadData(100), packFileContainer, animation.FileName, ref skeletonFileNameList, ref animationList);
            });

            foreach (var skeleton in skeletonFileNameList)
                _skeletonFileNames.Add(skeleton);

            foreach (var animation in animationList)
            {
                if (_skeletonNameToAnimationMap.ContainsKey(animation.Key) == false)
                    _skeletonNameToAnimationMap[animation.Key] = [];
                foreach (var animationReference in animation.Value)
                    _skeletonNameToAnimationMap[animation.Key].Add(animationReference);
            }
        }

        void FileDiscovered(byte[] byteChunk, PackFileContainer container, string fullPath, ref List<string> skeletonFileNameList, ref Dictionary<string, List<AnimationReference>> animationList)
        {
            // Skip broken animations, as the errors are annoying when the debuger is attached.
            if (Debugger.IsAttached)
            {
                var brokenAnims = new string[]
                {
                     "rigidmodels\\buildings\\roman_aqueduct_straight\\roman_aqueduct_straight_piece01_destruct01_anim.anim",
                     "animations\\battle\\raptor02\\subset\\colossal_squig\\deaths\\rp2_colossalsquig_death_01.anim",
                     "animations\\battle\\humanoid13b\\golgfag\\docking\\hu13b_golgfag_docking_armed_02.anim",
                     "animations\\battle\\humanoid13\\ogre\\rider\\hq3b_stonehorn_wb\\sword_and_crossbow\\missile_action\\crossbow\\hu13_hq3b_swc_rider1_shoot_back_crossbow_01.anim",
                     "animations\\battle\\humanoid13\\ogre\\rider\\hq3b_stonehorn_wb\\sword_and_crossbow\\missile_action\\crossbow\\hu13_hq3b_swc_rider1_reload_crossbow_01.anim",
                     "animations\\battle\\humanoid13\\ogre\\rider\\hq3b_stonehorn_wb\\sword_and_crossbow\\missile_action\\crossbow\\hu13_hq3b_sp_rider1_shoot_ready_crossbow_01.anim"
                };
                if (brokenAnims.Contains(fullPath))
                {
                    _logger.Here().Warning("Skipping loading of known broken file - " + fullPath);
                    return;
                }
            }

            try
            {
                if (byteChunk.Length == 0)
                {
                    throw new Exception("File empty.");
                }
                var animationSkeletonName = AnimationFile.GetAnimationName(byteChunk);

                lock (_threadLock)
                {
                    var newEntry = new ObservableCollection<AnimationReference>() { new AnimationReference(fullPath, container) };
                    if (animationList.ContainsKey(animationSkeletonName) == false)
                        animationList[animationSkeletonName] = [];
                    animationList[animationSkeletonName].Add(new AnimationReference(fullPath, container));

                    if (fullPath.Contains("animations\\skeletons", StringComparison.InvariantCultureIgnoreCase))
                        skeletonFileNameList.Add(fullPath);
                    else if (fullPath.Contains("tech", StringComparison.InvariantCultureIgnoreCase))
                        skeletonFileNameList.Add(fullPath);
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error("Parsing failed for " + fullPath + "\n" + e.ToString());
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
