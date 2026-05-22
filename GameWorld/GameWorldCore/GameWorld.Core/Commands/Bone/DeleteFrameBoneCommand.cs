// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Commands.Bone
{
    public class DeleteFrameBoneCommand : ICommand
    {
        public string HintText => "duplicate/add frame";

        public bool IsMutation => true;

        List<AnimationClip.KeyFrame> _backupFrames = new();
        AnimationClip _animation;
        int _frameToInsert;

        public void Configure(AnimationClip animation, int frameToInsert)
        {
            _animation = animation;
            _frameToInsert = frameToInsert;

            foreach (var frame in _animation.DynamicFrames)
            {
                _backupFrames.Add(frame.Clone());
            }
        }

        public void Execute()
        {
            _animation.DynamicFrames.RemoveAt(_frameToInsert);
        }

        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
