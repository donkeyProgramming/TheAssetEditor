using System;
using GameWorld.Core.Animation;

namespace Editors.AnimatioReTarget.Editor
{
    class ExportHelper
    {
        public static void ExportMappedSkeleton()
        {

            throw new NotImplementedException("Todo");
            //    var skeletonFile = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(_pfs, _copyFrom.Skeleton.SkeletonName);
            //    var clip = new AnimationClip(skeletonFile);
            //
            //    var mappedSkeleton = UpdateAnimation(clip);
            //    var mappedSkeletonFile = mappedSkeleton.ConvertToFileFormat(Generated.Skeleton);
            //
            //    var newSkeletonName = Generated.Skeleton.SkeletonName + "_generated";
            //    AnimationSettings.UseScaledSkeletonName.Value = true;
            //    AnimationSettings.ScaledSkeletonName.Value = newSkeletonName + ".anim";
            //
            //
            //    var skeletonBytes = AnimationFile.ConvertToBytes(mappedSkeletonFile);
            //    SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".anim", null, skeletonBytes);
            //
            //
            //    //Save inv matrix file
            //    var newSkeleton = new GameSkeleton(mappedSkeletonFile, null);
            //    var invMatrixFile = newSkeleton.CreateInvMatrixFile();
            //    var invMatrixBytes = invMatrixFile.GetBytes();
            //    SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".bone_inv_trans_mats", null, invMatrixBytes);
            //
            //    SaveMeshWithNewSkeleton(mappedSkeleton, "changed");
        }

        public static void ExportScaledMesh()
        {
            throw new NotImplementedException("Todo");
            //var commandExecutor = _componentManager.GetComponent<CommandExecutor>();
            //
            //var modelNodes = SceneNodeHelper.GetChildrenOfType<Rmv2ModelNode>(Generated.MainNode, (x) => x.IsVisible)
            //    .Where(x => x.IsVisible)
            //    .ToList();
            //
            //if (modelNodes.Count == 0)
            //{
            //    MessageBox.Show("Can not save, as there is no mesh", "Error", MessageBoxButton.OK);
            //    return;
            //}
            //
            //AnimationSettings.UseScaledSkeletonName.Value = true;
            //var scaleStr = "s" + AnimationSettings.Scale.Value.ToString().Replace(".", "").Replace(",", "");
            //var newSkeletonName = Generated.Skeleton.SkeletonName + "_" + scaleStr;
            //var originalSkeletonName = modelNodes.First().Model.Header.SkeletonName;
            //AnimationSettings.ScaledSkeletonName.Value = newSkeletonName;
            //
            //// Create scaled animation
            //var scaleAnimClip = new AnimationClip();
            //scaleAnimClip.DynamicFrames.Add(new AnimationClip.KeyFrame());
            //scaleAnimClip.DynamicFrames.Add(new AnimationClip.KeyFrame());
            //scaleAnimClip.PlayTimeInSec = 2.0f / 20.0f;
            //for (int i = 0; i < Generated.Skeleton.BoneCount; i++)
            //{
            //    scaleAnimClip.DynamicFrames[0].Position.Add(Generated.Skeleton.Translation[i]);
            //    scaleAnimClip.DynamicFrames[0].Rotation.Add(Generated.Skeleton.Rotation[i]);
            //    scaleAnimClip.DynamicFrames[0].Scale.Add(Vector3.One);
            //
            //    scaleAnimClip.DynamicFrames[1].Position.Add(Generated.Skeleton.Translation[i]);
            //    scaleAnimClip.DynamicFrames[1].Rotation.Add(Generated.Skeleton.Rotation[i]);
            //    scaleAnimClip.DynamicFrames[1].Scale.Add(Vector3.One);
            //}
            //
            //scaleAnimClip.DynamicFrames[0].Scale[0] = new Vector3((float)AnimationSettings.Scale.Value);
            //scaleAnimClip.DynamicFrames[1].Scale[0] = new Vector3((float)AnimationSettings.Scale.Value);
            //
            //// Create a skeleton from the scaled animation
            //
            //SaveMeshWithNewSkeleton(scaleAnimClip, scaleStr);
        }

        void SaveMeshWithNewSkeleton(AnimationClip newSkeletonClip, string savePostFix)
        {
            throw new NotImplementedException("Todo");
            //var commandExecutor = _componentManager.GetComponent<CommandExecutor>();
            //
            //var modelNodes = SceneNodeHelper.GetChildrenOfType<Rmv2ModelNode>(Generated.MainNode, (x) => x.IsVisible)
            //    .Where(x => x.IsVisible)
            //    .ToList();
            //
            //if (modelNodes.Count == 0)
            //{
            //    MessageBox.Show("Can not save, as there is no mesh", "Error", MessageBoxButton.OK);
            //    return;
            //}
            //
            //// Create a skeleton from the scaled animation
            //var newSkeletonName = Generated.Skeleton.SkeletonName + "_" + savePostFix;
            //var skeletonAnimFile = newSkeletonClip.ConvertToFileFormat(Generated.Skeleton);
            //skeletonAnimFile.Header.SkeletonName = newSkeletonName;
            //
            //AnimationSettings.UseScaledSkeletonName.Value = true;
            //AnimationSettings.ScaledSkeletonName.Value = newSkeletonName;
            //
            //var skeletonBytes = AnimationFile.ConvertToBytes(skeletonAnimFile);
            //SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".anim", null, skeletonBytes);
            //
            ////Save inv matrix file
            //var newSkeleton = new GameSkeleton(skeletonAnimFile, null);
            //var invMatrixFile = newSkeleton.CreateInvMatrixFile();
            //var invMatrixBytes = invMatrixFile.GetBytes();
            //SaveHelper.Save(_pfs, @"animations\skeletons\" + newSkeletonName + ".bone_inv_trans_mats", null, invMatrixBytes);
            //
            //var animationFrame = AnimationSampler.Sample(0, 0, Generated.Skeleton, newSkeletonClip);
            //
            //int numCommandsToUndo = 0;
            //var originalSkeletonName = modelNodes.First().Model.Header.SkeletonName;
            //foreach (var model in modelNodes)
            //{
            //    var header = model.Model.Header;
            //    header.SkeletonName = newSkeletonName;
            //    model.Model.Header = header;
            //
            //    var meshList = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(model);
            //    var cmd = new CreateAnimatedMeshPoseCommand(meshList, animationFrame, false);
            //    commandExecutor.ExecuteCommand(cmd, true);
            //
            //    numCommandsToUndo++;
            //}
            //
            //var meshName = Path.GetFileNameWithoutExtension(_copyTo.MeshName.Value);
            //var newMeshName = meshName + "_" + savePostFix + ".rigid_model_v2";
            //var bytes = SceneSaverService.Save(true, modelNodes, newSkeleton, RmvVersionEnum.RMV2_V7);
            //
            //SaveHelper.Save(_pfs, newMeshName, null, bytes);
            //
            //// Undo the mesh transform
            //for (int i = 0; i < numCommandsToUndo; i++)
            //    commandExecutor.Undo();
            //
            //// Reset the skeleton
            //foreach (var model in modelNodes)
            //{
            //    var header = model.Model.Header;
            //    header.SkeletonName = originalSkeletonName;
            //    model.Model.Header = header;
            //}
        }
    }
}
