using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3;
using CommonControls.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Wh3AnimPackCreator
{

    /* public interface Warhammer3BinCreator
     {

         AnimationBinWh3 CreateBinFromAnimationSet(string animationSet, string _filePrefix);
         IAnimationBinGenericFormat GetAnimationSet(string animationSet);
     }

     public class Warhammer3BinCreator_FromTroy : Warhammer3BinCreator
     {
         AnimationPackFile _animPackFile;
         AnimationBin _animationBin;

         public Warhammer3BinCreator_FromTroy(PackFileService pfs)
         {
             var gameAnimPackFile = pfs.FindFile(@"animations\animation_tables\animation_tables.animpack");
             _animPackFile = AnimationPackSerializer.Load(gameAnimPackFile, pfs, GameTypeEnum.Troy);
             _animationBin = _animPackFile.Files.First(x => x.FileName == @"animations/animation_tables/animation_tables.bin") as AnimationBin;
         }

         public IAnimationBinGenericFormat GetAnimationSet(string animationSet)
         {
             var fragment = _animPackFile.Files.First(x => x.FileName == animationSet) as AnimationFragmentFile;
             var groupedSlots = fragment.Fragments.GroupBy(x => x.Slot.Value).ToList();
         }

         public AnimationBinWh3 CreateBinFromAnimationSet(string animationSet, string _filePrefix)
         {
             var binInstance = _animationBin.AnimationTableEntries.First(X => X.Name.ToLower() == Path.GetFileNameWithoutExtension(animationSet).ToLower());

             var name = @"animations/database/battle/bin/" + _filePrefix + Path.GetFileNameWithoutExtension(animationSet) + ".bin";
             return new AnimationBinWh3(name)
             {
                 Name = _filePrefix + Path.GetFileNameWithoutExtension(animationSet),
                 SkeletonName = _filePrefix + binInstance.SkeletonName,
                 LocomotionGraph = @"animations\locomotion_graphs\entity_locomotion_graph.xml"
             };
         }

         public void AddAnimationToBin(AnimationBinWh3)
     }*/

    public class AnimationTransferHelper
    {
        List<string> _animFilesToCopy = new List<string>();
        List<string> _metaFilesToCopy = new List<string>();
        List<string> _soundMetaFilesToCopy = new List<string>();

        LogService _logService;

        PackFileService _inputPfs;
        PackFileService _outputPfs;
        AnimationPackFile _outputAnimPack;

        readonly string _filePrefix = "";
        ResourecSwapRules _resourecSwapRules;

        public AnimationTransferHelper(LogService logService, PackFileService inputPfs, ResourecSwapRules resourecSwapRules, PackFileService outputPfs, AnimationPackFile outputAnimPack)
        {
            _logService = logService;
            _resourecSwapRules = resourecSwapRules;
            _inputPfs = inputPfs;
            _outputPfs = outputPfs;
            _outputAnimPack = outputAnimPack;
        }

        public void Convert(string animationSetName)
        {
            var animContainer = GetAnimationContainersTroy(_inputPfs, animationSetName);
            var wh3Bin = CreateAnimationBinFromTroy(animContainer.AnimBin, animationSetName);
            _outputAnimPack.AddFile(wh3Bin);

            var groupedSlots = animContainer.FragmentFile.Fragments.GroupBy(x => x.Slot.Value).ToList();
            foreach (var groupedSlot in groupedSlots)
            {
                // If no mapping - skip with error
                bool doesInputAnimHaveMatchingOutputSlot = DoesInputAnimHaveMatchingOutputSlot(groupedSlot.Key, out string wh3SlotName, animationSetName);
                if (doesInputAnimHaveMatchingOutputSlot == false)
                    continue;   // Copy animations, but dont add to bin!

                // If no animations in group - Skip, with info
                // TODO: If none of the files found - skip with info
                //bool doesAnimationGroupHaveAnimations = DoesAnimationGroupHaveAnimations(groupedSlot.Key, groupedSlot.AsEnumerable(), animationSetName);
                //if (doesAnimationGroupHaveAnimations == false)
                //    continue;

                foreach (var animationInstance in groupedSlot)
                {
                    AddAnimationFiles(animationInstance.AnimationFile, animationInstance.MetaDataFile, animationInstance.SoundMetaDataFile);
                    AddAnimationToToBin(wh3Bin, animationInstance.AnimationFile, animationInstance.MetaDataFile, animationInstance.BlendInTime, animationInstance.SelectionWeight, animationInstance.WeaponBone, wh3SlotName);
                }
            }

            CopyFiles(wh3Bin.SkeletonName, animationSetName);
        }

        public void ConvertFrom3k(string animationSetName)
        {
            var gameAnimPackFile = _inputPfs.FindFile(@"animations\animation_tables\animation_tables.animpack");
            var gameAnimPack = AnimationPackSerializer.Load(gameAnimPackFile, _inputPfs, GameTypeEnum.ThreeKingdoms);
            var fragment = gameAnimPack.Files.First(x => x.FileName == animationSetName) as AnimationBinWh3;

            var wh3Bin = CreateAnimationBinFromThreeKingdoms(fragment, animationSetName);
            _outputAnimPack.AddFile(wh3Bin);

            foreach (var animationTableEntry in fragment.AnimationTableEntries)
            {
                // If no mapping - skip with error
                var currentAnimationlotName = "animationTableEntry.AnimationId";
                bool doesInputAnimHaveMatchingOutputSlot = DoesInputAnimHaveMatchingOutputSlot(currentAnimationlotName, out string wh3SlotName, animationSetName);
                if (doesInputAnimHaveMatchingOutputSlot == false)
                    continue;   // Copy animations, but dont add to bin!

                // If no animations in group - Skip, with info
                // TODO: If none of the files found - skip with info
                // bool doesAnimationGroupHaveAnimations = DoesAnimationGroupHaveAnimations(animationTableEntry.Key, animationTableEntry.AsEnumerable(), animationSetName);
                // if (doesAnimationGroupHaveAnimations == false)
                //     continue;

                foreach (var animationInstance in animationTableEntry.AnimationRefs)
                {
                    AddAnimationFiles(animationInstance.AnimationFile, animationInstance.AnimationMetaFile, animationInstance.AnimationSoundMetaFile);
                    AddAnimationToToBin(wh3Bin, animationInstance.AnimationFile, animationInstance.AnimationMetaFile, animationTableEntry.BlendIn, animationTableEntry.SelectionWeight, animationTableEntry.WeaponBools, wh3SlotName); ;
                }
            }

            CopyFiles(wh3Bin.SkeletonName, animationSetName);
        }


        void CopyFiles(string skeletonName, string animationSetName)
        {
            CopySkeleton(skeletonName);
            CopyAnimationFiles(_animFilesToCopy, animationSetName);
            CopyMetaFiles(_metaFilesToCopy, animationSetName);
            //CopySoundMetaFiles(soundMetaFilesToCopy, animationSetName);
        }


        void AddAnimationFiles(string animFile, string metaFile, string soundFile)
        {
            if (string.IsNullOrWhiteSpace(animFile) == false)
                _animFilesToCopy.Add(animFile);

            if (string.IsNullOrWhiteSpace(metaFile) == false)
                _metaFilesToCopy.Add(metaFile);

            if (string.IsNullOrWhiteSpace(soundFile) == false)
                _soundMetaFilesToCopy.Add(soundFile);
        }

        private bool DoesInputAnimHaveMatchingOutputSlot(string animationSlotName, out string outpSlotName, string currentBinName)
        {
            outpSlotName = _resourecSwapRules.GetMatchingAnimationSlotName(currentBinName, animationSlotName);
            if (outpSlotName == null)
                return false;

            return true;
        }

        private bool DoesAnimationGroupHaveAnimations(string animationSlotName, IEnumerable<AnimationSetEntry> animationEntryList, string currentBinName)
        {
            if (animationEntryList.Count() == 0)
            {
                _logService.AddLogItem(LogService.LogType.Info, currentBinName, $"No animations in slot {animationSlotName}", "No_ANIMATIONS_IN_SLOT");
                return false;
            }

            // Check if the files are actually there?
            return true;
        }

        private void CopySkeleton(string skeletonName)
        {
            var skeletonFile = _inputPfs.FindFile($"animations\\skeletons\\{skeletonName}.anim");
            var invMatrixFile = _inputPfs.FindFile($"animations\\skeletons\\{skeletonName}.bone_inv_trans_mats");

            var finalSkeletonName = UpdateFileName($"animations\\skeletons\\{skeletonName}.anim");
            var finalInvMatrixFile = UpdateFileName($"animations\\skeletons\\{skeletonName}.bone_inv_trans_mats");

            if (_outputPfs.FindFile(finalSkeletonName) == null)
                SaveHelper.Save(_outputPfs, finalSkeletonName, null, skeletonFile.DataSource.ReadData());

            if (_outputPfs.FindFile(finalInvMatrixFile) == null)
                SaveHelper.Save(_outputPfs, finalInvMatrixFile, null, invMatrixFile.DataSource.ReadData());
        }

        void CopyAnimationFiles(List<string> animationFiles, string currentBinName)
        {
            var distinctAnimFiles = animationFiles.Distinct();

            _logService.AddLogItem(LogService.LogType.Info, currentBinName, $"Animation files to copy = {distinctAnimFiles.Count()}");
            foreach (var animationFile in distinctAnimFiles)
            {
                var finalName = UpdateFileName(animationFile);
                if (_outputPfs.FindFile(finalName) != null)
                    continue;

                var file = _inputPfs.FindFile(animationFile);
                if (file == null)
                {
                    _logService.AddLogItem(LogService.LogType.Warning, currentBinName, $"File not found {animationFile}", "MISSING_ANIMATION_FILE");
                    continue;
                }

                SaveHelper.Save(_outputPfs, finalName, null, file.DataSource.ReadData());
            }
        }

        void CopyMetaFiles(List<string> metaFiles, string currentBinName)
        {
            var distinctMetaFiles = metaFiles.Distinct();

            _logService.AddLogItem(LogService.LogType.Info, currentBinName, $"Meta files to copy = {distinctMetaFiles.Count()}");
            foreach (var metaFile in distinctMetaFiles)
            {
                var finalName = UpdateFileName(metaFile);
                if (_outputPfs.FindFile(finalName) != null)
                    continue;

                var file = _inputPfs.FindFile(metaFile);
                if (file == null)
                {
                    _logService.AddLogItem(LogService.LogType.Warning, currentBinName, $"File not found {metaFile}", "MISSING_META_FILE");
                    continue;
                }

                byte[] metaBytes = _resourecSwapRules.ConvertMetaFile(currentBinName, file);
                SaveHelper.Save(_outputPfs, finalName, null, metaBytes);
            }
        }

        void CopySoundMetaFiles(List<string> soundMetaFiles, string currentBinName)
        {
            var distinctMetaFiles = soundMetaFiles.Distinct();

            _logService.AddLogItem(LogService.LogType.Info, currentBinName, $"SoundMeta files to copy = {distinctMetaFiles.Count()}");
            foreach (var metaFile in distinctMetaFiles)
            {

                var finalName = UpdateFileName(metaFile);
                if (_outputPfs.FindFile(finalName) != null)
                    continue;

                var file = _inputPfs.FindFile(metaFile);
                if (file == null)
                {
                    _logService.AddLogItem(LogService.LogType.Warning, currentBinName, $"File not found {metaFile}", "MISSING_META_FILE");
                    continue;
                }

                byte[] metaBytes = _resourecSwapRules.ConvertMetaFile(currentBinName, file);
                //SaveHelper.Save(_outputPfs, finalName, null, metaBytes);
            }
        }

        AnimationBinWh3 CreateAnimationBinFromTroy(AnimationBin bin, string fragment)
        {
            var binInstance = bin.AnimationTableEntries.First(X => X.Name.ToLower() == Path.GetFileNameWithoutExtension(fragment).ToLower());

            var name = @"animations/database/battle/bin/" + _filePrefix + Path.GetFileNameWithoutExtension(fragment) + ".bin";
            return new AnimationBinWh3(name)
            {
                Name = _filePrefix + Path.GetFileNameWithoutExtension(fragment),
                SkeletonName = _filePrefix + binInstance.SkeletonName,
                LocomotionGraph = @"animations\locomotion_graphs\entity_locomotion_graph.xml"
            };
        }

        private AnimationBinWh3 CreateAnimationBinFromThreeKingdoms(AnimationBinWh3 fragment, string animationSetName)
        {
            var name = @"animations/database/battle/bin/" + _filePrefix + Path.GetFileNameWithoutExtension(animationSetName) + ".bin";
            return new AnimationBinWh3(name)
            {
                Name = _filePrefix + Path.GetFileNameWithoutExtension(animationSetName),
                SkeletonName = _filePrefix + fragment.SkeletonName,
                LocomotionGraph = @"animations\locomotion_graphs\entity_locomotion_graph.xml"
            };
        }

        void AddAnimationToToBin(AnimationBinWh3 binWh3, string animationFile, string metaFile, float blendInTime, float selectionWeight, int weaponBools, string outputSlotName)
        {
            var outputSlot = AnimationSlotTypeHelperWh3.GetfromValue(outputSlotName);

            var animationEntry = binWh3.AnimationTableEntries.FirstOrDefault(x => x.AnimationId == outputSlot.Id);
            if (animationEntry == null)
            {
                animationEntry = new CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry()
                {
                    AnimationId = (uint)outputSlot.Id,
                    BlendIn = blendInTime,
                    SelectionWeight = selectionWeight,
                    WeaponBools = weaponBools
                };

                binWh3.AnimationTableEntries.Add(animationEntry);
            }

            animationEntry.AnimationRefs.Add(new CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
            {
                AnimationFile = UpdateFileName(animationFile),
                AnimationMetaFile = UpdateFileName(metaFile),
                AnimationSoundMetaFile = "",
            });
        }


        (AnimationBin AnimBin, AnimationFragmentFile FragmentFile) GetAnimationContainersTroy(PackFileService pfs, string fragmentName)
        {
            var gameAnimPackFile = pfs.FindFile(@"animations\animation_tables\animation_tables.animpack");
            var gameAnimPack = AnimationPackSerializer.Load(gameAnimPackFile, pfs, GameTypeEnum.Troy);
            var animBin = gameAnimPack.Files.First(x => x.FileName == @"animations/animation_tables/animation_tables.bin") as AnimationBin;
            var fragment = gameAnimPack.Files.First(x => x.FileName == fragmentName) as AnimationFragmentFile;

            return (animBin, fragment);
        }

        string UpdateFileName(string inputFileName)
        {
            if (string.IsNullOrWhiteSpace(inputFileName))
                return string.Empty;
            var fileName = Path.GetFileName(inputFileName);
            var path = Path.GetDirectoryName(inputFileName);
            var newName = $"{path}\\{_filePrefix}{fileName}";
            return newName;
        }
    }
}
