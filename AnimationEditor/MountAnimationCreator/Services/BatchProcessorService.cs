using CommonControls.Common;
using CommonControls.ErrorListDialog;
using CommonControls.Services;
using Filetypes.Animation;
using Filetypes.RigidModel;
using FileTypes.AnimationPack;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using View3D.Animation;

namespace AnimationEditor.MountAnimationCreator.Services
{
    class BatchProcessorService
    {
        PackFileService _pfs;
        MountAnimationGeneratorService _animationGenerator;
        BatchProcessOptions _batchProcessOptions;
        AnimationFragment _mountFragment;
        AnimationFragment _riderFragment;
       
        AnimationPackFile _outAnimPack;
        AnimationFragment _riderOutputFragment;

        string _animationPrefix = "new_";
        string _animPackName = "test_tables.animpack";
        string _animBinName = "test_tables.bin";
        string _fragmentName = "hu1_test_hr1_hammer";

        public BatchProcessorService(PackFileService pfs, MountAnimationGeneratorService animationGenerator, BatchProcessOptions batchProcessOptions)
        {
            _animPackName = SaveHelper.EnsureEnding(batchProcessOptions.AnimPackName, ".animpack");
            _animBinName = SaveHelper.EnsureEnding(batchProcessOptions.AnimPackName, "_tables.bin");
            _fragmentName = SaveHelper.EnsureEnding(batchProcessOptions.FragmentName, ".frg");

            _pfs = pfs;
            _animationGenerator = animationGenerator;
            _batchProcessOptions = batchProcessOptions;
        }

        public void Process(AnimationFragment mountFragment, AnimationFragment riderFragment)
        {
            var resultInfo = new ErrorListViewModel.ErrorList();
            _mountFragment = mountFragment;
            _riderFragment = riderFragment;

            CreateFiles();
            CreateFragmentAndAnimations(resultInfo);
            SaveFiles();

            ErrorListWindow.ShowDialog("Mount creation result", resultInfo, false);
        }

        void CreateFragmentAndAnimations(ErrorListViewModel.ErrorList resultInfo)
        {
            // Find all slots that can just be copied over
            foreach (var animationSlot in GetAnimationsThatRequireNoChanges())
                CopyAnimations(animationSlot, resultInfo);

            // Process animations that needs matching
            foreach (var animationSlot in GetMatchedAnimations())
                CreateAnimation(animationSlot.Item1, animationSlot.Item2, resultInfo);
        }

        void CreateAnimation( string riderSlot, string mountSlot, ErrorListViewModel.ErrorList resultInfo)
        {
            // Does the rider have this?
            var riderHasAnimation = _riderFragment.Fragments.FirstOrDefault(x => x.Slot.Value == riderSlot) != null;
            if (riderHasAnimation)
            {
                // Create a copy of the animation fragment entry
                var riderFragment = _riderFragment.Fragments.First(x => x.Slot.Value == riderSlot);
                var newRiderFragment = riderFragment.Clone();
                var newAnimationName = GenerateNewAnimationName(newRiderFragment.AnimationFile, _animationPrefix);
                newRiderFragment.AnimationFile = newAnimationName;
                _riderOutputFragment.Fragments.Add(newRiderFragment);

                var mountFragment = _mountFragment.Fragments.First(x => x.Slot.Value == mountSlot);

                // Generate new animation
                var riderAnim = LoadAnimation(riderFragment.AnimationFile);
                var mountAnim = LoadAnimation(mountFragment.AnimationFile);
    
                
                var newAnimation = _animationGenerator.GenerateMountAnimation(mountAnim, riderAnim);

                // Save the new animation           
                var animFile = newAnimation.ConvertToFileFormat(_animationGenerator.GetRiderSkeleton());
                var bytes = AnimationFile.GetBytes(animFile);
                SaveHelper.Save(_pfs, newAnimationName, null, bytes);

                resultInfo.Ok(mountSlot, "Matching animation found in rider ("+ riderSlot + "). New animation created");
            }
            else
            {
                // Add an empty fragment entry
                _riderOutputFragment.Fragments.Add(new AnimationFragmentEntry()
                {
                    Slot = AnimationSlotTypeHelper.GetfromValue(riderSlot),
                    Skeleton = _riderFragment.Skeletons.Values.First()
                });

                resultInfo.Error(mountSlot, "Expected slot missing in  rider (" + riderSlot + "), this need to be resolved!");
            }
        }

        void CopyAnimations(string riderSlot, ErrorListViewModel.ErrorList resultInfo)
        {
            var fragmentEntry = _riderFragment.Fragments.First(x => x.Slot.Value == riderSlot);
            _riderOutputFragment.Fragments.Add(fragmentEntry.Clone());

            resultInfo.Ok(riderSlot, "Animation copied from rider");
        }

        List<string> GetAnimationsThatRequireNoChanges()
        {
            return _riderFragment.Fragments
                   .Where(x => MountAnimationGeneratorService.IsCopyOnlyAnimation(x.Slot.Value))
                   .Select(x => x.Slot.Value)
                   .Distinct()
                   .ToList();
        }

        // Animations where rider and mount needs the same amount of frames
        List<(string, string)> GetMatchedAnimations()
        {
            return _mountFragment.Fragments
                .Where(x => AnimationSlotTypeHelper.GetMatchingRiderAnimation(x.Slot.Value) != null)
                .Select(x => (AnimationSlotTypeHelper.GetMatchingRiderAnimation(x.Slot.Value).Value, x.Slot.Value))
                .Distinct()
                .ToList();
        }


        void CreateFiles()
        {
            //AnimationPackLoader
            _outAnimPack = new AnimationPackFile(_animBinName);
            _outAnimPack.AnimationBin = new AnimationBin("animations/animation_tables/" + _animBinName);
            var tableEntry = new AnimationBinEntry(_fragmentName, _riderFragment.Skeletons.Values.First(), "Bin entry using skeleton - " + _mountFragment.Skeletons.Values.First() + " goes here");
            tableEntry.FragmentReferences.Add(new AnimationBinEntry.FragmentReference() { Name = _fragmentName });

            _outAnimPack.AnimationBin.AnimationTableEntries.Add(tableEntry);

            _riderOutputFragment = new AnimationFragment("animations/animation_tables/" + _fragmentName + ".frg");
            _riderOutputFragment.Skeletons = new AnimationFragment.StringArrayTable(_riderFragment.Skeletons.Values.First(), _riderFragment.Skeletons.Values.First());

            _outAnimPack.Fragments.Add(_riderOutputFragment);
        }

        void SaveFiles()
        {
            var bytes = _outAnimPack.ToByteArray();
            SaveHelper.Save(_pfs, "animations\\animation_tables\\" + _animPackName, null, bytes);
        }

        AnimationClip LoadAnimation(string path)
        {
            var file = _pfs.FindFile(path);
            var animation = AnimationFile.Create(file);
            return new AnimationClip(animation);
        }

        string GenerateNewAnimationName(string fullPath, string prefix, int numberId = 0)
        {
            string numberPostFix = "";
            if (numberId != 0)
                numberPostFix = "_" + numberId;

            var potentialName =  Path.GetDirectoryName(fullPath) + "\\" + prefix + numberPostFix + Path.GetFileName(fullPath);
            var fileRef = _pfs.FindFile(potentialName);
            if (fileRef == null)
                return potentialName;
            else
                return GenerateNewAnimationName(fullPath, prefix, numberId+1);
        }
    }
}