// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Shared.Ui.BaseDialogs.Table;

namespace CommonControls.Table
{
    /// <summary>
    /// Interaction logic for TableWindow.xaml
    /// </summary>
    public partial class TableWindow : Window
    {
        public TableWindow()
        {
            InitializeComponent();
        }

        public static void Show(TableViewModel viewModel)
        {
            var window = new TableWindow();
            window.DataContext = viewModel;
            window.ShowDialog();
        }
    }
}
