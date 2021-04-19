using AnimMetaEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace AnimMetaEditor.Views.MetadataTableViews
{
    /// <summary>
    /// Interaction logic for MetaDataView.xaml
    /// </summary>
    public partial class MetaDataView : UserControl
    {
        public MetaDataView()
        {
            InitializeComponent();
        }

        private void StackPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as DataGrid;
            var cont = obj.DataContext as MainViewModel;
            cont.DataTable.DataGridReference = obj;
        }
    }

    public class RowDataValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var drv = (value as BindingGroup).Items[0] as DataTableRow;
            if (!string.IsNullOrEmpty(drv.GetError()))
            {
                return new ValidationResult(false, drv.GetError());
            }
            else
            {
                return ValidationResult.ValidResult;
            }
        }
    }
}
