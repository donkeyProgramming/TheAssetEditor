using CommonControls.Common;
using CommonControls.ErrorListDialog;
using CommonControls.Services;
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
        List<AnimationFile> _animationFiles = new List<AnimationFile>();

        string _animationPrefix = "new_";
        string _animPackName = "test_tables.animpack";
        string _animBinName = "test_tables.bin";
        string _fragmentName = "hu1_test_hr1_hammer";

        public BatchProcessorService(PackFileService pfs, MountAnimationGeneratorService animationGenerator, BatchProcessOptions batchProcessOptions)
        {
            _pfs = pfs;
            _animationGenerator = animationGenerator;
            _batchProcessOptions = batchProcessOptions;
        }

        public void Process(AnimationFragment mountFragment, AnimationFragment riderFragment)
        {
            var resultInfo = new ErrorListViewModel.ErrorList();
            _mountFragment = mountFragment;
            _riderFragment = riderFragment;

            CreateFiles(_batchProcessOptions);
            CreateFragmentAndAnimations(resultInfo);
            SaveFiles();

            ErrorListWindow.ShowDialog("Mount creation result", resultInfo);
        }

        void CreateFragmentAndAnimations(ErrorListViewModel.ErrorList resultInfo)
        {
            CopyAnimations("MISSING_ANIM", resultInfo);

            foreach (var animationSlot in GetMetaDataSlots())
                CopyAnimations(animationSlot, resultInfo);

            foreach (var animationSlot in GetPoseAndDockAnimationsSlots())
                CopyAnimations(animationSlot, resultInfo);

            foreach (var animationSlot in GetPortholeSlots())
                CreateAnimation(animationSlot, animationSlot, resultInfo);

            foreach (var animationSlot in GetMatchedAnimations())
                CreateAnimation(animationSlot.Item1, animationSlot.Item2, resultInfo);

            // Casting, ranged
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

        List<string> GetPoseAndDockAnimationsSlots()
        {
            var poses = _riderFragment.Fragments
              .Where(x => x.Slot.Value.StartsWith("HAND_POSE_", StringComparison.CurrentCultureIgnoreCase) || x.Slot.Value.StartsWith("FACE_POSE", StringComparison.CurrentCultureIgnoreCase))
              .Select(x => x.Slot.Value)
              .Distinct();

            var docks = _riderFragment.Fragments
              .Where(x => x.Slot.Value.StartsWith("DOCK_", StringComparison.CurrentCultureIgnoreCase))
              .Select(x => x.Slot.Value)
              .Distinct();

            return poses.Concat(docks).ToList();
        }

        List<string> GetMetaDataSlots()
        {
            return _riderFragment.Fragments
                   .Where(x => x.Slot.Value.StartsWith("PERSISTENT_METADATA_", StringComparison.CurrentCultureIgnoreCase))
                   .Select(x => x.Slot.Value)
                   .Distinct()
                   .ToList();
        }

        List<string> GetPortholeSlots()
        {
            return _riderFragment.Fragments
                  .Where(x => x.Slot.Value.StartsWith("PORTHOLE_", StringComparison.CurrentCultureIgnoreCase))
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

        void CreateFiles(BatchProcessOptions batchProcessOptions)
        {
            //AnimationPackLoader
            _outAnimPack = new AnimationPackFile();
            _outAnimPack.AnimationBin = new AnimationBin("animations/animation_tables/" + _animBinName);
            var tableEntry = new AnimationBinEntry(_fragmentName, _riderFragment.Skeletons.Values.First(), _mountFragment.Skeletons.Values.First());
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

        string GenerateNewAnimationName(string fullPath, string prefix)
        {
            return Path.GetDirectoryName(fullPath) + "\\" + prefix + Path.GetFileName(fullPath);
        }
    }
}