using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using static Shared.Core.Misc.NotifyPropertyChangedImpl;

namespace Shared.Ui.BaseDialogs.MathViews
{
    public class Vector3ViewModel : NotifyPropertyChangedImpl
    {
        virtual public event ValueChangedDelegate<Vector3ViewModel> OnValueChanged;

        public Vector3ViewModel(double x = 0, double y = 0, double z = 0)
        {
            X.Value = x;
            Y.Value = y;
            Z.Value = z;

            X.PropertyChanged += PropertyChanged;
            Y.PropertyChanged += PropertyChanged;
            Z.PropertyChanged += PropertyChanged;
        }

        public Vector3ViewModel(double value = 0)
        {
            X.Value = value;
            Y.Value = value;
            Z.Value = value;

            X.PropertyChanged += PropertyChanged;
            Y.PropertyChanged += PropertyChanged;
            Z.PropertyChanged += PropertyChanged;
        }

        public Vector3ViewModel(Vector3 vector)
        {
            X.Value = vector.X;
            Y.Value = vector.Y;
            Z.Value = vector.Z;

            X.PropertyChanged += PropertyChanged;
            Y.PropertyChanged += PropertyChanged;
            Z.PropertyChanged += PropertyChanged;
        }

        private void PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DisableCallbacks == false)
                OnValueChanged?.Invoke(this);
        }

        DoubleViewModel _x = new DoubleViewModel();
        public DoubleViewModel X
        {
            get { return _x; }
            set { SetAndNotify(ref _x, value); OnValueChanged?.Invoke(this); }
        }

        DoubleViewModel _y = new DoubleViewModel();
        public DoubleViewModel Y
        {
            get { return _y; }
            set { SetAndNotify(ref _y, value); OnValueChanged?.Invoke(this); }
        }

        DoubleViewModel _z = new DoubleViewModel();
        public DoubleViewModel Z
        {
            get { return _z; }
            set { SetAndNotify(ref _z, value); OnValueChanged?.Invoke(this); }
        }

        public void Set(float x, float y, float z)
        {
            X.Value = x;
            Y.Value = y;
            Z.Value = z;
        }

        public void Set(Vector3 value)
        {
            X.Value = value.X;
            Y.Value = value.Y;
            Z.Value = value.Z;
        }

        public void Set(float value)
        {
            X.Value = value;
            Y.Value = value;
            Z.Value = value;
        }

        public void Clear()
        {
            Set(0, 0, 0);
        }

        public Vector3 GetAsVector3()
        {
            return new Vector3((float)X.Value, (float)Y.Value, (float)Z.Value);
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Z}";
        }
    }
}
