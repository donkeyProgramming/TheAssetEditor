// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Forms;
using Editors.AnimationVisualEditors.AnimationKeyframeEditor;
using GameWorld.Core.Commands.Bone;

namespace AnimationEditor.AnimationKeyframeEditor
{
    internal class CopyPastePose
    {
        private readonly AnimationKeyframeEditorViewModel _parent;
        private int _frameNrToCopy = 0;
        public CopyPastePose(AnimationKeyframeEditorViewModel parent)
        {
            _parent = parent;
        }

        public void CopyCurrentPose()
        {
            if (_parent.Rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _parent.Pause();
            _frameNrToCopy = _parent.Rider.Player.CurrentFrame;
            if (_parent.IncrementFrameAfterCopyOperation.Value)
            {
                _parent.NextFrame();
            }
        }

        public void PasteIntoCurrentFrame()
        {
            var currentFrame = _parent.Rider.Player.CurrentFrame;
            if (_parent.Rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _parent.Pause();
            _parent.CommandFactory.Create<PasteWholeTransformBoneCommand>().Configure(x => x.Configure(
                _parent.Rider.AnimationClip.DynamicFrames[_frameNrToCopy].Clone(),
                _parent.Rider.AnimationClip, currentFrame,
                _parent.PastePosition.Value, _parent.PasteRotation.Value, _parent.PasteScale.Value)).BuildAndExecute();

            if (_parent.IncrementFrameAfterCopyOperation.Value)
            {
                _parent.NextFrame();
            }
            _parent.IsDirty.Value = _parent.CommandExecutor.CanUndo();
        }


        public void PasteIntoSelectedCurrentNode()
        {
            var currentFrame = _parent.Rider.Player.CurrentFrame;
            if (_parent.Rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_parent.GetSelectedBones() == null || _parent.GetSelectedBones().Count == 0)
            {
                MessageBox.Show("no bones were selected", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            _parent.Pause();
            _parent.CommandFactory.Create<PasteIntoSelectedBonesTransformBoneCommand>().Configure(x => x.Configure(
                _parent.Rider.AnimationClip.DynamicFrames[_frameNrToCopy].Clone(),
                _parent.Rider.AnimationClip, currentFrame, _parent.GetSelectedBones(),
                _parent.PastePosition.Value, _parent.PasteRotation.Value, _parent.PasteScale.Value)).BuildAndExecute();

            if (_parent.IncrementFrameAfterCopyOperation.Value)
            {
                _parent.NextFrame();
            }
            _parent.IsDirty.Value = _parent.CommandExecutor.CanUndo();
        }

        public void PastePreviousEditedNodesIntoCurrentPose()
        {
            var currentFrame = _parent.Rider.Player.CurrentFrame;
            if (_parent.Rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_parent.GetModifiedBones()  == null || _parent.GetModifiedBones().Count == 0)
            {
                MessageBox.Show("no bones were modified", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _parent.Pause();
            _parent.CommandFactory.Create<PasteIntoSelectedBonesTransformBoneCommand>().Configure(x => x.Configure(
                _parent.Rider.AnimationClip.DynamicFrames[_parent.GetModifiedFrameNr()].Clone(),
                _parent.Rider.AnimationClip, currentFrame, _parent.GetModifiedBones(),
                _parent.PastePosition.Value, _parent.PasteRotation.Value, _parent.PasteScale.Value)).BuildAndExecute();

            if (_parent.IncrementFrameAfterCopyOperation.Value)
            {
                _parent.NextFrame();
            }
            _parent.IsDirty.Value = _parent.CommandExecutor.CanUndo();
        }
    }
}
