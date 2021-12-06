using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CommonControls.MathViews
{
    /// <summary>
    /// Interaction logic for Vector4View.xaml
    /// </summary>
    public partial class Vector4View : UserControl
    {
        public Vector4View()
        {
            InitializeComponent();
        }


        public Vector4ViewModel Vector4
        {
            get { return (Vector4ViewModel)GetValue(Vector4Property); }
            set { SetValue(Vector4Property, value); }
        }

        public static readonly DependencyProperty Vector4Property =
            DependencyProperty.Register("Vector4", typeof(Vector4ViewModel), typeof(Vector4View), new PropertyMetadata(null));

        public int FieldWidth
        {
            get { return (int)GetValue(FieldWidthProperty); }
            set { SetValue(FieldWidthProperty, value); }
        }

        public static readonly DependencyProperty FieldWidthProperty =
            DependencyProperty.Register("FieldWidth", typeof(int), typeof(Vector4View), new PropertyMetadata(60, null));

        public int NumbersMaxLength
        {
            get { return (int)GetValue(NumbersMaxLengthProperty); }
            set { SetValue(NumbersMaxLengthProperty, value); }
        }

        public static readonly DependencyProperty NumbersMaxLengthProperty =
            DependencyProperty.Register("NumbersMaxLength", typeof(int), typeof(Vector4View), new PropertyMetadata(60, null));
    }
}
