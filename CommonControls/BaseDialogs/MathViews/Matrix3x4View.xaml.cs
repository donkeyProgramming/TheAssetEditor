
using Common;
using CommonControls.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for Matrix3x4View.xaml
    /// </summary>
    public partial class Matrix3x4View : UserControl
    {
        public FileMatrix3x4ViewData MatrixData
        {
            get { return (FileMatrix3x4ViewData)GetValue(MatrixDataProperty); }
            set { SetValue(MatrixDataProperty, value); }
        }

        public static readonly DependencyProperty MatrixDataProperty = DependencyProperty.Register("MatrixData", typeof(FileMatrix3x4ViewData), typeof(Matrix3x4View), new UIPropertyMetadata(null));

        public Matrix3x4View()
        {
            InitializeComponent();
        }
    }

    public class FileMatrix3x4ViewData : NotifyPropertyChangedImpl
    {
        //FileMatrix3x4 _source;
        //public FileMatrix3x4ViewData(FileMatrix3x4 matrix, string name)
        //{
        //    //_source = matrix;
        //    //_name = name;
        //    //Matrix = new ObservableCollection<Vector4ViewData>();
        //    //Matrix.Add(new Vector4ViewData(_source.Matrix[0]));
        //    //Matrix.Add(new Vector4ViewData(_source.Matrix[1]));
        //    //Matrix.Add(new Vector4ViewData(_source.Matrix[2]));
        //}


        public ObservableCollection<Vector4ViewModel> Matrix { get; set; }

        string _name;
        public string Name
        {
            get { return _name; }
            set { SetAndNotify(ref _name, value); }
        }
    }
}
