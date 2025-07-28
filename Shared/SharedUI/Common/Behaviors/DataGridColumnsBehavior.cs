using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Shared.Ui.Common.Behaviors
{
    public static class DataGridColumnsBehavior
    {
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.RegisterAttached(
                "Columns",
                typeof(IEnumerable<DataGridColumn>),
                typeof(DataGridColumnsBehavior),
                new PropertyMetadata(null, OnColumnsChanged));

        public static void SetColumns(DependencyObject obj, IEnumerable<DataGridColumn> value) => obj.SetValue(ColumnsProperty, value);

        public static IEnumerable<DataGridColumn> GetColumns(DependencyObject obj) => (IEnumerable<DataGridColumn>)obj.GetValue(ColumnsProperty);

        private static void OnColumnsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is not DataGrid dataGrid)
                return;

            var newColumns = e.NewValue as IEnumerable<DataGridColumn>;

            if (e.OldValue is INotifyCollectionChanged oldObjects && e.OldValue is IEnumerable<DataGridColumn> oldColumns)
                oldObjects.CollectionChanged -= (_, __) => Refresh(dataGrid, oldColumns);

            if (e.NewValue is INotifyCollectionChanged newObjects && newColumns != null)
                newObjects.CollectionChanged += (_, __) => Refresh(dataGrid, newColumns);

            Refresh(dataGrid, newColumns);
        }

        private static void Refresh(DataGrid dataGrid, IEnumerable<DataGridColumn>? columns)
        {
            dataGrid.Columns.Clear();
            if (columns is null)
                return;

            foreach (var column in columns)
                dataGrid.Columns.Add(column);
        }
    }
}
