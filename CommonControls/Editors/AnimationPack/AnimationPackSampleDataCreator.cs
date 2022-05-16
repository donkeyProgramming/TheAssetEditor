
using CommonControls.BaseDialogs;
using CommonControls.Common;
using CommonControls.Editors.AnimationBatchExporter;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.DB;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimationPackSampleDataCreator
    {
        public static void CreateAnimationDbWarhammer3(PackFileService pfs)
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                var fileName = SaveHelper.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/database/battle/bin/" + fileName;

                if (!SaveHelper.IsFilenameUnique(pfs, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                var animPack = new AnimationPackFile();
                SaveHelper.Save(pfs, filePath, null, AnimationPackSerializer.ConvertToBytes(animPack));
            }
        }

        public static void CreateAnimationDb3k(PackFileService pfs)
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                var fileName = SaveHelper.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/database/battle/bin/" + fileName;
                if (!SaveHelper.IsFilenameUnique(pfs, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                // Create dummy data
                var animPack = new AnimationPackFile();
                SaveHelper.Save(pfs, filePath, null, AnimationPackSerializer.ConvertToBytes(animPack));
            }
        }


        public static IAnimationPackFile CreateExampleWarhammer3AnimSet(string binName)
        {
            var filename = SaveHelper.EnsureEnding(binName, ".bin");
            var filePath = @"animations/database/battle/bin/" + filename;
            var outputFile = new AnimationBinWh3(filePath)
            {
                TableVersion = 4,
                TableSubVersion = 3,
                Name = binName,
                Unkown = "",
                MountBin = "",
                SkeletonName = "humanoid01",
                LocomotionGraph = "animations/locomotion_graphs/entity_locomotion_graph.xml",
                UnknownValue1 = 0,
            };

            outputFile.AnimationTableEntries.Add(new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry()
            {
                AnimationId = 1, // STAND
                BlendIn = 0.5f,
                SelectionWeight = 1,
                WeaponBools = 1,
                Unk = false,

                AnimationRefs = new List<FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef>()
                { 
                    new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
                    { 
                        AnimationFile = @"animations/battle/humanoid01/sword_and_shield/stand/hu1_sws_stand_01.anim",
                        AnimationMetaFile = @"",
                        AnimationSoundMetaFile = @"" ,
                    },
                }
            });

            outputFile.AnimationTableEntries.Add(new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry()
            {
                AnimationId = 453,// Attack_1
                BlendIn = 0.3f,
                SelectionWeight = 1,
                WeaponBools = 1,
                Unk = false,

                AnimationRefs = new List<FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef>()
                {
                    new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
                    {
                        AnimationFile = @"animations/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_01.anim",
                        AnimationMetaFile = @"animations/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_01.meta",
                        AnimationSoundMetaFile = @"animations/audio/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_01.{27tsfy}.snd.meta",
                    },
                    new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
                    {
                          AnimationFile = @"animations/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_02.anim",
                        AnimationMetaFile = @"animations/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_02.anm.meta",
                        AnimationSoundMetaFile = @"animations/audio/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_02.{1o2asvr}.snd.meta",
                    }
                }
            });

            return outputFile;
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
