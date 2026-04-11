// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace Shared.Ui.BaseDialogs
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
            var wasChecked = IsChecked;
            base.OnClick();
            if (IsOptional && wasChecked == true)
                IsChecked = false;
        }
    }
}
