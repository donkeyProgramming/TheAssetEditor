using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.MountAnimationCreator.Services;
using CommonControls.BaseDialogs.ErrorListDialog;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3;
using static Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry;

namespace Editors.AnimationVisualEditors.MountAnimationCreator.Services
{
    class BatchProcessorService
    {
        IPackFileService _pfs;
        GameSkeleton _riderSkeleton;
        GameSkeleton _mountSkeleton;
        ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        MountAnimationGeneratorService _animationGenerator;
        BatchProcessOptions _batchProcessOptions;
        private readonly IFileSaveService _packFileSaveService;
        IAnimationBinGenericFormat _mountFragment;
        IAnimationBinGenericFormat _riderFragment;

        uint _animationOutputFormat;
        AnimationPackFile _outAnimPack;
        AnimationBinWh3 _riderOutputBin;


        string _animPackName = "test_tables.animpack";
        string _animBinName = "test_tables.bin";

        public BatchProcessorService(
            IPackFileService pfs,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            MountAnimationGeneratorService animationGenerator,
            BatchProcessOptions batchProcessOptions,
            IFileSaveService packFileSaveService,
            uint animationOutputFormat)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _animationGenerator = animationGenerator;
            _batchProcessOptions = batchProcessOptions;
            _packFileSaveService = packFileSaveService;
            _animationOutputFormat = animationOutputFormat;



            if (_batchProcessOptions != null)
            {
                _animPackName = SaveUtility.EnsureEnding(batchProcessOptions.AnimPackName, ".animpack");
                _animBinName = SaveUtility.EnsureEnding(batchProcessOptions.AnimPackName, "_tables.bin");
            }
        }

        public void Process(IAnimationBinGenericFormat mountFragment, IAnimationBinGenericFormat riderFragment)
        {
            var resultInfo = new ErrorList();
            _mountFragment = mountFragment;
            _riderFragment = riderFragment;

            _mountSkeleton = LoadSkeleton(_mountFragment.SkeletonName);
            _riderSkeleton = LoadSkeleton(_riderFragment.SkeletonName);

            CreateAnimPackFile();
            CreateFragmentAndAnimations(resultInfo);
            SaveFiles();

            ErrorListWindow.ShowDialog("Mount creation result", resultInfo, false);
        }

        void CreateFragmentAndAnimations(ErrorList resultInfo)
        {
            // Find all slots that can just be copied over
            foreach (var animationSlot in GetAnimationsThatRequireNoChanges())
                CopyAnimations(animationSlot, resultInfo);

            // Process animations that needs matching
            foreach (var animationSlot in GetMatchedAnimations())
                CreateAnimation(animationSlot.Item1, animationSlot.Item2, resultInfo);
        }

        void CreateAnimation(string riderSlot, string mountSlot, ErrorList resultInfo)
        {
            // Does the rider have this?
            var riderHasAnimation = _riderFragment.Entries.FirstOrDefault(x => x.SlotName == riderSlot) != null;
            if (riderHasAnimation)
            {
                // Create a copy of the animation fragment entry
                var riderFragment = _riderFragment.Entries.First(x => x.SlotName == riderSlot);
                var mountFragment = _mountFragment.Entries.First(x => x.SlotName == mountSlot);

                // Generate new animation
                var riderAnim = LoadAnimation(riderFragment.AnimationFile, _riderSkeleton);
                var mountAnim = LoadAnimation(mountFragment.AnimationFile, _mountSkeleton);
                var savedAnimName = SaveSingleAnim(mountAnim, riderAnim, riderFragment.AnimationFile);


                var newEntry = new AnimationBinEntry()
                {
                    AnimationId = (uint)riderFragment.SlotIndex,
                    BlendIn = riderFragment.BlendInTime,
                    SelectionWeight = riderFragment.SelectionWeight,
                    WeaponBools = riderFragment.WeaponBools,
                    Unk = false,
                    AnimationRefs = new List<AnimationRef>()
                    {
                        new AnimationRef()
                        {
                            AnimationFile = savedAnimName,
                            AnimationMetaFile = riderFragment.MetaFile,
                            AnimationSoundMetaFile = riderFragment.SoundFile
                        }
                    }
                };

                _riderOutputBin.AnimationTableEntries.Add(newEntry);
                resultInfo.Ok(mountSlot, "Matching animation found in rider (" + riderSlot + "). New animation created");
            }
            else
            {
                // Add an empty fragment entry
                _riderOutputBin.AnimationTableEntries.Add(new AnimationBinEntry()
                {
                    AnimationId = (uint)DefaultAnimationSlotTypeHelper.GetfromValue(riderSlot).Id,
                });

                resultInfo.Error(mountSlot, "Expected slot missing in  rider (" + riderSlot + "), this need to be resolved!");
            }
        }

