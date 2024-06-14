// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands.Bone.Clipboard;

namespace AnimationEditor.AnimationKeyframeEditor
{
    internal class AnimationCliboardCreator
    {
        public static BoneTransformClipboardData CreateFrameClipboard(GameSkeleton skeleton, AnimationClip currentFrames, int startFrame, int endFrame)
        {
            var output = new BoneTransformClipboardData();

            output.SkeletonName = skeleton.SkeletonName;

            for (int frameNr = startFrame; frameNr < endFrame; frameNr++)
            {

                var frames = new BoneTransformClipboardData.Frame();
                for (int boneId = 0; boneId < currentFrames.DynamicFrames[frameNr].Position.Count; boneId++)
                {
                    var transform = currentFrames.DynamicFrames[frameNr];
                    var boneName = skeleton.GetBoneNameByIndex(boneId);

                    frames.BoneIdToPosition.Add(boneName, transform.Position[boneId]);
                    frames.BoneIdToQuaternion.Add(boneName, transform.Rotation[boneId]);
                    frames.BoneIdToScale.Add(boneName, transform.Scale[boneId]);
                }
                output.Frames.Add(frameNr, frames);
            }

            return output;
        }
    }
}
