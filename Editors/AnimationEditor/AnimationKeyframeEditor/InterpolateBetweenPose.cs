using System.Windows.Forms;
using Editors.AnimationVisualEditors.AnimationKeyframeEditor;
using GameWorld.Core.Commands.Bone;

namespace AnimationEditor.AnimationKeyframeEditor
{
    internal class InterpolateBetweenPose
    {
        private readonly AnimationKeyframeEditorViewModel _parent;
        
        public int KeyFrameNrA {
            get => _keyframeNrA;
            private set
            {
                _keyframeNrA = value;
            }
        }
        private int _keyframeNrA;

        public int KeyFrameNrB
        {
            get => _keyframeNrB;
            private set
            {
                _keyframeNrB = value;
            }
        }
        private int _keyframeNrB;

        public InterpolateBetweenPose(AnimationKeyframeEditorViewModel parent) 
        {
            _parent = parent;
        }

        public void Reset()
        {
            _keyframeNrA = -1;
            _keyframeNrB = -1;
            _parent.SelectedFrameAInterpolation.Value = "Not set";
            _parent.SelectedFrameBInterpolation.Value = "Not set";
        }

        private bool Check()
        {
            var noKeyframeSelected = false;

            if (_keyframeNrA == -1) noKeyframeSelected = true;
            if (_keyframeNrB == -1) noKeyframeSelected = true;
            
            if(noKeyframeSelected)
            {
                MessageBox.Show("keyframe A and B must be selected to use this tool", "error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            if (_parent.Rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }


            return true;
        }

        private bool CheckForBones()
        {
            if (_parent.InterpolationOnlyOnSelectedBones)
            {
                if (_parent.GetSelectedBones() == null || _parent.GetSelectedBones().Count == 0)
                {
                    _parent.InterpolationOnlyOnSelectedBones = false;
                    MessageBox.Show("no bones were selected!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        public void  ApplySingleFrame()
        {
            if(_parent.InterpolationOnlyOnSelectedBones)
                ApplyOnCurrentFrameSelectedBones();
            else
                ApplyOnCurrentFrame();
        }

        public void ApplyMultipleFrame()
        {
            if (_parent.InterpolationOnlyOnSelectedBones)
                ApplyAcrossFramesSelectedBones();
            else
                ApplyAcrossFrames();
        }

        private void ApplyOnCurrentFrame()
        {
            if (!Check()) return;
            _parent.CommandFactory.Create<InterpolateFramesBoneCommand>().Configure(x => x.Configure(
                _parent.Rider.AnimationClip,
                _parent.Rider.Player.CurrentFrame,
                _keyframeNrA,
                _keyframeNrB,
                _parent.Skeleton,
                _parent.InterpolationValue.Value,
                _parent.PastePosition.Value,
                _parent.PasteRotation.Value,
                _parent.PasteScale.Value)).BuildAndExecute();
        }

        private void ApplyOnCurrentFrameSelectedBones()
        {
            if (!CheckForBones()) return;
            if (!Check()) return;

            _parent.CommandFactory.Create<InterpolateFramesSelectedBonesBoneCommand>().Configure(x => x.Configure(
            _parent.Rider.AnimationClip,
            _parent.Rider.Player.CurrentFrame,
            _keyframeNrA,
            _keyframeNrB,
            _parent.Skeleton,
            _parent.InterpolationValue.Value,
            _parent.GetSelectedBones(),
            _parent.PastePosition.Value,
            _parent.PasteRotation.Value,
            _parent.PasteScale.Value)).BuildAndExecute();
        }

        private void ApplyAcrossFrames()
        {
            if (!Check()) return;

        }

        private void ApplyAcrossFramesSelectedBones()
        {
            if (!CheckForBones()) return;
            if (!Check()) return;

        }

        public void SelectFrameA(int frameNrA)
        {
            if (_parent.Rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _keyframeNrA = frameNrA;
        }

        public void SelectFrameB(int frameNrB)
        {
            if (_parent.Rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _keyframeNrB = frameNrB;
        }        


    }
}
