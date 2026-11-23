using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimatioReTarget.Editor.BoneHandling.Presentation;
using Editors.Shared.Core.Common;
using GameWorld.Core.Services;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.AnimatioReTarget.Editor.BoneHandling
{
    public partial class BoneManager : ObservableObject
    {
        record SkeletonInfo(string Name, AnimationFile Data);

        private readonly IStandardDialogs _standardDialogs;
        private readonly IAbstractFormFactory<BoneMappingWindow> _boneMappingWindowFactory;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        private ISkeletonBoneHighlighter? _skeletonBoneHighlighter;
        private RemappedAnimatedBoneConfiguration? _activeConfig;
        private SkeletonInfo? _sourceSkeleton;
        private SkeletonInfo? _targetSkeleton;

        [ObservableProperty] SkeletonBoneNode_new? _selectedBone;
        [ObservableProperty] ObservableCollection<SkeletonBoneNode_new> _bones = [];

        /// <summary>Every bone in <see cref="Bones"/>, depth first, for the "Is relative to" picker.</summary>
        [ObservableProperty] List<SkeletonBoneNode_new> _flatBones = [];

        public BoneManager(
            IStandardDialogs standardDialogs, 
            IAbstractFormFactory<BoneMappingWindow> boneMappingWindowFactory,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _standardDialogs = standardDialogs;
            _boneMappingWindowFactory = boneMappingWindowFactory;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
        }

        partial void OnSelectedBoneChanged(SkeletonBoneNode_new? value)
        {
            if (_skeletonBoneHighlighter == null)
                return;
                
            if(value == null)
            {
                _skeletonBoneHighlighter.SelectSourceSkeletonBone(-1);
                _skeletonBoneHighlighter.SelectTargetSkeletonBone(-1);
            }
            else
            {
                _skeletonBoneHighlighter.SelectSourceSkeletonBone(value.BoneIndex);
                if(value.HasMapping)
                    _skeletonBoneHighlighter.SelectTargetSkeletonBone(value.MappedIndex);
                else
                    _skeletonBoneHighlighter.SelectTargetSkeletonBone(-1);
            }
        }

        public void SetSceneNodes(SceneObject source, SceneObject target, SceneObject generated)
        {
            _skeletonBoneHighlighter = new SkeletonBoneHighlighter(source, generated);
        }

        public void UpdateTargetSkeleton(string? skeletonName) 
        {
            if (skeletonName == _targetSkeleton?.Name)
                return;

            if (skeletonName == null)
            {
                _targetSkeleton = null;
            }
            else
            {
                var skeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(skeletonName);
                _targetSkeleton = new SkeletonInfo(skeletonName, skeleton);
            }

            Invalidate();
        }

        public void UpdateSourceSkeleton(string? skeletonName) 
        {
            if (skeletonName == _sourceSkeleton?.Name)
                return;

            if (skeletonName == null)
            {
                _sourceSkeleton = null;
            }
            else
            {
                var skeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(skeletonName);
                _sourceSkeleton = new SkeletonInfo(skeletonName, skeleton);
            }

            Invalidate();
        }

        void Invalidate()
        {
            Bones.Clear();
            // One settings node per target bone: the remapper and ApplyMapping look them up by target bone index
            if (_targetSkeleton != null)
            {
                Bones = SkeletonBoneNodeHelper.Build(_targetSkeleton.Data);
            }
            FlatBones = Flatten(Bones).ToList();
            SelectedBone = Bones.FirstOrDefault();
            _activeConfig = null;
        }

        public void ApplyDefaultMapping()
        {
            CreateMappingConfig();
            BoneMappingHelper.AutomapDirectBoneLinksBasedOnNames(_activeConfig.MeshBones.First(), _activeConfig.ParentModelBones);
            SkeletonBoneNodeHelper.ApplyMapping(Bones, _activeConfig);
        }


        [RelayCommand] void ShowBoneMappingWindow()
        {
            if (_targetSkeleton == null || _sourceSkeleton == null)
            {
                _standardDialogs.ShowDialogBox("Source or target skeleton not selected", "Error");
                return;
            }

            CreateMappingConfig();

            // Make this possible to use without being a modal!
            var handle = _boneMappingWindowFactory.Create();
            handle.ViewModel.Initialize(_activeConfig);
            var result = handle.ShowDialog();

            if ((result.HasValue && result.Value == true) == false)
                return;

            SkeletonBoneNodeHelper.ApplyMapping(Bones, _activeConfig);
        }

        void CreateMappingConfig()
        {

            if (_activeConfig == null)
            {
                _activeConfig = new RemappedAnimatedBoneConfiguration
                {
                    MeshSkeletonName = _targetSkeleton.Name,
                    MeshBones = AnimatedBoneHelper.CreateFromSkeleton(_targetSkeleton.Data),

                    ParnetModelSkeletonName = _sourceSkeleton.Name,
                    ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(_sourceSkeleton.Data),

                    SkeletonBoneHighlighter = _skeletonBoneHighlighter
                };
            }
        }

        static IEnumerable<SkeletonBoneNode_new> Flatten(IEnumerable<SkeletonBoneNode_new> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                foreach (var child in Flatten(node.Children))
                    yield return child;
            }
        }

        [RelayCommand] void ClearRelativeBone()
        {
            if (SelectedBone != null)
                SelectedBone.SelectedRelativeBone = null;
        }

        /// <summary>Puts the selected bone's settings back to their defaults. Its bone mapping is
        /// left alone, since that's set in the bone mapping window rather than here.</summary>
        [RelayCommand] void ResetSelectedBone()
        {
            if (SelectedBone == null)
                return;

            SelectedBone.IsLocalOffset = false;
            SelectedBone.BoneLengthMult = 1;
            SelectedBone.RotationOffset = new(0, 0, 0);
            SelectedBone.TranslationOffset = new(0, 0, 0);
            SelectedBone.ForceSnapToWorld = false;
            SelectedBone.FreezeTranslation = false;
            SelectedBone.FreezeRotation = false;
            SelectedBone.FreezeRotationZ = false;
            SelectedBone.ApplyTranslation = true;
            SelectedBone.ApplyRotation = true;
            SelectedBone.SelectedRelativeBone = null;
        }


    }
}
