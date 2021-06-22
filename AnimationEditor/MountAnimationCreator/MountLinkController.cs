using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Common;
using CommonControls.Services;
using FileTypes.AnimationPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace AnimationEditor.MountAnimationCreator
{
    public class MountLinkController : NotifyPropertyChangedImpl
    {
        public FilterCollection<AnimationFragment> SelectedMount { get; set; }
        public FilterCollection<AnimationFragment> SelectedRider { get; set; }

        public FilterCollection<FragmentStatusSlotItem> SelectedMountTag { get; set; }
        public FilterCollection<FragmentStatusSlotItem> SelectedRiderTag { get; set; }

        AssetViewModel _rider;
        AssetViewModel _mount;
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        Action _validateAction;

        public MountLinkController(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel rider, AssetViewModel mount, Action validate)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _rider = rider;
            _mount = mount;
            _validateAction = validate;

            SelectedMountTag = new FilterCollection<FragmentStatusSlotItem>(null, MountTagSeleted);
            SelectedRiderTag = new FilterCollection<FragmentStatusSlotItem>(null, RiderTagSelected);
            SelectedMount = new FilterCollection<AnimationFragment>(null, (value) => MuntSelected(value, SelectedMountTag, _mount.SkeletonName));
            SelectedRider = new FilterCollection<AnimationFragment>(null, (value) => MuntSelected(value, SelectedRiderTag, _rider.SkeletonName));

            SelectedMountTag.SearchFilter = (value, rx) => { return rx.Match(value.Entry.Value.Slot.Value).Success; };
            SelectedRiderTag.SearchFilter = (value, rx) => { return rx.Match(value.Entry.Value.Slot.Value).Success; };
            SelectedMount.SearchFilter = (value, rx) => { return rx.Match(value.FileName).Success; };
            SelectedRider.SearchFilter = (value, rx) => { return rx.Match(value.FileName).Success; };

            ReloadFragments();
        }

        public void ReloadFragments(bool rider = true, bool mount = true)
        {
            if (mount)
            {
                var mountSkeletonName = Path.GetFileNameWithoutExtension(_mount.SkeletonName);
                var allPossibleMount = LoadFragmentsForSkeleton(mountSkeletonName);
                SelectedMount.UpdatePossibleValues(allPossibleMount);
            }

            if (rider)
            {
                var selectedSlotId = SelectedRiderTag.SelectedItem?.Entry.Value.Slot.Id;
                var selectedRider = SelectedRider.SelectedItem?.FileName;
                var riderSkeletonName = Path.GetFileNameWithoutExtension(_rider.SkeletonName);
                var allPossibleRider = LoadFragmentsForSkeleton(riderSkeletonName);
                SelectedRider.UpdatePossibleValues(allPossibleRider);
                if (selectedRider != null)
                    SelectedRider.SelectedItem = SelectedRider.Values.FirstOrDefault(x => x.FileName == selectedRider);

                if (selectedSlotId != null)
                    SelectedRiderTag.SelectedItem = SelectedRiderTag.Values.FirstOrDefault(x => x.Entry.Value.Slot.Id == selectedSlotId.Value);
            }
        }

        public List<AnimationFragment> LoadFragmentsForSkeleton(string skeletonName, bool onlyPacksThatCanBeSaved = false)
        {
            var outputFragments = new List<AnimationFragment>();
            var animPacks = _pfs.FindAllWithExtention(@".animpack");
            foreach (var animPack in animPacks)
            {
                if (onlyPacksThatCanBeSaved == true)
                {
                    if (_pfs.GetPackFileContainer(animPack).IsCaPackFile)
                        continue;
                }

                var animPackFile = new AnimationPackFile(animPack, skeletonName);
                foreach (var fragment in animPackFile.Fragments)
                {
                    outputFragments.Add(fragment);
                }
            }
            return outputFragments;
        }

        void MuntSelected(AnimationFragment value, FilterCollection<FragmentStatusSlotItem> collection, string skeletonName)
        {
            if (value == null)
            {
                collection.UpdatePossibleValues(null);
                _validateAction();
                return;
            }

            var newSkeletonName = value.Skeletons.Values.FirstOrDefault();
            var existingSkeletonName = Path.GetFileNameWithoutExtension(skeletonName);
            if (newSkeletonName != existingSkeletonName)
            {
                MessageBox.Show("This fragment does not fit the current skeleton");
                return;
            }

            collection.UpdatePossibleValues(value.Fragments.Select(x => new FragmentStatusSlotItem(x)));
            _validateAction();
        }

        private void MountTagSeleted(FragmentStatusSlotItem value)
        {
            if (value != null)
            {
                var file = _pfs.FindFile(value.Entry.Value.AnimationFile);
                var animationRef = _skeletonAnimationLookUpHelper.FindAnimationRefFromPackFile(file, _pfs);
                _mount.SetAnimation(animationRef);

                var lookUp = "RIDER_" + value.Entry.Value.Slot.Value;
                SelectedRiderTag.Filter = "";
                SelectedRiderTag.SelectedItem = SelectedRiderTag.Values.FirstOrDefault(x => x.Entry.Value.Slot.Value == lookUp);
            }
            _validateAction();
        }

        private void RiderTagSelected(FragmentStatusSlotItem value)
        {
            if (value != null)
            {
                var file = _pfs.FindFile(value.Entry.Value.AnimationFile);
                if (file == null)
                {
                    _rider.SetAnimation(null);
                }
                else
                {
                    var animationRef = _skeletonAnimationLookUpHelper.FindAnimationRefFromPackFile(file, _pfs);
                    _rider.SetAnimation(animationRef);
                }
            }
            _validateAction();
        }
       
    }
}
