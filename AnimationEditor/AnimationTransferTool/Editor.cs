using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Common;
using CommonControls.Editors.BoneMapping;
using CommonControls.Editors.BoneMapping.View;
using CommonControls.SelectionListDialog;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation;
using View3D.Commands;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services;

namespace AnimationEditor.AnimationTransferTool
{
    public class Editor : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<Editor>();

        PackFileService _pfs;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        IComponentManager _componentManager;
        AssetViewModel _copyTo;
        AssetViewModel _copyFrom;
        public AssetViewModel Generated { get; set; }
        List<IndexRemapping> _remappingInformaton;
        RemappedAnimatedBoneConfiguration _config;

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public ObservableCollection<SkeletonBoneNode> FlatBoneList { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public AnimationSettings AnimationSettings { get; set; } = new AnimationSettings();

        SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value); HightlightSelectedBones(value); }
        }

        public Editor(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, AssetViewModel copyToAsset, AssetViewModel copyFromAsset, AssetViewModel generated, IComponentManager componentManager)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _componentManager = componentManager;

            _copyTo = copyToAsset;
            _copyFrom = copyFromAsset;
            Generated = generated;

            _copyFrom.SkeletonChanged += CopyFromSkeletonChanged;
            _copyTo.MeshChanged += CopyToMeshChanged;

            _copyTo.Offset = Matrix.CreateTranslation(new Vector3(0, 0, -2));
            _copyFrom.Offset = Matrix.CreateTranslation(new Vector3(0, 0, 2));

            AnimationSettings.OffsetGenerated.OnValueChanged += (vector) => generated.Offset = Matrix.CreateTranslation(new Vector3((float)vector.X.Value, (float)vector.Y.Value, (float)vector.Z.Value));
            AnimationSettings.OffsetTarget.OnValueChanged += (vector) => _copyTo.Offset = Matrix.CreateTranslation(new Vector3((float)vector.X.Value, (float)vector.Y.Value, (float)vector.Z.Value));
            AnimationSettings.OffsetSource.OnValueChanged += (vector) => _copyFrom.Offset = Matrix.CreateTranslation(new Vector3((float)vector.X.Value, (float)vector.Y.Value, (float)vector.Z.Value));

            if(_copyTo.Skeleton != null)
                CopyToMeshChanged(_copyTo);
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
                if (_remappingInformaton != null)
                { 
                    var mapping = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                    if (mapping != null)
                        _copyFrom.SelectedBoneIndex(mapping.NewValue);
                }
            }
        }

        private void CopyToMeshChanged(AssetViewModel newValue)
        {
            Generated.CopyMeshFromOther(newValue);
            CreateBoneOverview(newValue.Skeleton);
            HightlightSelectedBones(null);

            _config = null;
            AnimationSettings.UseScaledSkeletonName.Value = false;
            AnimationSettings.ScaledSkeletonName.Value = "";
        }

        private void CopyFromSkeletonChanged(GameSkeleton newValue)
        {
            _remappingInformaton = null;
            CreateBoneOverview(_copyTo.Skeleton);
            HightlightSelectedBones(null);

            _config = null;
            AnimationSettings.UseScaledSkeletonName.Value = false;
            AnimationSettings.ScaledSkeletonName.Value = "";
        }

        public void OpenMappingWindow()
        {
            if (_copyTo.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Source or target skeleton not selected", "Error");
                return;
            }

            var targetSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyTo.SkeletonName.Value);
            var sourceSkeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyFrom.SkeletonName.Value);

            if (_config == null)
            {
                _config = new RemappedAnimatedBoneConfiguration();
                _config.MeshSkeletonName = _copyTo.SkeletonName.Value;
                _config.MeshBones = AnimatedBoneHelper.CreateFromSkeleton(targetSkeleton);
                
                _config.ParnetModelSkeletonName = _copyFrom.SkeletonName.Value;
                _config.ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(sourceSkeleton);
            }

            var window = new BoneMappingWindow(new BoneMappingViewModel(_config));
            if (window.ShowDialog() == true)
            {
                _remappingInformaton = AnimatedBoneHelper.BuildRemappingList(_config.MeshBones.First());
                UpdateAnimation();
                UpdateBonesAfterMapping(Bones);
            }
        }

        void UpdateBonesAfterMapping(IEnumerable<SkeletonBoneNode> bones)
        {
            foreach (var bone in bones)
            {
                var mapping = _remappingInformaton.FirstOrDefault(x => x.OriginalValue == bone.BoneIndex.Value);
                bone.HasMapping.Value = mapping != null;
                UpdateBonesAfterMapping(bone.Children);
            }
        }

        public void ClearRelativeSelectedBone()
        {
            if(SelectedBone != null)
                SelectedBone.SelectedRelativeBone = null;
        }

        public void UpdateAnimation()
        {
            if (CanUpdateAnimation(true))
            {
                var newAnimationClip = UpdateAnimation(_copyFrom.AnimationClip);
                Generated.SetAnimationClip(newAnimationClip, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
            }
        }

        bool CanUpdateAnimation(bool requireAnimation)
        {
            if (_remappingInformaton == null)
            {
                MessageBox.Show("No mapping created?", "Error", MessageBoxButton.OK);
                return false;
            }

            if (_copyTo.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Missing a skeleton?", "Error", MessageBoxButton.OK);
                return false;
            }

            if (_copyFrom.AnimationClip == null && requireAnimation)
            {
                MessageBox.Show("No animation to copy selected", "Error", MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        AnimationClip UpdateAnimation(AnimationClip clip)
        {
            var service = new AnimationRemapperService(AnimationSettings, _remappingInformaton, Bones);
            var newClip = service.ReMapAnimation(_copyFrom.Skeleton, _copyTo.Skeleton, clip);
            return newClip;
        }

        public void OpenBatchProcessDialog()
        {
            if (!CanUpdateAnimation(false))
                return;

            // Find all animations for skeleton
            var copyFromAnims = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(_copyFrom.Skeleton.SkeletonName);

            var items = copyFromAnims.Select(x => new SelectionListViewModel<SkeletonAnimationLookUpHelper.AnimationReference>.Item()
            {
                IsChecked = new NotifyAttr<bool>(!x.AnimationFile.Contains("tech", StringComparison.InvariantCultureIgnoreCase)),
                DisplayName = x.AnimationFile,
                ItemValue = x
            }).ToList();

            var window = SelectionListWindow.ShowDialog("Select animations:", items);
            if (window.Result)
            {
                using (var waitCursor = new WaitCursor())
                {
                    var index = 1;
                    var numItemsToProcess = items.Count(x => x.IsChecked.Value);
                    foreach (var item in items)
                    {
                        if (item.IsChecked.Value)
                        {
                            var file = _pfs.FindFile(item.ItemValue.AnimationFile, item.ItemValue.Container) as PackFile;
                            var animFile = AnimationFile.Create(file.DataSource.ReadDataAsChunk());
                            var clip = new AnimationClip(animFile);

                            _logger.Here().Information($"Processing animation {index} / {numItemsToProcess} - {item.DisplayName}");

                            var updatedClip = UpdateAnimation(clip);
                            SaveAnimation(updatedClip, item.ItemValue.AnimationFile, false);
                            index++;
                        }
                       
                    }
                }
            }
        }

        public void SaveAnimation()
        {
            if (Generated.AnimationClip == null || Generated.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Can not save, as no animation has been generated. Press the Apply button first", "Error", MessageBoxButton.OK);
                return;
            }

            SaveAnimation(Generated.AnimationClip, Generated.AnimationName.Value.AnimationFile);
        }

        void SaveAnimation(AnimationClip clip, string animationName, bool prompOnOverride = true)
        {
            var originalPath = animationName;
            var orgSkeleton = _copyFrom.Skeleton.SkeletonName;
            var newSkeleton = _copyTo.Skeleton.SkeletonName;
            var newPath = originalPath.Replace(orgSkeleton, newSkeleton);

            var animFile = clip.ConvertToFileFormat(_copyTo.Skeleton);
            if (AnimationSettings.UseScaledSkeletonName.Value)
                animFile.Header.SkeletonName = AnimationSettings.ScaledSkeletonName.Value;

            var currentFileName = Path.GetFileName(newPath);
            newPath = newPath.Replace(currentFileName, "cust_" + currentFileName);

            SaveHelper.Save(_pfs, newPath, null, AnimationFile.GetBytes(animFile), prompOnOverride);
        }

        public void ClearAllSettings()
        {
            if(MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                CreateBoneOverview(_copyTo.Skeleton);
        }


        public void UseTargetAsSource()
        {
            if (MessageBox.Show("Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                AnimationSettings.UseScaledSkeletonName.Value = false;
                AnimationSettings.ScaledSkeletonName.Value = "";
                _copyFrom.CopyMeshFromOther(_copyTo);
            }
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone = null;
            Bones.Clear();
            FlatBoneList.Clear();
            FlatBoneList.Add(null);

            if (skeleton == null)
                return;
            for (int i = 0; i<skeleton.BoneCount; i++)
            {
                SkeletonBoneNode newBone = null;
                var parentBoneId = skeleton.GetParentBone(i);
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

        public void ExportScaledMesh()
        {
            var commandExecutor = _componentManager.GetComponent<CommandExecutor>();

            var modelNodes = SceneNodeHelper.GetChildrenOfType<Rmv2ModelNode>(Generated.MainNode, (x) => x.IsVisible)
                .Where(x => x.IsVisible)
                .ToList();

            if(modelNodes.Count == 0)
            {
                MessageBox.Show("Can not save, as there is no mesh", "Error", MessageBoxButton.OK);
                return;
            }

            AnimationSettings.UseScaledSkeletonName.Value = true;
            var scaleStr = "s" + AnimationSettings.Scale.Value.ToString().Replace(".", "").Replace(",", "");
            var newSkeletonName = Generated.Skeleton.SkeletonName + "_" + scaleStr;
            var originalSkeletonName = modelNodes.First().Model.Header.SkeletonName;
            AnimationSettings.ScaledSkeletonName.Value = newSkeletonName;

            // Create scaled animation
            var scaleAnimClip = new AnimationClip();
            scaleAnimClip.DynamicFrames.Add(new AnimationClip.KeyFrame());
            scaleAnimClip.DynamicFrames.Add(new AnimationClip.KeyFrame());
            scaleAnimClip.PlayTimeInSec = 2.0f / 20.0f;
            for (int i = 0; i < Generated.Skeleton.BoneCount; i++)
            {
                scaleAnimClip.DynamicFrames[0].Position.Add(Generated.Skeleton.Translation[i]);
                scaleAnimClip.DynamicFrames[0].Rotation.Add(Generated.Skeleton.Rotation[i]);
                scaleAnimClip.DynamicFrames[0].Scale.Add(Vector3.One);

                scaleAnimClip.DynamicFrames[1].Position.Add(Generated.Skeleton.Translation[i]);
                scaleAnimClip.DynamicFrames[1].Rotation.Add(Generated.Skeleton.Rotation[i]);
                scaleAnimClip.DynamicFrames[1].Scale.Add(Vector3.One);

                scaleAnimClip.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
                scaleAnimClip.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(i));
            }

            scaleAnimClip.DynamicFrames[0].Scale[0] = new Vector3((float)AnimationSettings.Scale.Value);
            scaleAnimClip.DynamicFrames[1].Scale[0] = new Vector3((float)AnimationSettings.Scale.Value);

            // Create a skeleton from the scaled animation
            var skeletonAnimFile = scaleAnimClip.ConvertToFileFormat(Generated.Skeleton);
            skeletonAnimFile.Header.SkeletonName = newSkeletonName;

            var skeletonBytes = AnimationFile.GetBytes(skeletonAnimFile);
            SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".anim", null, skeletonBytes);

            var animationFrame = AnimationSampler.Sample(0, 0, Generated.Skeleton, scaleAnimClip);

            int numCommandsToUndo = 0;
            foreach (var model in modelNodes)
            {
                var header = model.Model.Header;
                header.SkeletonName = newSkeletonName;
                model.Model.Header = header;

                var meshList = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(model);
                var cmd = new CreateAnimatedMeshPoseCommand(meshList, animationFrame, false);
                commandExecutor.ExecuteCommand(cmd, true);
  
                numCommandsToUndo++;
            }

            var meshName = Path.GetFileNameWithoutExtension(_copyTo.MeshName.Value);
            var newMeshName = meshName + "_" + scaleStr + ".rigid_model_v2";
            var bytes = MeshSaverService.Save(true, modelNodes, Generated.Skeleton);
            SaveHelper.Save(_pfs, newMeshName, null, bytes);

            // Undo the mesh transform
            for(int i = 0; i < numCommandsToUndo; i++)
                commandExecutor.Undo(); 

            // Reset the skeleton
            foreach (var model in modelNodes)
            {
                var header = model.Model.Header;
                header.SkeletonName = originalSkeletonName;
                model.Model.Header = header;
            }
        }
    }
}

