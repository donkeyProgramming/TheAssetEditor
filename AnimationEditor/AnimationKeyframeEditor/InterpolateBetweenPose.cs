// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using View3D.Animation;

namespace AnimationEditor.AnimationKeyframeEditor
{
    internal class InterpolateBetweenPose
    {
        private readonly AnimationKeyframeEditorViewModel _parent;
        private AnimationClip.KeyFrame _keyframeA;
        private AnimationClip.KeyFrame _keyframeB;
        
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


        private int _currentFrameNr;

        public InterpolateBetweenPose(AnimationKeyframeEditorViewModel parent) 
        {
            _parent = parent;
            _currentFrameNr = -1;
        }

        private bool Check()
        {
            var noKeyframeSelected = false;

            if (_keyframeA == null) noKeyframeSelected = true;
            if (_keyframeB == null) noKeyframeSelected = true;
            
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
            Console.WriteLine("interpolation preview goes here, slider value " + _parent.InterpolationValue);

        }

        private void ApplyOnCurrentFrameSelectedBones()
        {
            if (!CheckForBones()) return;
            if (!Check()) return;

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

            _keyframeA = _parent.Rider.AnimationClip.DynamicFrames[frameNrA].Clone();
            _keyframeNrA = frameNrA;
        }

        public void SelectFrameB(int frameNrB)
        {
            if (_parent.Rider.AnimationClip == null)
            {
                MessageBox.Show("animation not loaded!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _keyframeB = _parent.Rider.AnimationClip.DynamicFrames[frameNrB].Clone();
            _keyframeNrB = frameNrB;
        }        


    }
}
