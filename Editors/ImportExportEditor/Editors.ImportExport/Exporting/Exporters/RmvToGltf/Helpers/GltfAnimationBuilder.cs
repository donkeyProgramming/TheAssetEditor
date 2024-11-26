using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using Editors.ImportExport.Common;
using GameWorld.Core.Animation;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using SharpGLTF.Schema2;
using SysNum = System.Numerics;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public class GltfAnimationBuilder
    {
        private readonly PackFileService _packFileService;


        public GltfAnimationBuilder(PackFileService packFileServoce)
        {
            _packFileService = packFileServoce;
        }


        public ProcessedGltfSkeleton Build(string skeletonName, RmvToGltfExporterSettings settings, ProcessedGltfSkeleton gltfSkeleton, ModelRoot outputScene)
        {
            if (settings.ExportAnimations == false)
                return null;

            // TODO: REMOVE THIS:
            ///////////////   TESTING   /////////////////////
            /// lOAD WALKING ANIMATION TO TEST
            //var foundAnimFile = _packFileService.FindFile(@"animations\battle\humanoid01\2handed_hammer\combat_idles\hu1_2hh_combat_idle_02.anim");
            //settings.InputAnimationFiles.Add(foundAnimFile);

            var testAnimList = AnimationCommon.FetchAnimationsFromSkeletonId("humanoid01", _packFileService);

            foreach (var animPackFile in testAnimList)
            {
                settings.InputAnimationFiles.Add(animPackFile);
            }

            const int maxAnimations = 50;
            settings.InputAnimationFiles.RemoveRange(maxAnimations, settings.InputAnimationFiles.Count - maxAnimations - 1);
            // TODO: REMOVE
            //settings.InputAnimationFiles.RemoveRange(7, settings.InputAnimationFiles.Count-8);

            ///////////////   END: TESTING   ///////////////////

            var animationSkeletonPackFile = AnimationCommon.FetchSkeletonFileFromId(skeletonName, _packFileService);
            var animationSkeleton = AnimationFile.Create(animationSkeletonPackFile);
            foreach (var animation in settings.InputAnimationFiles)
            {
                var animationToExport = AnimationFile.Create(animation);                
                CreateFromTWAnim(animation.Name, gltfSkeleton, animationSkeleton, animationToExport, outputScene, settings);
            }

            return gltfSkeleton;
        }

        private void CreateFromTWAnim(string animationName, ProcessedGltfSkeleton gltfSkeleton, AnimationFile skeletonAnimFile, AnimationFile animationToExport, ModelRoot modelRoot, RmvToGltfExporterSettings settings)
        {
            var doMirror = settings.MirrorMesh;
            var gameSkeleton = new GameSkeleton(skeletonAnimFile, null);
            var animationClip = new AnimationClip(animationToExport, gameSkeleton);

            var secondsPerFrame = animationClip.PlayTimeInSec / animationClip.DynamicFrames.Count;            

            var gltfAnimation = modelRoot.CreateAnimation(animationName);

            for (var boneIndex = 0; boneIndex < animationClip.AnimationBoneCount; boneIndex++)
            {
                var translationKeyFrames = new Dictionary<float, SysNum.Vector3>();
                var rotationKeyFrames = new Dictionary<float, SysNum.Quaternion>();
                var scaleKeyFrames = new Dictionary<float, SysNum.Vector3>();

                // populate the bone track containers with the key frames from the .ANIM animation file
                for (var frameIndex = 0; frameIndex < animationClip.DynamicFrames.Count; frameIndex++)
                {
                    translationKeyFrames.Add(secondsPerFrame * (float)frameIndex, VecConv.GetSys(GlobalSceneTransforms.FlipVector(animationClip.DynamicFrames[frameIndex].Position[boneIndex], doMirror)));
                    rotationKeyFrames.Add(secondsPerFrame * (float)frameIndex, VecConv.GetSys(GlobalSceneTransforms.FlipQuaternion(animationClip.DynamicFrames[frameIndex].Rotation[boneIndex], doMirror)));
                    scaleKeyFrames.Add(secondsPerFrame * (float)frameIndex, new SysNum.Vector3(1, 1, 1));
                }

                // add the transformations
                var boneNode = gltfSkeleton.Data[boneIndex].Item1;
                gltfAnimation.CreateRotationChannel(boneNode, rotationKeyFrames);
                gltfAnimation.CreateTranslationChannel(boneNode, translationKeyFrames);
                gltfAnimation.CreateScaleChannel(boneNode, scaleKeyFrames);
            }
        }
    }
}
