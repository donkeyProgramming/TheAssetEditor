using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameWorld.Core.Animation;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationPack;

namespace Editors.CampaignAnimationSetEditor.Services
{
    /// <summary>Builds a complete <see cref="CampaignAnimationBin"/> from an existing battle
    /// animation set. The status/slot layout and defaults come from a survey of 661 vanilla WH3
    /// cam_*.bin files rather than guesswork - see the PR description.
    ///
    /// Rider and flyer variants are auto-detected from the fragment: a non-empty MountBin means a
    /// rider (use the RIDER_ slot vocabulary), and a FLY_STAND slot means a flyer (use FLY_ slots
    /// plus the PERSISTENT_METADATA_FLYING overlay). Flyers often lack dedicated FLY_ slots for
    /// combat idle/locomotion/attack, so FLY_STAND is the fallback for any that are missing.</summary>
    public class CampaignAnimationSetGeneratorService
    {
        static readonly string[] StanceAndDeployStatusNames =
        [
            "status_stance_march", "status_stance_ambush", "status_stance_raid", "status_stance_camp",
            "status_stance_muster", "status_stance_siege", "status_stance_blockade", "status_stance_channeling",
            "status_stance_set_camp_raiding", "status_stance_tunneling", "status_stance_raise_dead",
            "status_deploy",
        ];

        static readonly (string BattleSlot, string CampaignDock)[] DockSlotMap =
        [
            ("DOCK_EQUIPMENT_RIGHT_HAND", "DOCK_EQPT_RHAND"),
            ("DOCK_EQUIPMENT_LEFT_HAND", "DOCK_EQPT_LHAND"),
            ("DOCK_EQUIPMENT_LEFT_WAIST", "DOCK_EQPT_LWAIST"),
            ("DOCK_EQUIPMENT_RIGHT_WAIST", "DOCK_EQPT_RWAIST"),
            ("DOCK_EQUIPMENT_BACK", "DOCK_EQPT_BACK"),
        ];

        // Cast-spell slots use a "_FLYING" suffix rather than the usual "FLY_" prefix, and have no
        // single canonical slot - only short/medium/long forward/up variants. Medium is preferred.
        static readonly string[] CastSpellBaseNames = ["CAST_SPELL_FORWARD_MEDIUM", "CAST_SPELL_UP_MEDIUM", "CAST_SPELL_FORWARD_SHORT", "CAST_SPELL_UP_SHORT", "CAST_SPELL_FORWARD_LONG", "CAST_SPELL_UP_LONG"];

        readonly IPackFileService _packFileService;
        readonly FreezeRootBoneCommand _freezeRootBoneCommand;

        public CampaignAnimationSetGeneratorService(IPackFileService packFileService, FreezeRootBoneCommand freezeRootBoneCommand)
        {
            _packFileService = packFileService;
            _freezeRootBoneCommand = freezeRootBoneCommand;
        }

        /// <summary>A frozen animation clip computed during Generate() but not yet written.</summary>
        public record PendingAnimationWrite(string Path, byte[] Bytes);

        /// <summary>Frozen clips are returned rather than written during Generate(), so the caller
        /// can commit them alongside the .bin itself. Generating a set and then never saving would
        /// otherwise leave orphaned .anim files behind.</summary>
        public record GenerateResult(CampaignAnimationBin Bin, IReadOnlyList<PendingAnimationWrite> PendingAnimationWrites);

