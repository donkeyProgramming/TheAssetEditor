using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectFragAndSlotViewModel : NotifyPropertyChangedImpl
    {
        string _currentSkeletonName = "";
        AssetViewModel _asset;

        public FilterCollection<AnimationSetFile> FragmentList { get; set; }

        public FilterCollection<AnimationSetEntry> FragmentSlotList { get; set; }

        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        SelectMetaViewModel _metaViewModel;


        public SelectFragAndSlotViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel asset, SelectMetaViewModel metaViewModel)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _asset = asset;
            _metaViewModel = metaViewModel;

            FragmentList = new FilterCollection<AnimationSetFile>(null, (value) => FragmentSelected(value, FragmentSlotList, _asset.SkeletonName.Value))
            {
                SearchFilter = (value, rx) => { return rx.Match(value.FileName).Success; }
            };
            FragmentSlotList = new FilterCollection<AnimationSetEntry>(null, FragmentSlotSelected)
            {
                SearchFilter = (value, rx) => { return rx.Match(value.Slot.Value).Success; }
            };

            OnSkeletonChange(_asset.Skeleton);
            Subscribe();
        }


        public void PreviewSelectedSlot()
        {
            if (FragmentList.SelectedItem != null && FragmentList.SelectedItem != null)
            {
                var animPack = FragmentList.SelectedItem.Parent;
                CommonControls.Editors.AnimationPack.AnimPackViewModel.ShowPreviewWinodow(animPack, _pfs, _skeletonAnimationLookUpHelper, FragmentList.SelectedItem.FileName);
            }
        }

        void Subscribe()
        {
            _asset.AnimationChanged += OnAnimationChange;
            _asset.SkeletonChanged += OnSkeletonChange;
        }

        private void OnSkeletonChange(View3D.Animation.GameSkeleton newValue)
        {
            Unsubscribe();

            FragmentList.SelectedItem = null;
            FragmentSlotList.SelectedItem = null;

            if (newValue == null)
            {
                FragmentList.UpdatePossibleValues(new List<AnimationSetFile>());
                FragmentSlotList.UpdatePossibleValues(new List<AnimationSetEntry>());
                _currentSkeletonName = "";
                return;
            }

            // Same skeleton again, should fix this propper.
            if (_currentSkeletonName == _asset.SkeletonName.Value)
                return;

            _currentSkeletonName = _asset.SkeletonName.Value;
            var skeletonName = Path.GetFileNameWithoutExtension(_asset.SkeletonName.Value);
            var allPossibleFragments = LoadFragmentsForSkeleton(skeletonName);
            FragmentList.UpdatePossibleValues(allPossibleFragments);

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

        public List<AnimationSetFile> LoadFragmentsForSkeleton(string skeletonName, bool onlyPacksThatCanBeSaved = false)
        {
            var outputFragments = new List<AnimationSetFile>();
            var animPacks = _pfs.FindAllWithExtention(@".animpack");
            foreach (var animPack in animPacks)
            {
                if (onlyPacksThatCanBeSaved == true)
                {
                    if (_pfs.GetPackFileContainer(animPack).IsCaPackFile)
                        continue;
                }

                var animPackFile = AnimationPackSerializer.Load(animPack, _pfs);
                var fragments = animPackFile.GetAnimationSets(skeletonName);
                foreach (var fragment in fragments)
                    outputFragments.Add(fragment);
            }
            return outputFragments;
        }

        void FragmentSelected(AnimationSetFile value, FilterCollection<AnimationSetEntry> collection, string skeletonName)
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

        private void FragmentSlotSelected(AnimationSetEntry value)
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
