// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Shared.Ui.BaseDialogs.MathViews;

namespace CommonControls.MathViews
{
    /// <summary>
    /// Interaction logic for Vector3View.xaml
    /// </summary>
    public partial class Vector3View : UserControl
    {
        public Vector3View()
        {
            InitializeComponent();
        }

        public Vector3ViewModel Vector3
        {
            get { return (Vector3ViewModel)GetValue(Vector3Property); }
            set { SetValue(Vector3Property, value); }
        }

        public static readonly DependencyProperty Vector3Property =
            DependencyProperty.Register("Vector3", typeof(Vector3ViewModel), typeof(Vector3View), new PropertyMetadata(null));

        public int FieldWidth
        {
            get { return (int)GetValue(FieldWidthProperty); }
            set { SetValue(FieldWidthProperty, value); }
        }

        public static readonly DependencyProperty FieldWidthProperty =
            DependencyProperty.Register("FieldWidth", typeof(int), typeof(Vector3View), new PropertyMetadata(60, null));

        public int NumbersMaxLength
        {
            get { return (int)GetValue(NumbersMaxLengthProperty); }
            set { SetValue(NumbersMaxLengthProperty, value); }
        }

        public static readonly DependencyProperty NumbersMaxLengthProperty =
            DependencyProperty.Register("NumbersMaxLength", typeof(int), typeof(Vector3View), new PropertyMetadata(60, null));
    }
}


