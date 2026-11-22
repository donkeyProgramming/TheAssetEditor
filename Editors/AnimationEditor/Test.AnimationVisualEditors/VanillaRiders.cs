using System.IO;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationPack;
using Test.TestingUtility.Shared;

namespace Test.AnimationVisualEditors
{
    // Vanilla WH3 riders, read from the packs in WH3_DATA (tests skip without it). Two ways of riding hr1_warhorse:
    //   hu1_empire_hr1_warhorse_hammer      synchronized: animroot moves with the mount's, the rider sits at saddle height
    //   hu1_wh3_kislev_hr1_warhorse_sword   attached: animroot at the origin, glued to ap_riderposition_0 of the tech skeleton
    static class VanillaRiders
    {
        public const string MountSet = "hr1_warhorse";
        public const string SynchronizedSet = "hu1_empire_hr1_warhorse_hammer";
        public const string AttachedSet = "hu1_wh3_kislev_hr1_warhorse_sword";
        public const string TechSkeleton = @"variantmeshes\wh_variantmodels\hr1\emp\emp_warbeast_horse\tech\emp_warbeast_horse_01.anim";
        public const string AttachBone = "ap_riderposition_0";
        public const string RiderMesh = @"variantmeshes\wh_variantmodels\hu1\emp\emp_arch_lector\cape\emp_arch_lector_cape_01.rigid_model_v2";
        public const string MountMesh = @"variantmeshes\wh_variantmodels\hr1\emp\emp_boris_todbringer_mount\emp_boris_todbringer_mount_01.rigid_model_v2";

        // Sets; animations and skeletons; the horse mesh and tech skeleton; a humanoid mesh
        static readonly string[] Packs = ["anim.pack", "anim2.pack", "variants.pack", "variants4.pack"];

        public static AssetEditorTestRunner Load()
        {
            var dataFolder = Environment.GetEnvironmentVariable("WH3_DATA");
            Assume.That(dataFolder != null && Directory.Exists(dataFolder), "set WH3_DATA");

            var runner = new AssetEditorTestRunner();
            ((LocalizationManager)runner.ServiceProvider.GetService(typeof(LocalizationManager))!).LoadLanguage("en");
            runner.CreateCaContainer();
            var loader = (IPackFileContainerLoader)runner.ServiceProvider.GetService(typeof(IPackFileContainerLoader))!;
            foreach (var pack in Packs)
                runner.PackFileService.AddContainer(loader.CreateFromPackFile(PackFileContainerType.Normal, Path.Combine(dataFolder!, pack), true));
            return runner;
        }

        public static IAnimationBinGenericFormat Set(IPackFileService pfs, string name) =>
            PackFileServiceUtility.GetAllAnimPacks(pfs)
                .SelectMany(x => AnimationPackSerializer.Load(x, pfs).GetGenericAnimationSets())
                .First(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        public static GameSkeleton Skeleton(IPackFileService pfs, string path) => new(AnimationFile.Create(pfs.FindFile(path)!), null);

        public static AnimationClip Clip(IPackFileService pfs, string path, GameSkeleton skeleton) => new(AnimationFile.Create(pfs.FindFile(path)!), skeleton);

        public static AnimationClip SlotClip(IPackFileService pfs, IAnimationBinGenericFormat set, string slot, GameSkeleton skeleton) =>
            Clip(pfs, set.Entries.First(x => x.SlotName == slot).AnimationFile, skeleton);

        public static Vector3 World(GameSkeleton skeleton, AnimationClip clip, int frame, int bone) =>
            AnimationSampler.Sample(frame, 0, skeleton, clip).GetSkeletonAnimatedWorld(skeleton, bone).Translation;
    }
}
