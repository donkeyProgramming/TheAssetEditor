using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.DB;
using System.Collections.Generic;
using System.Linq;

namespace CommonControls.FileTypes.AnimationPack
{
    public class AnimationPackFile
    {
        public string FileName { get; set; }
        List<IAnimationPackFile> _files { get; set; } = new List<IAnimationPackFile>();

        public IEnumerable<IAnimationPackFile> Files { get => _files; }

        public void AddFile(IAnimationPackFile file)
        {
            file.Parent = this;
            _files.Add(file);
        }

        public List<AnimationFragmentFile> GetAnimationSets(string skeletonName = null)
        {
            var sets = _files.Where(x => x is AnimationFragmentFile).Cast<AnimationFragmentFile>();
            if(skeletonName != null)
                sets = sets.Where(x => x.Skeletons.Values.Contains(skeletonName));

            return sets.ToList();
        }

        public static AnimationBin CreateExampleWarhammerBin(string binPath)
        {
            var animDb = new AnimationBin(binPath);

            animDb.AnimationTableEntries.Add(
                new AnimationBinEntry("ExampleDbRef", "ExampleSkeleton")
                {
                    Unknown = 1,
                    FragmentReferences = new List<AnimationBinEntry.FragmentReference>()
                    {
                        new AnimationBinEntry.FragmentReference() { Name = "FragNameRef0"},
                        new AnimationBinEntry.FragmentReference() { Name = "FragNameRef1"}
                    }
                });

            return animDb;
        }

        public static AnimationFragmentFile CreateExampleWarhammerAnimSet(string fragmentName)
        {
            var filename = SaveHelper.EnsureEnding(fragmentName, ".frg");
            var filePath = @"animations/animation_tables/" + filename;
            
            var animSet = new AnimationFragmentFile(filePath, null);
            
            animSet.Skeletons = new StringArrayTable("ExampleSkeleton", "ExampleSkeleton");
            animSet.Fragments.Add(new AnimationSetEntry()
            {
                AnimationFile = @"animations/battle/ExampleSkeleton/exampleanim.anim",
                MetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim.anm.meta",
                Skeleton = @"ExampleSkeleton",
                Slot = AnimationSlotTypeHelper.GetfromValue("MISSING_ANIM"),
                SoundMetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim.snd.meta"
            });
            
            animSet.Fragments.Add(new AnimationSetEntry()
            {
                AnimationFile = @"animations/battle/ExampleSkeleton/exampleanim2.anim",
                MetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim2.anm.meta",
                Skeleton = @"ExampleSkeleton",
                Slot = AnimationSlotTypeHelper.GetfromValue("STAND"),
                SoundMetaDataFile = @"animations/battle/ExampleSkeleton/exampleanim2.snd.meta"
            });

            return animSet;
        }

        public static AnimationSet3kFile CreateExample3kAnimSet(string fragmentName)
        {
            var filename = SaveHelper.EnsureEnding(fragmentName, ".bin");
            var filePath = @"animations/database/battle/bin/" + filename;
            
            var animSet = new AnimationSet3kFile(filePath, null);
            animSet.MountSkeleton = "horse_1h095";
            animSet.FragmentName = "infantry_1h095_hero";
            animSet.SkeletonName = "male01";
            animSet.IsSimpleFlight = false;
            animSet.MountFragment = "cavalry_1h095_hero";

            var row0 = new AnimationSet3kFile.AnimationSetEntry()
            {
                Slot = 0,
                BlendWeight = 0.15f,
                SelectionWeight = 1,
                Flag = false,
                WeaponBone = 47,
                Animations = new List<AnimationSet3kFile.AnimationSetEntry.AnimationEntry>()
                { 
                    new AnimationSet3kFile.AnimationSetEntry.AnimationEntry()
                    { 
                        AnimationFile = @"animations/skeletons/male01.anim",
                        MetaFile = @"animations/battle/persistent/cav_1h095_mandatory_persistent_metadata_alive_0.anm.meta",
                        SoundMeta = @"animations/audio/battle/persistent/cav_1h095_mandatory_persistent_metadata_alive_0.{e20c2u}.snd.meta",
                    }
                }
            };

            var row1 = new AnimationSet3kFile.AnimationSetEntry()
            {
                Slot = 3,
                BlendWeight = 0.15f,
                SelectionWeight = 1,
                Flag = false,
                WeaponBone = 47,
                Animations = new List<AnimationSet3kFile.AnimationSetEntry.AnimationEntry>()
                {
                    new AnimationSet3kFile.AnimationSetEntry.AnimationEntry()
                    {
                        AnimationFile = @"animations/skeletons/male01.anim",
                        MetaFile = @"animations/battle/persistent/cav_1h095_mandatory_persistent_metadata_alive_0.anm.meta",
                        SoundMeta = @"animations/audio/battle/persistent/cav_1h095_mandatory_persistent_metadata_alive_0.{e20c2u}.snd.meta",
                    }
                }
            };

            animSet.Entries.Add(row0);
            animSet.Entries.Add(row1);

            return animSet;
        }
    }
}
