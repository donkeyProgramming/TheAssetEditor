using Shared.Core.Misc;
using Shared.Ui.BaseDialogs.MathViews;

namespace AnimationEditor.MountAnimationCreator.ViewModels
{
    public class AnimationSettingsViewModel : NotifyPropertyChangedImpl
    {
        public event ValueChangedDelegate SettingsChanged;

        public AnimationSettingsViewModel()
        {
            LoopCounter.OnValueChanged += ValueChanged;
            Scale.OnValueChanged += ValueChanged;
            Translation.OnValueChanged += ValueChanged;
            Rotation.OnValueChanged += ValueChanged;
        }
        void ValueChanged(double v) => SettingsChanged?.Invoke();
        void ValueChanged(Vector3ViewModel v) => SettingsChanged?.Invoke();

        bool _fitAnimation = true;
        public bool FitAnimation
        {
            get { return _fitAnimation; }
            set { SetAndNotify(ref _fitAnimation, value); SettingsChanged?.Invoke(); }
        }

        DoubleViewModel _loopCounter = new DoubleViewModel(1);
        public DoubleViewModel LoopCounter
        {
            get { return _loopCounter; }
            set { SetAndNotify(ref _loopCounter, value); SettingsChanged?.Invoke(); }
        }

        Vector3ViewModel _translation = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel Translation
        {
            get { return _translation; }
            set { SetAndNotify(ref _translation, value); SettingsChanged?.Invoke(); }
        }

        Vector3ViewModel _rotation = new Vector3ViewModel(0, 0, 0);
        public Vector3ViewModel Rotation
        {
            get { return _rotation; }
            set { SetAndNotify(ref _rotation, value); SettingsChanged?.Invoke(); }
        }

        DoubleViewModel _scale = new DoubleViewModel(1);
        public DoubleViewModel Scale
        {
            get { return _scale; }
            set { SetAndNotify(ref _scale, value); SettingsChanged?.Invoke(); }
        }

        bool _keepRiderRotation = true;
        public bool KeepRiderRotation
        {
            get { return _keepRiderRotation; }
            set { SetAndNotify(ref _keepRiderRotation, value); SettingsChanged?.Invoke(); }
        }

        bool _isRootNodeAnimation = false;
        public bool IsRootNodeAnimation
        {
            get { return _isRootNodeAnimation; }
            set { SetAndNotify(ref _isRootNodeAnimation, value); SettingsChanged?.Invoke(); }
        }
    }
}
