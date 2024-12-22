using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommonControls.SelectionListDialog;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.AnimationVisualEditors.AnimationTransferTool;
using Editors.AnimationVisualEditors.AnimationTransferTool.BoneHandling;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.Ui.BaseDialogs.MathViews;
using Shared.Ui.BaseDialogs.SelectionListDialog;
using Shared.Ui.Common;
using Shared.Ui.Editors.BoneMapping;

namespace AnimationEditor.AnimationTransferTool
{
    // When applying proportion scling, should also rotation be scaled? 
    // Show scale factor in view for each bone 



    public class BoneHandler
    {
        // Selected bone
        // All bones array

        // Send event => Updated

        // Open mapping window

    }

    public class RenderHelper
    { 
        // uses boneHandler

        // Draw selected bones

        // Handle offset
        // Draw on groud. Source, target, generated
    
    }

    public class Configuration
    { 
        // Source skeleton
        // Target skeleton

        // Global scale
        // Use Relaitve scale
        // Zero unmapped bones
        // Speed mult
    
    }


    public class AnimationRegargeter
    {
        // uses BoneHandler to get data
        // Outputes an animation

    }

    public class SaveHandler
    {
        // Used AnimationRegargeter

        // Save
        // Batch Save

    }




    public partial class AnimationTransferToolViewModel : EditorHostBase
    {
        AnimationToolInput _inputTargetData;
        AnimationToolInput _inputSourceData;

        private readonly SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        private readonly SceneObjectEditor _assetViewModelBuilder;
        private readonly IFileSaveService _packFileSaveService;
       // private readonly ILogger _logger = Logging.Create<AnimationTransferToolViewModel>();
        private readonly IPackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly AnimationPlayerViewModel _player;

        private SceneObject _copyTo;
        private SceneObject _copyFrom;
        private SceneObject Generated { get; set; }

        List<IndexRemapping> _remappingInformation;
        RemappedAnimatedBoneConfiguration _config;
        //AssetEditorWindow<BoneMappingViewModel> _activeBoneMappingWindow;

        FilterCollection<SkeletonBoneNode> ModelBoneList { get; set; } = new FilterCollection<SkeletonBoneNode>(null);
        ObservableCollection<SkeletonBoneNode> Bones { get; set; } = [];
        ObservableCollection<SkeletonBoneNode> FlatBoneList { get; set; } = [];
        AnimationSettings AnimationSettings { get; set; } = new AnimationSettings();

        NotifyAttr<SkeletonBoneNode> SelectedBone { get; set; } = new NotifyAttr<SkeletonBoneNode>();

        //---

        [ObservableProperty] BoneManager _boneManager;
        //--


        public override Type EditorViewModelType => typeof(EditorView);

        public AnimationTransferToolViewModel(
            BoneManager boneManager,

            IPackFileService pfs, IEditorHostParameters editorHostParameters,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            AnimationPlayerViewModel player,
            SceneObjectViewModelBuilder referenceModelSelectionViewModelBuilder,
            SceneObjectEditor assetViewModelBuilder,
            IFileSaveService packFileSaveService) : base(editorHostParameters)
        {

            _boneManager = boneManager;


            DisplayName = "Animation transfer tool";

            _sceneObjectViewModelBuilder = referenceModelSelectionViewModelBuilder;
            _assetViewModelBuilder = assetViewModelBuilder;
            _packFileSaveService = packFileSaveService;
     
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _player = player;

            SelectedBone.PropertyChanged += (x, y) => HightlightSelectedBones((x as NotifyAttr<SkeletonBoneNode>).Value);

            Initialize();
        }

        public void SetDebugInputParameters(AnimationToolInput target, AnimationToolInput source)
        {
            _inputTargetData = target;
            _inputSourceData = source;
        }

