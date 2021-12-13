using System.Windows;
using System.Windows.Controls;

namespace CommonControls.BaseDialogs
{
    public class OptionalRadioButton : RadioButton
    {
        #region bool IsOptional dependency property
        public static DependencyProperty IsOptionalProperty =
            DependencyProperty.Register(
                "IsOptional",
                typeof(bool),
                typeof(OptionalRadioButton),
                new PropertyMetadata(true,
                    (obj, args) =>
                    {
                        ((OptionalRadioButton)obj).OnIsOptionalChanged(args);
                    }));
        public bool IsOptional
        {
            get
            {
                return (bool)GetValue(IsOptionalProperty);
            }
            set
            {
                SetValue(IsOptionalProperty, value);
            }
        }
        private void OnIsOptionalChanged(DependencyPropertyChangedEventArgs args)
        {
            // Add event handler if needed
        }
        #endregion

        protected override void OnClick()
        {
            bool? wasChecked = IsChecked;
            base.OnClick();
            if (IsOptional && wasChecked == true)
                IsChecked = false;
        }
    }
}
