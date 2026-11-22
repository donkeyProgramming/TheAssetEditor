using System;
using System.Collections.Generic;
using System.Linq;
using AnimationEditor.MountAnimationCreator.ViewModels;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3;
using static Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry;

namespace Editors.AnimationVisualEditors.MountAnimationCreator.Services
{
    /// <summary>One mounted rider animation: its RIDER_ slot, and the mount animation of the matching mount slot.</summary>
    public record RiderSlotAnimation(string RiderSlot, AnimationClip Rider, AnimationClip Mount, GameSkeleton RiderSkeleton, GameSkeleton MountSkeleton);

    /// <summary>What to do with one rider animation: <see cref="Clip"/> is saved in its place, or with no clip the
    /// set keeps the original and <see cref="Note"/> says why.</summary>
    public record RiderSlotResult(AnimationClip Clip, string Note)
    {
        public static RiderSlotResult Keep(string why) => new(null, why);
    }

    /// <summary>
    /// Makes a new animation set from a rider set, passing each mounted RIDER_ animation through a processor. Every
    /// slot and variant of the rider set is kept, and the mount set is named as its mount.
    /// </summary>
    public class RiderSetBatchService
    {
        readonly IPackFileService _pfs;
        readonly IFileSaveService _fileSaveService;
        readonly ISkeletonAnimationLookUpHelper _skeletonLookUp;
        readonly AnimationOutputSettings _output;

        public RiderSetBatchService(IPackFileService pfs, IFileSaveService fileSaveService, ISkeletonAnimationLookUpHelper skeletonLookUp, AnimationOutputSettings output)
        {
            _pfs = pfs;
            _fileSaveService = fileSaveService;
            _skeletonLookUp = skeletonLookUp;
            _output = output;
        }

        public static string AnimPackPath(string setName) => $@"animations\database\battle\bin\{setName}.animpack";

