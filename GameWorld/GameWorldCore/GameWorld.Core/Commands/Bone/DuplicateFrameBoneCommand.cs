// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;

namespace GameWorld.Core.Commands.Bone
{
    public class DuplicateFrameBoneCommand : ICommand
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
            var clone = _animation.DynamicFrames[_frameToInsert].Clone();
            _animation.DynamicFrames.Insert(_frameToInsert, clone);
        }

        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
