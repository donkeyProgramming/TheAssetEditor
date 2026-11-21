using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Editors.Shared.Core.Common;
using GameWorld.Core.Services;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Common;

namespace AnimationEditor.MountAnimationCreator.ViewModels
{
    public class MountLinkViewModel : NotifyPropertyChangedImpl
    {
        public FilterCollection<IAnimationBinGenericFormat> AnimationSetForMount { get; set; }
        public FilterCollection<IAnimationBinGenericFormat> AnimationSetForRider { get; set; }

        public FilterCollection<AnimationBinEntryGenericFormat> SelectedMountTag { get; set; }
        public FilterCollection<AnimationBinEntryGenericFormat> SelectedRiderTag { get; set; }

        private readonly SceneObject _rider;
        private readonly SceneObject _mount;
        private readonly SceneObjectEditor _assetViewModelEditor;
        private readonly IPackFileService _pfs;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly Action _validateAction;

        public MountLinkViewModel(SceneObjectEditor assetViewModelEditor, IPackFileService pfs, ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, SceneObject rider, SceneObject mount, Action validate)
        {
            _assetViewModelEditor = assetViewModelEditor;
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _rider = rider;
            _mount = mount;
            _validateAction = validate;

            AnimationSetForMount = new FilterCollection<IAnimationBinGenericFormat>(null, (value) => UpdateAnimationSet(value, SelectedMountTag, _mount.SkeletonName.Value));
            AnimationSetForRider = new FilterCollection<IAnimationBinGenericFormat>(null, (value) => UpdateAnimationSet(value, SelectedRiderTag, _rider.SkeletonName.Value));
            SelectedMountTag = new FilterCollection<AnimationBinEntryGenericFormat>(null, MountTagSeleted);
            SelectedRiderTag = new FilterCollection<AnimationBinEntryGenericFormat>(null, RiderTagSelected);

            SelectedMountTag.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };
            SelectedRiderTag.SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; };
            AnimationSetForMount.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };
            AnimationSetForRider.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };

            ReloadFragments();
        }

        /// <summary>Reloads the set lists, keeping the selected sets and slots where they're still there.</summary>
        public void ReloadFragments(bool rider = true, bool mount = true)
        {
            if (mount)
                Reload(AnimationSetForMount, SelectedMountTag, _mount.SkeletonName.Value);
            if (rider)
                Reload(AnimationSetForRider, SelectedRiderTag, _rider.SkeletonName.Value);
        }

        void Reload(FilterCollection<IAnimationBinGenericFormat> sets, FilterCollection<AnimationBinEntryGenericFormat> slots, string skeletonPath)
        {
            var selectedSet = sets.SelectedItem?.FullPath;
            var selectedSlot = slots.SelectedItem;

            sets.UpdatePossibleValues(LoadAnimationSetForSkeleton(Path.GetFileNameWithoutExtension(skeletonPath)));
            if (selectedSet == null)
                return;

            sets.SelectedItem = sets.PossibleValues.FirstOrDefault(x => x.FullPath == selectedSet);
            if (sets.SelectedItem != null && selectedSlot != null)
                slots.SelectedItem = slots.PossibleValues.FirstOrDefault(x => x.SlotName == selectedSlot.SlotName && x.AnimationFile == selectedSlot.AnimationFile);
        }

        public List<IAnimationBinGenericFormat> LoadAnimationSetForSkeleton(string skeletonName, bool onlyPacksThatCanBeSaved = false)
        {
            var outputFragments = new List<IAnimationBinGenericFormat>();
            var animPacks = PackFileServiceUtility.GetAllAnimPacks(_pfs);
            foreach (var animPack in animPacks)
            {
                if (onlyPacksThatCanBeSaved == true)
                {
                    if (_pfs.GetPackFileContainer(animPack).IsCaPackFile)
                        continue;
                }

                var animPackFile = AnimationPackSerializer.Load(animPack, _pfs);
                var fragments = animPackFile.GetGenericAnimationSets(skeletonName);
                foreach (var fragment in fragments)
                    outputFragments.Add(fragment);
            }
            return outputFragments;
        }

        void UpdateAnimationSet(IAnimationBinGenericFormat value, FilterCollection<AnimationBinEntryGenericFormat> collection, string skeletonName)
        {
            if (value == null)
            {
                collection.UpdatePossibleValues(null);
                _validateAction();
                return;
            }

            var newSkeletonName = value.SkeletonName;
            var existingSkeletonName = Path.GetFileNameWithoutExtension(skeletonName);
            if (newSkeletonName != existingSkeletonName)
            {
                MessageBox.Show("This fragment does not fit the current skeleton");
                return;
            }

            collection.UpdatePossibleValues(value.Entries);
            _validateAction();
        }

        private void MountTagSeleted(AnimationBinEntryGenericFormat value)
        {
            if (value != null)
            {
                _assetViewModelEditor.SetAnimation(_mount, FindAnimation(value.AnimationFile));

                var lookUp = "RIDER_" + value.SlotName;
                SelectedRiderTag.Filter = "";
                SelectedRiderTag.SelectedItem = SelectedRiderTag.Values.FirstOrDefault(x => x.SlotName == lookUp);
            }
            _validateAction();
        }

        private void RiderTagSelected(AnimationBinEntryGenericFormat value)
        {
            if (value != null)
                _assetViewModelEditor.SetAnimation(_rider, FindAnimation(value.AnimationFile));
            _validateAction();
        }

        // Some vanilla bin entries name animations that don't exist; they load as none
        string FindAnimation(string path) => string.IsNullOrWhiteSpace(path) || _pfs.FindFile(path) == null ? null : path;
    }
}
