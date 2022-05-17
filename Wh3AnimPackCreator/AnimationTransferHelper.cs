using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Wh3AnimPackCreator
{
    public class AnimationTransferHelper
    {
        PackFileService _inputPfs;
        PackFileService _outputPfs;
        AnimationPackFile _outputAnimPack;

        readonly string _filePrefix = "tr_";
        ResourecSwapRules _resourecSwapRules;

        public AnimationTransferHelper(PackFileService inputPfs, ResourecSwapRules resourecSwapRules, PackFileService outputPfs, AnimationPackFile outputAnimPack)
        {
            _resourecSwapRules = resourecSwapRules;
            _inputPfs = inputPfs;
            _outputPfs = outputPfs;
            _outputAnimPack = outputAnimPack;
        }

        public void Run(string fragmentName)
        {
            Console.WriteLine($"\t Processing {fragmentName}");

            var animContainer = GetAnimationContainers(_inputPfs, fragmentName);
            var fragment = animContainer.FragmentFile;
                
            var animFilesToCopy = new List<string>();
            var metaFilesToCopy = new List<string>();

            var groupedSlots = fragment.Fragments.GroupBy(x => x.Slot.Value).ToList();

            AnimationBinWh3 wh3Bin = CreateAnimationBin(animContainer.AnimBin, fragmentName);
            _outputAnimPack.AddFile(wh3Bin);
            foreach (var groupedSlot in groupedSlots)
            {
                Console.WriteLine($"\t\t Processing Slot: {groupedSlot.Key}[{groupedSlot.Count()}]");

                var wh3SlotName = _resourecSwapRules.GetMatchingAnimationSlotName(groupedSlot.Key);
                if (wh3SlotName == null)
                    continue;

                foreach (var animationInstance in groupedSlot)
                {
                    if (string.IsNullOrWhiteSpace(animationInstance.AnimationFile) == false)
                        animFilesToCopy.Add(animationInstance.AnimationFile);

                    if (string.IsNullOrWhiteSpace(animationInstance.MetaDataFile) == false)
                        metaFilesToCopy.Add(animationInstance.MetaDataFile);

                    AddAnimationToToBin(wh3Bin, animationInstance, wh3SlotName);
                }
            }

            CopyAnimationFiles(animFilesToCopy);
            CopyMetaFiles(metaFilesToCopy);
        }

        void CopyAnimationFiles(List<string> animationFiles)
        {
            var distinctAnimFiles = animationFiles.Distinct();
            Console.WriteLine();
            Console.WriteLine($"\t Copying Animation Files {distinctAnimFiles.Count()}:");
            distinctAnimFiles.ForEach(i => Console.WriteLine($"\t\t {i}"));
        }

        void CopyMetaFiles(List<string> metaFiles)
        {
            var distinctMetaFiles = metaFiles.Distinct();
            Console.WriteLine();
            Console.WriteLine($"\t Copying Meta Files {distinctMetaFiles.Count()}:");
            distinctMetaFiles.ForEach(i => Console.WriteLine($"\t\t {i}"));
        }

        AnimationBinWh3 CreateAnimationBin(AnimationBin bin, string fragment)
        {
            var binInstance = bin.AnimationTableEntries.First(X => X.Name.ToLower() == Path.GetFileNameWithoutExtension(fragment).ToLower());

            var name = @"animations/database/battle/bin/" + _filePrefix + Path.GetFileNameWithoutExtension(fragment) + ".bin";
            return new AnimationBinWh3(name)
            { 
                Name = _filePrefix + Path.GetFileNameWithoutExtension(fragment) + ".bin",
                SkeletonName = binInstance.SkeletonName,
                LocomotionGraph = "locomoationGraph.."
            };
        }

        void AddAnimationToToBin(AnimationBinWh3 binWh3, AnimationSetEntry animation, string outputSlotName)
        {
            var outputSlot = AnimationSlotTypeHelperWh3.GetfromValue(outputSlotName);

            var animationEntry = binWh3.AnimationTableEntries.FirstOrDefault(x => x.AnimationId == outputSlot.Id);
            if (animationEntry == null)
            {
                animationEntry = new CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry()
                {
                    AnimationId = (uint)outputSlot.Id,
                    BlendIn = animation.BlendInTime,
                    SelectionWeight = animation.SelectionWeight,
                    WeaponBools = animation.WeaponBone
                };

                binWh3.AnimationTableEntries.Add(animationEntry);
            }
           
            animationEntry.AnimationRefs.Add(new CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
            {
                AnimationFile = UpdateFileName(animation.AnimationFile),
                AnimationMetaFile = UpdateFileName(animation.MetaDataFile),
                AnimationSoundMetaFile = "",
            });
        }


        (AnimationBin AnimBin, AnimationFragmentFile FragmentFile) GetAnimationContainers(PackFileService pfs, string fragmentName)
        {
            var gameAnimPackFile = pfs.FindFile(@"animations\animation_tables\animation_tables.animpack");
            var gameAnimPack = AnimationPackSerializer.Load(gameAnimPackFile, pfs, new BaseAnimationSlotHelper(GameTypeEnum.Troy));
            var animBin = gameAnimPack.Files.First(x => x.FileName == @"animations/animation_tables/animation_tables.bin") as AnimationBin;
            var fragment = gameAnimPack.Files.First(x => x.FileName == fragmentName) as AnimationFragmentFile;

            return (animBin, fragment);
        }

        string UpdateFileName(string inputFileName)
        {
            var fileName = Path.GetFileName(inputFileName);
            var path = Path.GetDirectoryName(inputFileName);
            var newName = $"{path}\\{_filePrefix}{fileName}";
            return newName;


        }
    }
}
