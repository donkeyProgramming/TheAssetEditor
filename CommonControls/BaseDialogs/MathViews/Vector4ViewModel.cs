// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommonControls.MathViews
{
    public class Vector4ViewModel : Vector3ViewModel
    {
        public override event ValueChangedDelegate<Vector3ViewModel> OnValueChanged;

        DoubleViewModel _w = new DoubleViewModel();
        public DoubleViewModel W
        {
            get { return _w; }
            set { SetAndNotify(ref _w, value); OnValueChanged?.Invoke(this); }
        }

        public Vector4ViewModel(double x = 0, double y = 0, double z = 0, double w = 1) : base(0)
        {
            X.Value = x;
            Y.Value = y;
            Z.Value = z;
            W.Value = w;
        }

        public void SetValue(double value)
        {
            X.Value = value;
            Y.Value = value;
            Z.Value = value;
            W.Value = value;
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Z}, {W}";
        }
    }
}
