// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using View3D.Animation;

namespace View3D.Commands.Bone
{
    public class DuplicateDeleteFrameBoneCommand : ICommand
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

        public void RemoveFrame()
        {
            _animation.DynamicFrames.RemoveAt(_frameToInsert);
        }

        public void DuplicateFrame()
        {
            var clone = _animation.DynamicFrames[_frameToInsert].Clone();
            _animation.DynamicFrames.Insert(_frameToInsert, clone);
        }

        public void Execute()
        {
            
        }

        public void Undo()
        {
            _animation.DynamicFrames = _backupFrames;
        }
    }
}
