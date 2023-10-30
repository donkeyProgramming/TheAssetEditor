// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Windows;
using AssetManagement.Strategies.Fbx.ExportDIalog.UserControls;

namespace AssetManagement.Strategies.Fbx.ExportDIalog
{
    /// <summary>
    /// Interaction logic for ExportDIalog.xaml
    /// </summary>
    public partial class ExportDIalog : Window
    {
        public ExportDIalog()
        {
            var newPanel = new ExportPanelUserControl();
            AddChild(newPanel);
            AddChild(newPanel);

            InitializeComponent();
        }
    }
}