        void Initialize()
        {
            _inputTargetData = new AnimationToolInput()
            {
                Mesh = _pfs.FindFile(@"variantmeshes\variantmeshdefinitions\dwf_giant_slayers.variantmeshdefinition")
            };

            _inputSourceData = new AnimationToolInput()
            {
                Mesh = _pfs.FindFile(@"variantmeshes\variantmeshdefinitions\emp_archer_ror.variantmeshdefinition"),
                Animation = _pfs.FindFile(@"animations\battle\humanoid01\sword_and_pistol\missile_attacks\hu1_swp_missile_attack_aim_to_shootready_01.anim")
            };

            var target = _sceneObjectViewModelBuilder.CreateAsset(true, "Target", Color.Black, _inputTargetData);
            var source = _sceneObjectViewModelBuilder.CreateAsset(true, "Source", Color.Black, _inputSourceData);
            var sourceView = _sceneObjectViewModelBuilder.CreateAsset(true, "Generated", Color.Black, null);
            sourceView.Data.IsSelectable = false;
            sourceView.IsExpand = false;
            sourceView.IsEnabled = false;
            //sourceView.


            var generated = sourceView.Data;// _assetViewModelBuilder.CreateAsset("", Color.Black);

            source.Data.IsSelectable = false;

            _player.RegisterAsset(generated);
            Create(target.Data, source.Data, generated);

            SceneObjects.Add(target);
            SceneObjects.Add(source);
            SceneObjects.Add(sourceView);

            BoneManager.SetSceneNodes(source.Data, target.Data);
        }


        void Create(SceneObject copyToAsset, SceneObject copyFromAsset, SceneObject generated)
        {
            _copyTo = copyToAsset;
            _copyFrom = copyFromAsset;
            Generated = generated;

            _copyFrom.SkeletonChanged += CopyFromSkeletonChanged;
            _copyTo.MeshChanged += CopyToMeshChanged;

            if (_copyTo.Skeleton != null)
                CopyToMeshChanged(_copyTo);

            if (_copyFrom.Skeleton != null)
                CopyFromSkeletonChanged(_copyFrom.Skeleton);
            AnimationSettings.DisplayOffset.OnValueChanged += DisplayOffset_OnValueChanged;
            DisplayOffset_OnValueChanged(new Vector3ViewModel(0, 0, 2));

            BoneManager.UpdateSourceSkeleton(_copyFrom.Skeleton.SkeletonName);
            BoneManager.UpdateTargetSkeleton(_copyTo.Skeleton.SkeletonName);
        }

        private void DisplayOffset_OnValueChanged(Vector3ViewModel newValue)
        {
            _copyTo.Offset = Matrix.CreateTranslation(newValue.GetAsVector3() * -1);
            _copyFrom.Offset = Matrix.CreateTranslation(newValue.GetAsVector3());
        }

        void HightlightSelectedBones(SkeletonBoneNode bone)
        {
            if (bone == null)
            {
                Generated.SelectedBoneIndex(-1);
                _copyFrom.SelectedBoneIndex(-1);
            }
            else
            {
                Generated.SelectedBoneIndex(bone.BoneIndex.Value);
                if (_remappingInformation != null)
                {
                    var mapping = _remappingInformation.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                    if (mapping != null)
                        _copyFrom.SelectedBoneIndex(mapping.NewValue);
                }
            }
        }

        private void CopyToMeshChanged(SceneObject newValue)
        {
            _assetViewModelBuilder.CopyMeshFromOther(Generated, newValue);
            CreateBoneOverview(newValue.Skeleton);
            HightlightSelectedBones(null);

            _config = null;
            AnimationSettings.UseScaledSkeletonName.Value = false;
            AnimationSettings.ScaledSkeletonName.Value = "";
        }

        private void CopyFromSkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == _copyFrom.Skeleton)
                return;

            _remappingInformation = null;
            CreateBoneOverview(_copyTo.Skeleton);
            HightlightSelectedBones(null);

            var standAnim = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(newValue.SkeletonName).FirstOrDefault(x => x.AnimationFile.Contains("stand"));
            if (standAnim != null)
                _assetViewModelBuilder.SetAnimation(_copyFrom, standAnim);