        public string SaveSingleAnim(AnimationClip mountAnim, AnimationClip riderAnim, string originalAnimationName)
        {
            var newAnimationName = GenerateNewAnimationName(originalAnimationName, _batchProcessOptions.SavePrefix);

            var newAnimation = _animationGenerator.GenerateMountAnimation(mountAnim, riderAnim);

            // Save the new animation           
            var animFile = newAnimation.ConvertToFileFormat(_animationGenerator.GetRiderSkeleton());

            if (_animationOutputFormat != 7)
            {
                var skeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(animFile.Header.SkeletonName);
                animFile.ConvertToVersion(_animationOutputFormat, skeleton, _pfs);
            }

            var bytes = AnimationFile.ConvertToBytes(animFile);
            _packFileSaveService.Save(newAnimationName, bytes, false);

            return newAnimationName;
        }

        void CopyAnimations(string riderSlot, ErrorList resultInfo)
        {
            var fragmentEntry = _riderFragment.Entries.First(x => x.SlotName == riderSlot);
            var newEntry = new AnimationBinEntry()
            {
                AnimationId = (uint)fragmentEntry.SlotIndex,
                BlendIn = fragmentEntry.BlendInTime,
                SelectionWeight = fragmentEntry.SelectionWeight,
                WeaponBools = fragmentEntry.WeaponBools,
                Unk = false,
                AnimationRefs = new List<AnimationRef>()
                {
                    new AnimationRef()
                    {
                        AnimationFile = fragmentEntry.AnimationFile,
                        AnimationMetaFile = fragmentEntry.MetaFile,
                        AnimationSoundMetaFile = fragmentEntry.SoundFile
                    }
                }
            };

            _riderOutputBin.AnimationTableEntries.Add(newEntry);
            resultInfo.Ok(riderSlot, "Animation copied from rider");
        }

        List<string> GetAnimationsThatRequireNoChanges()
        {
            return _riderFragment.Entries
                   .Where(x => MountAnimationGeneratorService.IsCopyOnlyAnimation(x.SlotName))
                   .Select(x => x.SlotName)
                   .Distinct()
                   .ToList();
        }

        // Animations where rider and mount needs the same amount of frames
        List<(string, string)> GetMatchedAnimations()
        {
            return _mountFragment.Entries
                .Where(x => DefaultAnimationSlotTypeHelper.GetMatchingRiderAnimation(x.SlotName) != null)
                .Select(x => (DefaultAnimationSlotTypeHelper.GetMatchingRiderAnimation(x.SlotName).Value, x.SlotName))
                .Distinct()
                .ToList();
        }

        void CreateAnimPackFile()
        {
            _outAnimPack = new AnimationPackFile("Placeholder");
            _riderOutputBin = new AnimationBinWh3(@"animations/database/battle/bin/" + _animBinName)
            {
                SkeletonName = _riderFragment.SkeletonName,
                Name = Path.GetFileNameWithoutExtension(_animBinName),
                LocomotionGraph = @"animations/locomotion_graphs/entity_locomotion_graph.xml",
                UnknownValue1 = 0,
                MountBin = _mountFragment.Name
            };

            _outAnimPack.AddFile(_riderOutputBin);
        }

        void SaveFiles()
        {
            var bytes = AnimationPackSerializer.ConvertToBytes(_outAnimPack);
            _packFileSaveService.Save("animations\\database\\battle\\bin\\" + _animPackName, bytes, false);
        }

        AnimationClip LoadAnimation(string path, GameSkeleton skeleton)
        {
            var file = _pfs.FindFile(path);
            var animation = AnimationFile.Create(file);
            return new AnimationClip(animation, skeleton);
        }

        GameSkeleton LoadSkeleton(string skeletonName)
        {
            var skeletonFile = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(skeletonName);
            return new GameSkeleton(skeletonFile, null);
        }

        string GenerateNewAnimationName(string fullPath, string prefix, int numberId = 0)
        {
            var numberPostFix = "";
            if (numberId != 0)
                numberPostFix = "_" + numberId;

            var potentialName = Path.GetDirectoryName(fullPath) + "\\" + prefix + numberPostFix + Path.GetFileName(fullPath);
            var fileRef = _pfs.FindFile(potentialName);
            if (fileRef == null)
                return potentialName;
            else
                return GenerateNewAnimationName(fullPath, prefix, numberId + 1);
        }
    }
}
