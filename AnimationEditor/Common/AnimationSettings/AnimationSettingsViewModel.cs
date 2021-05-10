using Common;
using CommonControls.MathViews;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimationEditor.Common.AnimationSettings
{
    public class AnimationSettingsViewModel : NotifyPropertyChangedImpl
    {
        bool _fitAnimation = true;
        public bool FitAnimation
        {
            get { return _fitAnimation; }
            set { SetAndNotify(ref _fitAnimation, value); }
        }

        DoubleViewModel _loopCounter = new DoubleViewModel(1);
        public DoubleViewModel LoopCounter
        {
            get { return _loopCounter; }
            set { SetAndNotify(ref _loopCounter, value); }
        }

        bool _applyOffsets = false;
        public bool ApplyOffsets
        {
            get { return _applyOffsets; }
            set { SetAndNotify(ref _applyOffsets, value); }
        }

        Vector3ViewModel _translation = new Vector3ViewModel();
        public Vector3ViewModel Translation
        {
            get { return _translation; }
            set { SetAndNotify(ref _translation, value); }
        }

        Vector3ViewModel _rotation = new Vector3ViewModel();
        public Vector3ViewModel Rotation
        {
            get { return _rotation; }
            set { SetAndNotify(ref _rotation, value); }
        }

        DoubleViewModel _scale = new DoubleViewModel(1);
        public DoubleViewModel Scale
        {
            get { return _scale; }
            set { SetAndNotify(ref _scale, value); }
        }
    }
}
