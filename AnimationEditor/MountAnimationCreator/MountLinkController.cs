using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Common;
using CommonControls.Services;
using FileTypes.AnimationPack;
using FileTypes.PackFiles.Models;
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
        public FilterCollection<FragmentDisplayItem> SelectedMount { get; set; }
        public FilterCollection<FragmentDisplayItem> SelectedRider { get; set; }

        public FilterCollection<SlotDisplayItem> SelectedMountTag { get; set; }
        public FilterCollection<SlotDisplayItem> SelectedRiderTag { get; set; }

        AssetViewModel _rider;
        AssetViewModel _mount;
        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public MountLinkController(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel rider, AssetViewModel mount)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _rider = rider;
            _mount = mount;

            SelectedMountTag = new FilterCollection<SlotDisplayItem>(null, MountTagSeleted);
            SelectedRiderTag = new FilterCollection<SlotDisplayItem>(null, RiderTagSelected);
            SelectedMount = new FilterCollection<FragmentDisplayItem>(null, (value) => MuntSelected(value, SelectedMountTag, _mount.SkeletonName));
            SelectedRider = new FilterCollection<FragmentDisplayItem>(null, (value) => MuntSelected(value, SelectedRiderTag, _rider.SkeletonName));

            SelectedMountTag.SearchFilter = (value, rx) => { return rx.Match(value.DisplayName).Success; };
            SelectedRiderTag.SearchFilter = (value, rx) => { return rx.Match(value.DisplayName).Success; };
            SelectedMount.SearchFilter = (value, rx) => { return rx.Match(value.DisplayName).Success; };
            SelectedRider.SearchFilter = (value, rx) => { return rx.Match(value.DisplayName).Success; };

            ReloadFragments();
        }

        public void ReloadFragments()
        {
            var mountSkeletonName = Path.GetFileNameWithoutExtension(_mount.SkeletonName);
            var riderSkeletonName = Path.GetFileNameWithoutExtension(_rider.SkeletonName);

            var allPossibleMount = new List<FragmentDisplayItem>();
            var allPossibleRider = new List<FragmentDisplayItem>();

            var animPacks = _pfs.FindAllWithExtention(@".animpack");
            foreach (var animPack in animPacks)
            {
                var animPackFile = new AnimationPackFile(animPack);
                foreach (var fragment in animPackFile.Fragments)
                {
                    if(fragment.Skeletons.Values.FirstOrDefault() == mountSkeletonName)
                        allPossibleMount.Add(new FragmentDisplayItem(fragment));
                    else if(fragment.Skeletons.Values.FirstOrDefault() == riderSkeletonName)
                        allPossibleRider.Add(new FragmentDisplayItem(fragment));
                }
            }

            var allFragments = _pfs.FindAllWithExtention(@".frg");
            foreach (var fragmentPack in allFragments)
            {
                var fragment = new AnimationFragment(fragmentPack.Name, fragmentPack.DataSource.ReadDataAsChunk());
                if (fragment.Skeletons.Values.FirstOrDefault() == mountSkeletonName)
                    allPossibleMount.Add(new FragmentDisplayItem(fragment));
                else if (fragment.Skeletons.Values.FirstOrDefault() == riderSkeletonName)
                    allPossibleRider.Add(new FragmentDisplayItem(fragment));
            }

            SelectedMount.UpdatePossibleValues(allPossibleMount);
            SelectedRider.UpdatePossibleValues(allPossibleRider);
        }

        void MuntSelected(FragmentDisplayItem value, FilterCollection<SlotDisplayItem> collection, string skeletonName)
        {
            if (value == null)
            {
                collection.UpdatePossibleValues(null);
                return;
            }
            var newSkeletonName = value.Entry.Skeletons.Values.FirstOrDefault();
            var existingSkeletonName = Path.GetFileNameWithoutExtension(skeletonName);
            if (newSkeletonName != existingSkeletonName)
            {
                MessageBox.Show("This fragment does not fit the current skeleton");
                return;
            }

            collection.UpdatePossibleValues(value.Entry.Fragments.Select(x =>new SlotDisplayItem(x)));
        }

        private void MountTagSeleted(SlotDisplayItem value)
        {
            if (value == null)
                return;

            var file = _pfs.FindFile(value.Entry.AnimationFile);
            var animationRef = _skeletonAnimationLookUpHelper.FindAnimationRefFromPackFile(file, _pfs);
            _mount.SetAnimation(animationRef);

            var lookUp = "RIDER_" + value.Entry.Slot.Value;
            SelectedRiderTag.SelectedItem = SelectedRiderTag.Values.FirstOrDefault(x => x.Entry.Slot.Value == lookUp);
        }

        private void RiderTagSelected(SlotDisplayItem value)
        {
            if (value == null)
                return;

            var file = _pfs.FindFile(value.Entry.AnimationFile);
            var animationRef = _skeletonAnimationLookUpHelper.FindAnimationRefFromPackFile(file, _pfs);
            _rider.SetAnimation(animationRef);
        }


        // Can we remove this?
        public class FragmentDisplayItem
        {
            public AnimationFragment Entry { get; set; }
            public FragmentDisplayItem(AnimationFragment entry)
            {
                Entry = entry;
            }

            public string DisplayName { get => Entry.FileName; }
        }

        public class SlotDisplayItem
        {
            public AnimationFragmentEntry Entry { get; set; }
            public SlotDisplayItem(AnimationFragmentEntry entry)
            {
                Entry = entry;
            }

            public string DisplayName { get => Entry.Slot.Value; }
        }
    }

    
}
