using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonControls.MathViews
{
    public class Vector3ViewModel : NotifyPropertyChangedImpl
    {
        virtual public event ValueChangedDelegate<Vector3ViewModel> OnValueChanged;

        public Vector3ViewModel(double x = 0, double y = 0, double z = 0)
        {
            X.Value = x;
            Y.Value = y;
            Z.Value = z;
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

        public void Set(float value)
        {
            X.Value = value;
            Y.Value = value;
            Z.Value = value;
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Z}";
        }
    }
}
