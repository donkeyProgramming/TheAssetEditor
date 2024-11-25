using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Common;
using Shared.Ui.Events.UiCommands;
using static Editors.Shared.Core.Services.SkeletonAnimationLookUpHelper;

namespace Editors.Shared.Core.Common.ReferenceModel
{
    public partial class BinAnimationViewModel : ObservableObject
    {
        private readonly SceneObject _sceneObject;
        private readonly SceneObjectEditor _sceneObjectEditor;
        private readonly IPackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IUiCommandFactory _uiCommandFactory;

        [ObservableProperty] FilterCollection<IAnimationBinGenericFormat> _fragmentList;
        [ObservableProperty] FilterCollection<AnimationBinEntryGenericFormat> _fragmentSlotList;
        [ObservableProperty] string? _metaDataName;
        [ObservableProperty] string? _metaDataPersistName;
        [ObservableProperty] string? _animationFileName;

        public BinAnimationViewModel(SceneObjectEditor sceneObjectEditor, IPackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, SceneObject sceneObject, IUiCommandFactory uiCommandFactory)
        {
            _sceneObjectEditor = sceneObjectEditor;
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _sceneObject = sceneObject;
            _uiCommandFactory = uiCommandFactory;

            FragmentList = new FilterCollection<IAnimationBinGenericFormat>(null, (value) => OnFragmentSelected(value, FragmentSlotList, _sceneObject.SkeletonName.Value))
            {
                SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; }
            };
            FragmentSlotList = new FilterCollection<AnimationBinEntryGenericFormat>(null, OnAnimationSlotSelected)
            {
                SearchFilter = (value, rx) => { return rx.Match(value.SlotName).Success; }
            };

            OnSkeletonChange(_sceneObject.Skeleton);
            _sceneObject.SkeletonChanged += OnSkeletonChange;


  
        }

        public void PreviewSelectedSlot()
        {
            if (FragmentList.SelectedItem != null)
                _uiCommandFactory.Create<OpenEditorCommand>().ExecuteAsWindow(FragmentList.SelectedItem.PackFileReference.FileName, 800, 900);
        }


        private void OnSkeletonChange(GameSkeleton newValue)
        {
            FragmentList.SelectedItem = null;
            FragmentSlotList.SelectedItem = null;

            if (newValue == null)
            {
                FragmentList.UpdatePossibleValues(null);
                FragmentSlotList.UpdatePossibleValues(null);
                return;
            }

            var skeletonName = Path.GetFileNameWithoutExtension(_sceneObject.SkeletonName.Value);
            var allPossibleFragments = LoadFragmentsForSkeleton(skeletonName);
            FragmentList.UpdatePossibleValues(allPossibleFragments);
        }

        List<IAnimationBinGenericFormat> LoadFragmentsForSkeleton(string skeletonName, bool onlyPacksThatCanBeSaved = false)
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

        void OnFragmentSelected(IAnimationBinGenericFormat value, FilterCollection<AnimationBinEntryGenericFormat> animationSlotsCollection, string skeletonName)
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

        void OnAnimationSlotSelected(AnimationBinEntryGenericFormat value)
        {
            AnimationReference? animationReference = null;
            PackFile? meta = null;
            PackFile? persistMeta = null;
            string? persistFullpath = null;

            if (string.IsNullOrWhiteSpace(value?.AnimationFile) == false)
            {
                var file = _pfs.FindFile(value.AnimationFile);
                animationReference = _skeletonAnimationLookUpHelper.FindAnimationRefFromPackFile(file);
            }

            if (string.IsNullOrWhiteSpace(value?.MetaFile) == false)
                meta = _pfs.FindFile(value.MetaFile);

            var persist = FragmentSlotList.PossibleValues.FirstOrDefault(x => x.SlotName == "PERSISTENT_METADATA_ALIVE");
            if (persist != null && string.IsNullOrWhiteSpace(persist.MetaFile) == false)
            {
                persistMeta = _pfs.FindFile(persist.MetaFile);
                persistFullpath = persist.MetaFile;
            }

            if (persistMeta == null)
            {
                var persistFlying = FragmentSlotList.PossibleValues.FirstOrDefault(x => x.SlotName == "PERSISTENT_METADATA_FLYING");
                if (persistFlying != null && string.IsNullOrWhiteSpace(persistFlying.MetaFile) == false)
                { 
                    persistMeta = _pfs.FindFile(persistFlying.MetaFile);
                    persistFullpath = persistFlying.MetaFile;
                }
            }

            _sceneObjectEditor.SetAnimation(_sceneObject, animationReference);
            _sceneObjectEditor.SetMetaFile(_sceneObject, meta, persistMeta);

            MetaDataName = value?.MetaFile;
            MetaDataPersistName = persistFullpath;
            if (animationReference != null)
                AnimationFileName = "[" + animationReference?.Container.Name + "] " + animationReference?.AnimationFile;
            else
                AnimationFileName = null;
        }
    }
}
