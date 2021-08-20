using Common;
using CommonControls.Common;
using CommonControls.Services;
using FileTypes.AnimationPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectFragAndSlotViewModel : NotifyPropertyChangedImpl
    {
        AssetViewModel _asset;

        public FilterCollection<AnimationFragment> FragmentList { get; set; }

        public FilterCollection<AnimationFragmentEntry> FragmentSlotList { get; set; }

        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        SelectMetaViewModel _metaViewModel;


        public SelectFragAndSlotViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel asset, SelectMetaViewModel metaViewModel)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _asset = asset;
            _metaViewModel = metaViewModel;

            FragmentList = new FilterCollection<AnimationFragment>(null, (value) => FragmentSelected(value, FragmentSlotList, _asset.SkeletonName.Value))
            {
                SearchFilter = (value, rx) => { return rx.Match(value.FileName).Success; }
            };
            FragmentSlotList = new FilterCollection<AnimationFragmentEntry>(null, FragmentSlotSelected)
            {
                SearchFilter = (value, rx) => { return rx.Match(value.Slot.Value).Success; }
            };


            OnSkeletonChange(_asset.Skeleton);
            Subscribe();
        }

        void Subscribe()
        {
            _asset.AnimationChanged += OnAnimationChange;
            _asset.SkeletonChanged += OnSkeletonChange;
        }

        private void OnSkeletonChange(View3D.Animation.GameSkeleton newValue)
        {
            Unsubscribe();

            if (newValue == null)
            {
                FragmentList.UpdatePossibleValues(new List<AnimationFragment>());
                FragmentSlotList.UpdatePossibleValues(new List<AnimationFragmentEntry>());
                return;
            }
            var mountSkeletonName = Path.GetFileNameWithoutExtension(_asset.SkeletonName.Value);
            var allPossibleMount = LoadFragmentsForSkeleton(mountSkeletonName);
            FragmentList.UpdatePossibleValues(allPossibleMount);

            Subscribe();
        }

        private void OnAnimationChange(View3D.Animation.AnimationClip newValue)
        {
            //throw new NotImplementedException();
        }

        void Unsubscribe()
        {
            _asset.AnimationChanged += OnAnimationChange;
            _asset.SkeletonChanged += OnSkeletonChange;
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

        void FragmentSelected(AnimationFragment value, FilterCollection<AnimationFragmentEntry> collection, string skeletonName)
        {
            if (value == null)
            {
                collection.UpdatePossibleValues(null);
                return;
            }

            var newSkeletonName = value.Skeletons.Values.FirstOrDefault();
            var existingSkeletonName = Path.GetFileNameWithoutExtension(skeletonName);
            if (newSkeletonName != existingSkeletonName)
            {
                MessageBox.Show("This fragment does not fit the current skeleton");
                return;
            }

            collection.UpdatePossibleValues(value.Fragments);
        }

        private void FragmentSlotSelected(AnimationFragmentEntry value)
        {
            if(value == null)
            {
                _asset.SetAnimation(null);
                _asset.SetMetaFile(null, null);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(value.AnimationFile) == false)
                {
                    var file = _pfs.FindFile(value.AnimationFile);
                    var animationRef = _skeletonAnimationLookUpHelper.FindAnimationRefFromPackFile(file, _pfs);
                    _asset.SetAnimation(animationRef);
                }
                else
                {
                    _asset.SetAnimation(null);
                }

                if (string.IsNullOrWhiteSpace(value.MetaDataFile) == false)
                    _metaViewModel.SelectedMetaFile = _pfs.FindFile(value.MetaDataFile);
                else
                    _metaViewModel.SelectedMetaFile = null;

                var persist = FragmentSlotList.PossibleValues.FirstOrDefault(x => x.Slot.Value == "PERSISTENT_METADATA_ALIVE");
                if (persist != null && string.IsNullOrWhiteSpace(persist.MetaDataFile) == false)
                    _metaViewModel.SelectedPersistMetaFile = _pfs.FindFile(persist.MetaDataFile);
                else
                    _metaViewModel.SelectedPersistMetaFile = null;

                if (_metaViewModel.SelectedPersistMetaFile == null)
                {
                    var persistFlying = FragmentSlotList.PossibleValues.FirstOrDefault(x => x.Slot.Value == "PERSISTENT_METADATA_FLYING");
                    if (persistFlying != null && string.IsNullOrWhiteSpace(persistFlying.MetaDataFile) == false)
                        _metaViewModel.SelectedPersistMetaFile = _pfs.FindFile(persistFlying.MetaDataFile);
                }
            }
        }
    }
}
