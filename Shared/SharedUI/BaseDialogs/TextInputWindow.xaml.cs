// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;

namespace CommonControls.BaseDialogs
{
    /// <summary>
    /// Interaction logic for TextInputWindow.xaml
    /// </summary>
    public partial class TextInputWindow : Window
    {
        public TextInputWindow()
        {
            InitializeComponent();
        }

        public TextInputWindow(string title, string initialValue = "", bool focusTextInput = false)
        {
            InitializeComponent();
            Title = title;
            TextValue = initialValue;
            Owner = Application.Current.MainWindow;

            if (focusTextInput)
            {
                TextBoxItem.Focus();
            }
        }

        public string TextValue
        {
            get => TextBoxItem.Text;
            set => TextBoxItem.Text = value;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