        /// <param name="freezeRootForRiderAnimations">Riders only. Older rider rigs bake the mount's
        /// movement into every animation, needing the root bone zeroed throughout to sit correctly on
        /// the campaign map; newer WH3 rigs are authored in place, so this defaults to off. Locomotion
        /// is always frozen regardless.</param>
        /// <param name="forceGroundAnimations">Overrides flying auto-detection, for a flying-capable
        /// unit wanted walking on the campaign map. No effect on units with no flying slots.</param>
        public GenerateResult Generate(IAnimationBinGenericFormat battleFragment, string reference, bool freezeRootForRiderAnimations = false, bool forceGroundAnimations = false)
        {
            var pendingWrites = new List<PendingAnimationWrite>();

            var skeletonName = battleFragment.SkeletonName;
            var skeletonFile = _packFileService.FindFile($"animations/skeletons/{skeletonName}.anim")
                ?? throw new Exception($"Could not find a skeleton file for '{skeletonName}' under animations/skeletons/");

            var skeletonAnimFile = AnimationFile.Create(skeletonFile);
            var animationPlayer = new AnimationPlayer();
            var gameSkeleton = new GameSkeleton(skeletonAnimFile, animationPlayer);
            var rootBoneIndex = gameSkeleton.BoneNames.FindIndex(x => string.Equals(x, "animroot", StringComparison.OrdinalIgnoreCase));

            var entries = battleFragment.Entries;

            var isRider = !string.IsNullOrWhiteSpace(battleFragment.MountBin);
            var ridPrefix = isRider ? "RIDER_" : "";
            var isFlying = !forceGroundAnimations && IsFlyingMoveset(entries, ridPrefix);
            var flyPrefix = isFlying ? "FLY_" : "";

            // STAND is the most neutral pose a skeleton has (STAND_IDLE_* are fidget variations
            // played while standing), and exists in all four ground/rider/flying combinations.
            var standEntry = FindEntry(entries,
                [ridPrefix + flyPrefix + "STAND"],
                [ridPrefix + flyPrefix + "STAND_IDLE_", ridPrefix + "STAND_"])
                ?? entries.FirstOrDefault();

            // The idle-fidget family is named STAND_IDLE_1..10 on the ground but FLY_IDLE_1..5 when
            // flying - FLY_STAND_IDLE_ doesn't exist.
            var idleFidgetPrefix = ridPrefix + (isFlying ? "FLY_IDLE_" : "STAND_IDLE_");
            var combatIdlePrefix = ridPrefix + flyPrefix + "COMBAT_IDLE_";

            var selectionEntry = entries.FirstOrDefault(e => e.SlotName.StartsWith(idleFidgetPrefix, StringComparison.OrdinalIgnoreCase)) ?? standEntry;

            // status_battle.Idle gets every combat-ready idle available rather than one reused pose.
            var battleIdleEntries = entries
                .Where(e => e.SlotName.StartsWith(idleFidgetPrefix, StringComparison.OrdinalIgnoreCase) || e.SlotName.StartsWith(combatIdlePrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (battleIdleEntries.Count == 0 && standEntry != null)
                battleIdleEntries.Add(standEntry);

            var attackEntry = FindEntry(entries, [ridPrefix + flyPrefix + "ATTACK_1"], [ridPrefix + flyPrefix + "ATTACK_"]) ?? standEntry;
            var defendEntry = FindEntry(entries, [ridPrefix + flyPrefix + "DEFEND_1"], [ridPrefix + flyPrefix + "DEFEND_"]) ?? standEntry;
            var deathEntry = FindEntry(entries,
                [ridPrefix + "DEATH_STAND_1"],
                [ridPrefix + "DEATH_STAND_", ridPrefix + "KNOCKDOWN_DEATH_", ridPrefix + "DEATH_FACE_"])
                ?? standEntry;

            // No rider+flying cast-spell slot exists, so riders always use the plain RIDER_ variant.
            var castSpellNames = isRider
                ? CastSpellBaseNames.Select(n => "RIDER_" + n).ToArray()
                : isFlying
                    ? CastSpellBaseNames.Select(n => n + "_FLYING").Concat(CastSpellBaseNames).ToArray()
                    : CastSpellBaseNames;
            var castSpellPrefixes = isRider ? new[] { "RIDER_CAST_SPELL_" } : new[] { "CAST_SPELL_FORWARD_", "CAST_SPELL_UP_" };
            var castSpellEntry = FindEntry(entries, castSpellNames, castSpellPrefixes);

            // Non-locomotion animations are only re-exported through the freeze-root-bone transform
            // for riders that opted in; everything else references the original battle file.
            var freezeNonLocomotion = isRider && freezeRootForRiderAnimations;
            var resolvedPathCache = new Dictionary<string, string>();
            string ResolvePath(string sourcePath) => ResolveAnimationPath(sourcePath, freezeNonLocomotion, gameSkeleton, rootBoneIndex, resolvedPathCache, pendingWrites);

            var bin = new CampaignAnimationBin
            {
                Reference = reference,
                SkeletonName = skeletonName,
                Version = 3,
            };

            var statusNormal = new CampaignAnimationBin.StatusItem { Name = "status_normal" };
            if (standEntry != null)
                statusNormal.Idle = [ToAnimationEntry(standEntry, "status_normal", ResolvePath)];
            if (selectionEntry != null)
                statusNormal.Selection = [ToAnimationEntry(selectionEntry, "status_normal", ResolvePath)];

            // One Locomotion entry only. RUN_1 is preferred (337 "run" vs 50 "walk" entries in the
            // survey), falling back to WALK_1. Flyers often lack FLY_WALK_1, so reuse FLY_STAND.
            AnimationBinEntryGenericFormat? locomotionEntry = isFlying
                ? entries.FirstOrDefault(e => string.Equals(e.SlotName, ridPrefix + "FLY_WALK_1", StringComparison.OrdinalIgnoreCase)) ?? standEntry
                : entries.FirstOrDefault(e => string.Equals(e.SlotName, ridPrefix + "RUN_1", StringComparison.OrdinalIgnoreCase))
                    ?? entries.FirstOrDefault(e => string.Equals(e.SlotName, ridPrefix + "WALK_1", StringComparison.OrdinalIgnoreCase));

            if (locomotionEntry != null && rootBoneIndex >= 0)
            {
                var frozen = BuildFrozenLocomotionEntry(locomotionEntry, gameSkeleton, rootBoneIndex, pendingWrites);
                if (frozen != null)
                    statusNormal.Locomotion = [frozen];
            }
            bin.Status.Add(statusNormal);

            var statusBattle = new CampaignAnimationBin.StatusItem { Name = "status_battle" };
            if (battleIdleEntries.Count > 0)
                statusBattle.Idle = battleIdleEntries.Select(e => ToAnimationEntry(e, "status_normal", ResolvePath)).ToList();

            statusBattle.Action =
            [
                ToActionEntry(attackEntry, "battle_fatality", 100, ResolvePath),
                ToActionEntry(attackEntry, "battle_quick_fatality", 101, ResolvePath),
                ToActionEntry(defendEntry, "battle_draw", 102, ResolvePath),
                ToActionEntry(defendEntry, "battle_quick_draw", 103, ResolvePath),
                ToActionEntry(deathEntry, "response_battle_fatality", 104, ResolvePath),
                ToActionEntry(deathEntry, "response_battle_quick_fatality", 105, ResolvePath),
                ToActionEntry(defendEntry, "response_battle_draw", 106, ResolvePath),
                ToActionEntry(defendEntry, "response_battle_quick_draw", 107, ResolvePath),
            ];
            bin.Status.Add(statusBattle);

            // Only units with a moveset get the full stance/deploy block, matching the corpus:
            // static things like siege engines keep just status_normal/status_battle.
            if (locomotionEntry != null && selectionEntry != null)
            {
                foreach (var statusName in StanceAndDeployStatusNames)
                {
                    // Channeling gets the unit's cast-spell pose rather than the shared idle.
                    var idleSource = statusName == "status_stance_channeling" ? castSpellEntry ?? selectionEntry : selectionEntry;
                    bin.Status.Add(new CampaignAnimationBin.StatusItem
                    {
                        Name = statusName,
                        Idle = [ToAnimationEntry(idleSource, "status_normal", ResolvePath)],
                    });
                }
            }

            var docks = DockSlotMap
                .Select(map => (map.CampaignDock, Entry: entries.FirstOrDefault(e => string.Equals(e.SlotName, map.BattleSlot, StringComparison.OrdinalIgnoreCase))))
                .Where(x => x.Entry != null)
                .Select(x => new CampaignAnimationBin.PersistentMeta_Dock
                {
                    Dock = x.CampaignDock,
                    Type = "global",
                    Animation = ResolvePath(x.Entry!.AnimationFile),
                    AnimationMeta = x.Entry.MetaFile ?? "",
                    SoundMeta = x.Entry.SoundFile ?? "",
                    BlendTime = 0.5f,
                })
                .ToList();

            // PERSISTENT_METADATA_ALIVE/_FLYING are the battle-side slots for an always-blended
            // overlay animation (breathing, ambient detail), always landing in
            // global.PersitantMetaData with Type="global" (384/384 vanilla instances). Which one to
            // prefer follows the flying decision, with the other as fallback.
            var persistentEntry = isFlying
                ? FindEntry(entries, ["PERSISTENT_METADATA_FLYING"], []) ?? FindEntry(entries, ["PERSISTENT_METADATA_ALIVE"], [])
                : FindEntry(entries, ["PERSISTENT_METADATA_ALIVE"], []) ?? FindEntry(entries, ["PERSISTENT_METADATA_FLYING"], []);
            var persistentMetaData = new List<CampaignAnimationBin.PersistentMeta>();
            if (persistentEntry != null)
            {
                persistentMetaData.Add(new CampaignAnimationBin.PersistentMeta
                {
                    Animation = ResolvePath(persistentEntry.AnimationFile),
                    Type = "global",
                    AnimationMeta = persistentEntry.MetaFile ?? "",
                    SoundMeta = persistentEntry.SoundFile ?? "",
                    BlendTime = persistentEntry.BlendInTime > 0 ? persistentEntry.BlendInTime : 0.3f,
                });
            }

            if (docks.Count > 0 || persistentMetaData.Count > 0)
            {
                bin.Status.Add(new CampaignAnimationBin.StatusItem
                {
                    Name = "global",
                    Docks = docks,
                    PersitantMetaData = persistentMetaData,
                });
            }

            return new GenerateResult(bin, pendingWrites);
        }

        /// <summary>FLY_STAND alone is enough to call a unit a flyer - FLY_COMBAT_IDLE_1/FLY_WALK_1
        /// are commonly missing on genuine flyers, so requiring them rejects real flying sets.</summary>
        internal static bool IsFlyingMoveset(List<AnimationBinEntryGenericFormat> entries, string ridPrefix) =>
            entries.Any(e => string.Equals(e.SlotName, ridPrefix + "FLY_STAND", StringComparison.OrdinalIgnoreCase));

        static CampaignAnimationBin.AnimationEntry ToAnimationEntry(AnimationBinEntryGenericFormat source, string sourceStatusName, Func<string, string> resolvePath) => new()
        {
            Animation = resolvePath(source.AnimationFile),
            Type = sourceStatusName,
            MetaFile = source.MetaFile ?? "",
            SoundMeta = source.SoundFile ?? "",
            BlendTime = source.BlendInTime > 0 ? source.BlendInTime : 0.5f,
            Weight = 0,
        };

        static CampaignAnimationBin.ActionEntry ToActionEntry(AnimationBinEntryGenericFormat? source, string actionType, int actionId, Func<string, string> resolvePath)
        {
            if (source == null)
                throw new Exception("Battle animation set has no usable animation to build campaign Action entries from.");

            return new CampaignAnimationBin.ActionEntry
            {
                Animation = resolvePath(source.AnimationFile),
                Type = "status_normal",
                Meta = source.MetaFile ?? "",
                SoundMeta = source.SoundFile ?? "",
                BlendTime = 0.5f,
                ActionType = actionType,
                ActionId = actionId,
                Unknown = false,
            };
        }

        /// <summary>Distance=5 is the most common value across all 661 vanilla files for both ground
        /// (82% of 337 "run" entries) and flying clips. MinDistance=0 ("usable at any move length")
        /// is the safe choice for a single clip - a nonzero value would leave short campaign moves
        /// with nothing to play.</summary>
        CampaignAnimationBin.LocomotionEntry? BuildFrozenLocomotionEntry(AnimationBinEntryGenericFormat source, GameSkeleton skeleton, int rootBoneIndex, List<PendingAnimationWrite> pendingWrites)
        {
            var frozen = FreezeAnimation(source.AnimationFile, skeleton, rootBoneIndex);
            if (frozen == null)
                return null;

            pendingWrites.Add(frozen);

            return new CampaignAnimationBin.LocomotionEntry
            {
                Animation = frozen.Path,
                Type = "status_normal",
                AnimationMeta = source.MetaFile ?? "",
                SoundMeta = source.SoundFile ?? "",
                BlendTime = 1,
                ModelScale = 1,
                DistanceTraveled = 5,
                DistanceMinTravled = 0,
            };
        }

        /// <summary>Resolves the campaign-side path for a battle animation, optionally freezing the
        /// root bone first. Cached per source path so an animation reused across several statuses is
        /// only frozen once, queuing at most one pending write per distinct source.</summary>
        string ResolveAnimationPath(string sourcePath, bool freeze, GameSkeleton skeleton, int rootBoneIndex, Dictionary<string, string> cache, List<PendingAnimationWrite> pendingWrites)
        {
            if (!freeze || rootBoneIndex < 0)
                return sourcePath;

            if (cache.TryGetValue(sourcePath, out var cached))
                return cached;

            var frozen = FreezeAnimation(sourcePath, skeleton, rootBoneIndex);
            var resolved = frozen?.Path ?? sourcePath;
            if (frozen != null)
                pendingWrites.Add(frozen);

            cache[sourcePath] = resolved;
            return resolved;
        }

        /// <summary>Computes the frozen clip's bytes and target path without writing to the pack -
        /// see <see cref="GenerateResult"/> for why that's deferred until save.</summary>
        PendingAnimationWrite? FreezeAnimation(string sourcePath, GameSkeleton skeleton, int rootBoneIndex)
        {
            var sourceFile = _packFileService.FindFile(sourcePath);
            if (sourceFile == null)
                return null;

            var animationFile = AnimationFile.Create(sourceFile);
            var clip = new AnimationClip(animationFile, skeleton);

            if (!_freezeRootBoneCommand.Execute(clip, rootBoneIndex, "animroot", out var frozenClip) || frozenClip == null)
                return null;

            var frozenFile = frozenClip.ConvertToFileFormat(skeleton);
            var frozenBytes = AnimationFile.ConvertToBytes(frozenFile);
            var newPath = BuildCampaignAnimationPath(sourcePath);
            return new PendingAnimationWrite(newPath, frozenBytes);
        }

        /// <summary>Mirrors vanilla's convention of nesting a campaign derivative under a "campaign"
        /// subfolder with a "cam_" prefix, e.g. .../locomotion/bi2_walk_01.anim -&gt;
        /// .../locomotion/campaign/cam_bi2_walk_01.anim.</summary>
        internal static string BuildCampaignAnimationPath(string sourcePath)
        {
            var directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? "";
            var fileName = Path.GetFileName(sourcePath);
            if (!fileName.StartsWith("cam_", StringComparison.OrdinalIgnoreCase))
                fileName = "cam_" + fileName;

            return $"{directory}/campaign/{fileName}";
        }

        internal static AnimationBinEntryGenericFormat? FindEntry(List<AnimationBinEntryGenericFormat> entries, string[] exactNames, string[] prefixes)
        {
            foreach (var name in exactNames)
            {
                var match = entries.FirstOrDefault(e => string.Equals(e.SlotName, name, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return match;
            }

            foreach (var prefix in prefixes)
            {
                var match = entries.FirstOrDefault(e => e.SlotName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
