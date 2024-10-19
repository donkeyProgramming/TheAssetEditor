using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Common;
using Shared.Ui.Events.UiCommands;

namespace Editors.Shared.Core.Common.ReferenceModel
{
    public class SelectFragAndSlotViewModel : NotifyPropertyChangedImpl
    {
        string _currentSkeletonName = "";
        private readonly SceneObject _asset;
        private readonly SceneObjectBuilder _assetViewModelEditor;
        private readonly PackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly SelectMetaViewModel _metaViewModel;
        private readonly IUiCommandFactory _uiCommandFactory;

        public FilterCollection<IAnimationBinGenericFormat> FragmentList { get; set; }

        public FilterCollection<AnimationBinEntryGenericFormat> FragmentSlotList { get; set; }

        public SelectFragAndSlotViewModel(SceneObjectBuilder assetViewModelEditor, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, SceneObject asset, SelectMetaViewModel metaViewModel, IUiCommandFactory uiCommandFactory)
        {
            _assetViewModelEditor = assetViewModelEditor;
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _asset = asset;
            _metaViewModel = metaViewModel;
            _uiCommandFactory = uiCommandFactory;
            FragmentList = new FilterCollection<IAnimationBinGenericFormat>(null, (value) => FragmentSelected(value, FragmentSlotList, _asset.SkeletonName.Value))
            {
                SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; }
            };
            FragmentSlotList = new FilterCollection<AnimationBinEntryGenericFormat>(null, FragmentSlotSelected)
            {
                SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; }
            };

            OnSkeletonChange(_asset.Skeleton);
            Subscribe();
        }

        public void PreviewSelectedSlot()
        {
            if (FragmentList.SelectedItem != null && FragmentList.SelectedItem != null)
            {
                var animPack = FragmentList.SelectedItem.PackFileReference;
                _uiCommandFactory.Create<OpenEditorCommand>().ExecuteAsWindow(animPack.FileName, 800, 900);
            }
        }

        void Subscribe()
        {
            _asset.SkeletonChanged += OnSkeletonChange;
        }

        private void OnSkeletonChange(GameSkeleton newValue)
        {
            Unsubscribe();

            FragmentList.SelectedItem = null;
            FragmentSlotList.SelectedItem = null;

            if (newValue == null)
            {
                FragmentList.UpdatePossibleValues(new List<IAnimationBinGenericFormat>());
                FragmentSlotList.UpdatePossibleValues(new List<AnimationBinEntryGenericFormat>());
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


        void Unsubscribe()
        {
            _asset.SkeletonChanged += OnSkeletonChange;
        }

        public List<IAnimationBinGenericFormat> LoadFragmentsForSkeleton(string skeletonName, bool onlyPacksThatCanBeSaved = false)
        {
            var outputFragments = new List<IAnimationBinGenericFormat>();
            var animPacks = _pfs.GetAllAnimPacks();
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

        void FragmentSelected(IAnimationBinGenericFormat value, FilterCollection<AnimationBinEntryGenericFormat> animationSlotsCollection, string skeletonName)
        {
            if (value == null)
            {
                animationSlotsCollection.UpdatePossibleValues(null);
                return;
            }

            var newSkeletonName = value.SkeletonName;
            var existingSkeletonName = Path.GetFileNameWithoutExtension(skeletonName);
            if (newSkeletonName != existingSkeletonName)
            {
                MessageBox.Show("This fragment does not fit the current skeleton");
                return;
            }

            animationSlotsCollection.UpdatePossibleValues(value.Entries);
        }

        private void FragmentSlotSelected(AnimationBinEntryGenericFormat value)
        {
            if (value == null)
            {
                _assetViewModelEditor.SetAnimation(_asset, null);
                _assetViewModelEditor.SetMetaFile(_asset, null, null);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(value.AnimationFile) == false)
                {
                    var file = _pfs.FindFile(value.AnimationFile);
                    var animationRef = _skeletonAnimationLookUpHelper.FindAnimationRefFromPackFile(file, _pfs);
                    _assetViewModelEditor.SetAnimation(_asset, animationRef);
                }
                else
                {
                    _assetViewModelEditor.SetAnimation(_asset, null);
                }

                if (string.IsNullOrWhiteSpace(value.MetaFile) == false)
                    _metaViewModel.SelectedMetaFile = _pfs.FindFile(value.MetaFile);
                else
                    _metaViewModel.SelectedMetaFile = null;

                var persist = FragmentSlotList.PossibleValues.FirstOrDefault(x => x.SlotName == "PERSISTENT_METADATA_ALIVE");
                if (persist != null && string.IsNullOrWhiteSpace(persist.MetaFile) == false)
                    _metaViewModel.SelectedPersistMetaFile = _pfs.FindFile(persist.MetaFile);
                else
                    _metaViewModel.SelectedPersistMetaFile = null;

                if (_metaViewModel.SelectedPersistMetaFile == null)
                {
                    var persistFlying = FragmentSlotList.PossibleValues.FirstOrDefault(x => x.SlotName == "PERSISTENT_METADATA_FLYING");
                    if (persistFlying != null && string.IsNullOrWhiteSpace(persistFlying.MetaFile) == false)
                        _metaViewModel.SelectedPersistMetaFile = _pfs.FindFile(persistFlying.MetaFile);
                }
            }
        }
    }
}
