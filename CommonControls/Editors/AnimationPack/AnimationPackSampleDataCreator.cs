
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
        public static PackFile CreateAnimationDbWarhammer3(PackFileService pfs)
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
                return CreateAnimationDbWarhammer3(pfs, window.TextValue);
            return null;
        }

        public static PackFile CreateAnimationDbWarhammer3(PackFileService pfs, string name)
        {

            var fileName = SaveHelper.EnsureEnding(name, ".animpack");
            var filePath = @"animations/database/battle/bin/" + fileName;

            if (!SaveHelper.IsFilenameUnique(pfs, filePath))
            {
                MessageBox.Show("Filename is not unique");
                return null;
            }

            var animPack = new AnimationPackFile();
            return SaveHelper.Save(pfs, filePath, null, AnimationPackSerializer.ConvertToBytes(animPack));
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
    }
}
