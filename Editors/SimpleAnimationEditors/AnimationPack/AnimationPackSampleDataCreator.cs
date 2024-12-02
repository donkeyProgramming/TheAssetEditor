using System.Windows;
using CommonControls.BaseDialogs;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimationPackSampleDataCreator
    {
        public static PackFile? CreateAnimationDbWarhammer3(IFileSaveService saveHelper, IPackFileService pfs)
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
                return CreateAnimationDbWarhammer3(saveHelper, pfs, window.TextValue);
            return null;
        }

        public static string GenerateWh3AnimPackName(string name)
        {
            var fileName = SaveUtility.EnsureEnding(name, ".animpack");
            var filePath = @"animations/database/battle/bin/" + fileName;
            return filePath;
        }

        public static PackFile? CreateAnimationDbWarhammer3(IFileSaveService saveHelper, IPackFileService pfs, string name)
        {
            var filePath = GenerateWh3AnimPackName(name);

            if (!SaveUtility.IsFilenameUnique(pfs, filePath))
            {
                MessageBox.Show("Filename is not unique");
                return null;
            }

            var animPack = new AnimationPackFile("Placeholder");
            return saveHelper.Save(filePath, AnimationPackSerializer.ConvertToBytes(animPack), false);
        }

        public static void CreateAnimationDb3k(IPackFileService pfs, IFileSaveService saveHelper)
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                var fileName = SaveUtility.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/database/battle/bin/" + fileName;
                if (!SaveUtility.IsFilenameUnique(pfs, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                // Create dummy data
                var animPack = new AnimationPackFile("Placeholder");
                saveHelper.Save(filePath, AnimationPackSerializer.ConvertToBytes(animPack), false);
            }
        }


        public static IAnimationPackFile CreateExampleWarhammer3AnimSet(string binName)
        {
            var filename = SaveUtility.EnsureEnding(binName, ".bin");
            var filePath = @"animations/database/battle/bin/" + filename;
            var outputFile = new AnimationBinWh3(filePath)
            {
                TableVersion = 4,
                TableSubVersion = 3,
                Name = binName,
                Unknown = "",
                MountBin = "",
                SkeletonName = "humanoid01",
                LocomotionGraph = "animations/locomotion_graphs/entity_locomotion_graph.xml",
                UnknownValue1 = 0,
            };

            outputFile.AnimationTableEntries.Add(new Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry()
            {
                AnimationId = 1, // STAND
                BlendIn = 0.5f,
                SelectionWeight = 1,
                WeaponBools = 1,
                Unk = false,

                AnimationRefs = new List<Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef>()
                {
                    new Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
                    {
                        AnimationFile = @"animations/battle/humanoid01/sword_and_shield/stand/hu1_sws_stand_01.anim",
                        AnimationMetaFile = @"",
                        AnimationSoundMetaFile = @"" ,
                    },
                }
            });

            outputFile.AnimationTableEntries.Add(new Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry()
            {
                AnimationId = 453,// Attack_1
                BlendIn = 0.3f,
                SelectionWeight = 1,
                WeaponBools = 1,
                Unk = false,

                AnimationRefs = new List<Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef>()
                {
                    new Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
                    {
                        AnimationFile = @"animations/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_01.anim",
                        AnimationMetaFile = @"animations/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_01.meta",
                        AnimationSoundMetaFile = @"animations/audio/battle/humanoid01/sword_and_shield/attacks/hu1_sws_attack_01.{27tsfy}.snd.meta",
                    },
                    new Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
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
