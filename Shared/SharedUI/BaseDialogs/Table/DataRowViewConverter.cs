using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace Shared.Ui.BaseDialogs.Table
{
    public class DataRowViewConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var cell = value as DataGridCell;
            if (cell == null)
                return null;

            var drv = cell.DataContext as System.Data.DataRowView;
            if (drv == null)
                return null;


            return drv.Row[cell.Column.SortMemberPath];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