            _config = null;
            AnimationSettings.UseScaledSkeletonName.Value = false;
            AnimationSettings.ScaledSkeletonName.Value = "";
        }

        public void OpenMappingWindow()
        {
           ////if (_activeBoneMappingWindow != null)
           ////{
           ////    _activeBoneMappingWindow.Focus();
           ////    return;
           ////}
           //
           //if (_copyTo.Skeleton == null || _copyFrom.Skeleton == null)
           //{
           //    MessageBox.Show("Source or target skeleton not selected", "Error");
           //    return;
           //}
           //
           //var targetSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_copyTo.SkeletonName.Value);
           //var sourceSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_copyFrom.SkeletonName.Value);
           //
           //if (_config == null)
           //{
           //    _config = new RemappedAnimatedBoneConfiguration();
           //    _config.MeshSkeletonName = _copyTo.SkeletonName.Value;
           //    _config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(targetSkeleton);
           //
           //    _config.ParnetModelSkeletonName = _copyFrom.SkeletonName.Value;
           //    _config.ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(sourceSkeleton);
           //    _config.SkeletonBoneHighlighter = new SkeletonBoneHighlighter(Generated, _copyFrom);
           //}
           //
           ////_activeBoneMappingWindow = _windowFactory.Create<BoneMappingViewModel, BoneMappingView>("Bone-Mapping", 1200, 1100);
           ////_activeBoneMappingWindow.TypedContext.Initialize(_activeBoneMappingWindow, _config);
           ////var windowResult = _activeBoneMappingWindow.ShowWindow(true);
           ////if (windowResult == true)
           ////{
           ////    _remappingInformation = AnimatedBoneHelper.BuildRemappingList(_config.MeshBones.First());
           ////    UpdateAnimation();
           ////    UpdateBonesAfterMapping(Bones);
           ////}
           //
           //// TODO
           ///* _activeBoneMappingWindow = new BoneMappingWindow(new BoneMappingViewModel(_config), false);
           // _activeBoneMappingWindow.Show();
           // _activeBoneMappingWindow.ApplySettings += BoneMappingWindow_Apply;
           // _activeBoneMappingWindow.Closed += BoneMappingWindow_Closed;*/
        }

        //private void BoneMappingWindow_Apply(object sender, EventArgs e)
        //{
        //
        //}
        //
        //private void BoneMappingWindow_Closed(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (_activeBoneMappingWindow.Result == true)
        //        {
        //            _remappingInformation = AnimatedBoneHelper.BuildRemappingList(_config.MeshBones.First());
        //            UpdateAnimation();
        //            UpdateBonesAfterMapping(Bones);
        //        }
        //    }
        //    finally
        //    {
        //        _activeBoneMappingWindow.Closed -= BoneMappingWindow_Closed;
        //        _activeBoneMappingWindow.ApplySettings -= BoneMappingWindow_Apply;
        //        _activeBoneMappingWindow = null;
        //    }
        //}

        void UpdateBonesAfterMapping(IEnumerable<SkeletonBoneNode> bones)
        {
            foreach (var bone in bones)
            {
                var mapping = _remappingInformation.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                bone.HasMapping.Value = mapping != null;
                UpdateBonesAfterMapping(bone.Children);
            }
        }

        public void ClearRelativeSelectedBoneAction()
        {
            if (SelectedBone.Value != null)
                SelectedBone.Value.SelectedRelativeBone.Value = null;
        }

      


        public void ClearAllSettings()
        {
            if (MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                CreateBoneOverview(_copyTo.Skeleton);
        }

        public void UseTargetAsSource()
        {
            if (MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                AnimationSettings.UseScaledSkeletonName.Value = false;
                AnimationSettings.ScaledSkeletonName.Value = "";
                _assetViewModelBuilder.CopyMeshFromOther(_copyFrom, _copyTo);
            }
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone.Value = null;
            Bones.Clear();
            FlatBoneList.Clear();
            FlatBoneList.Add(null);

            if (skeleton == null)
                return;
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                SkeletonBoneNode newBone = null;
                var parentBoneId = skeleton.GetParentBoneIndex(i);
                if (parentBoneId == -1)
                {
                    newBone = new SkeletonBoneNode(skeleton.BoneNames[i], i, -1);
                    Bones.Add(newBone);
                }
                else
                {
                    var treeParent = BoneHelper.GetBoneFromId(Bones, parentBoneId);
                    if (treeParent != null)
                    {
                        newBone = new SkeletonBoneNode(skeleton.BoneNames[i], i, parentBoneId);
                        treeParent.Children.Add(newBone);
                    }
                }

                FlatBoneList.Add(newBone);
            }
        }

        public void ExportMappedSkeleton() => ExportHelper.ExportMappedSkeleton();
        public void ExportScaledMesh() => ExportHelper.ExportScaledMesh();
    }
}
