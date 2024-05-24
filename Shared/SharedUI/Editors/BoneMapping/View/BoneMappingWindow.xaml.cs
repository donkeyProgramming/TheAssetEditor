// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using Shared.Ui.Editors.BoneMapping;

namespace CommonControls.Editors.BoneMapping.View
{
    /// <summary>
    /// Interaction logic for BoneMappingWindow.xaml
    /// </summary>
    public partial class BoneMappingWindow : Window
    {
        public event EventHandler ApplySettings;
        public bool Result { get; private set; } = false;

        public BoneMappingWindow()
        {
            InitializeComponent();
        }

        public BoneMappingWindow(BoneMappingViewModel dataContext, bool showApplyButton)
        {
            DataContext = dataContext;
            InitializeComponent();
            if (showApplyButton == false)
                ApplyButtonHandle.Visibility = Visibility.Collapsed;

            Title = $"Bone Mapping - Mapping from '{dataContext.MeshSkeletonName.Value}' to '{dataContext.ParentSkeletonName.Value}'";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as BoneMappingViewModel;
            var res = viewModel.Validate(out string errorText);
            if (res == false)
            {
                var messageBoxResult = MessageBox.Show("Are you sure you want to do this?\n\n" + errorText + "\n\nContinue?", "Error", MessageBoxButton.OKCancel);
                if (messageBoxResult == MessageBoxResult.Cancel)
                    return;
            }

            Result = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySettings?.Invoke(this, e);
        }
    }
}