        public ErrorList Process(IAnimationBinGenericFormat riderSet, IAnimationBinGenericFormat mountSet, string newSetName, Func<RiderSlotAnimation, RiderSlotResult> processor)
        {
            var log = new ErrorList();
            if (riderSet is not AnimationBinWh3 riderBin)
            {
                log.Error(riderSet.Name, "Only Warhammer III animation sets can be processed");
                return log;
            }

            var riderSkeleton = new GameSkeleton(_skeletonLookUp.GetSkeletonFileFromName(riderSet.SkeletonName), null);
            var mountSkeleton = new GameSkeleton(_skeletonLookUp.GetSkeletonFileFromName(mountSet.SkeletonName), null);
            var mountSlots = mountSet.Entries.GroupBy(x => x.SlotName).ToDictionary(x => x.Key, x => x.ToList());
            // Slots often share an animation: one new file per rider and mount animation pair
            var made = new Dictionary<(string Rider, string Mount), string>(new PathPairComparer());
            // A rider animation shared with different mount animations is made more than once, under different names
            var madePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var newBin = new AnimationBinWh3($"animations/database/battle/bin/{newSetName}.bin")
            {
                TableVersion = riderBin.TableVersion,
                TableSubVersion = riderBin.TableSubVersion,
                Name = newSetName,
                MountBin = mountSet.Name,
                Unknown = riderBin.Unknown,
                SkeletonName = riderBin.SkeletonName,
                LocomotionGraph = riderBin.LocomotionGraph,
                UnknownValue1 = riderBin.UnknownValue1,
            };

            foreach (var entry in riderBin.AnimationTableEntries)
            {
                var slot = AnimationSlotTypeHelperWh3.GetFromId((int)entry.AnimationId).Value;
                var newEntry = new AnimationBinEntry
                {
                    AnimationId = entry.AnimationId,
                    BlendIn = entry.BlendIn,
                    SelectionWeight = entry.SelectionWeight,
                    WeaponBools = entry.WeaponBools,
                    Unk = entry.Unk,
                };
                newBin.AnimationTableEntries.Add(newEntry);

                for (var variant = 0; variant < entry.AnimationRefs.Count; variant++)
                {
                    var animation = entry.AnimationRefs[variant];
                    var newRef = new AnimationRef { AnimationFile = animation.AnimationFile, AnimationMetaFile = animation.AnimationMetaFile, AnimationSoundMetaFile = animation.AnimationSoundMetaFile };
                    newEntry.AnimationRefs.Add(newRef);

                    var item = entry.AnimationRefs.Count == 1 ? slot : $"{slot} [{variant + 1}]";
                    var mountSlot = slot.StartsWith("RIDER_", StringComparison.OrdinalIgnoreCase) ? slot[6..] : null;
                    if (mountSlot == null || !mountSlots.TryGetValue(mountSlot, out var mountVariants))
                    {
                        log.Ok(item, mountSlot == null ? "Kept: not a mounted slot" : $"Kept: the mount set has no {mountSlot}");
                        continue;
                    }

                    // A mount slot with as many variants as the rider's lines up variant for variant
                    var mountAnimation = mountVariants[variant < mountVariants.Count ? variant : 0].AnimationFile;
                    var riderClip = LoadClip(animation.AnimationFile, riderSkeleton);
                    var mountClip = LoadClip(mountAnimation, mountSkeleton);
                    if (riderClip == null || mountClip == null)
                    {
                        log.Warning(item, $"Kept: can't find {(riderClip == null ? animation.AnimationFile : mountAnimation)}");
                        continue;
                    }

                    RiderSlotResult result;
                    try
                    {
                        result = processor(new RiderSlotAnimation(slot, riderClip, mountClip, riderSkeleton, mountSkeleton));
                    }
                    catch (Exception e)
                    {
                        log.Error(item, $"Kept: {e.Message}");
                        continue;
                    }

                    if (result.Clip == null)
                    {
                        log.Ok(item, "Kept: " + result.Note);
                        continue;
                    }

                    // Checked after the processor, which may keep a shared animation in one slot and remake it in another
                    if (made.TryGetValue((animation.AnimationFile, mountAnimation), out var madeEarlier))
                    {
                        newRef.AnimationFile = madeEarlier;
                        log.Ok(item, $"Uses {madeEarlier}, made for an earlier slot");
                        continue;
                    }

                    newRef.AnimationFile = _output.Save(_pfs, _fileSaveService, _skeletonLookUp, result.Clip, riderSkeleton, animation.AnimationFile, madePaths);
                    made[(animation.AnimationFile, mountAnimation)] = newRef.AnimationFile;
                    log.Ok(item, $"Saved {newRef.AnimationFile}{(string.IsNullOrEmpty(result.Note) ? "" : " (" + result.Note + ")")}");
                }
            }

            foreach (var mountSlot in mountSlots.Keys.Where(x => riderSet.Entries.All(r => !string.Equals(r.SlotName, "RIDER_" + x, StringComparison.OrdinalIgnoreCase))))
            {
                if (AnimationSlotTypeHelperWh3.GetfromValue("RIDER_" + mountSlot) != null)
                    log.Warning("RIDER_" + mountSlot, $"The rider set has no animation for the mount's {mountSlot}");
            }

            var animPack = new AnimationPackFileDatabase(newSetName);
            animPack.AddFile(newBin);
            _fileSaveService.Save(AnimPackPath(newSetName), AnimationPackSerializer.ConvertToBytes(animPack), false);
            return log;
        }

        // Bins write paths with either slash
        class PathPairComparer : IEqualityComparer<(string, string)>
        {
            static string Normalise(string path) => path?.Replace('/', '\\').ToLowerInvariant();
            public bool Equals((string, string) x, (string, string) y) => Normalise(x.Item1) == Normalise(y.Item1) && Normalise(x.Item2) == Normalise(y.Item2);
            public int GetHashCode((string, string) obj) => HashCode.Combine(Normalise(obj.Item1), Normalise(obj.Item2));
        }

        AnimationClip LoadClip(string path, GameSkeleton skeleton)
        {
            var file = string.IsNullOrWhiteSpace(path) ? null : _pfs.FindFile(path);
            return file == null ? null : new AnimationClip(AnimationFile.Create(file), skeleton);
        }
    }
}
