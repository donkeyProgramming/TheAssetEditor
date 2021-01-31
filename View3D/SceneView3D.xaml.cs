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
using View3D.Scene;

namespace View3D
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class SceneView3D : UserControl
    {
        bool firstTime = true;
        public SceneView3D()
        {
            InitializeComponent();
            //DataContextChanged += SceneView3D_DataContextChanged;
        }

        private void SceneView3D_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if (GridItem.Children.Count != 0)
            //    return;
            //firstTime = false;
            //if (e.NewValue != null)
            //    GridItem.Children.Add( (e.NewValue as SceneViewModel).ThaScene);
            ////View3D.SceneView3D.
        }
    }
}