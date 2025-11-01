using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Shared.Ui.Common.Behaviors
{
    public static class DataGridColumnsBehavior
    {
        private static readonly DependencyProperty ColumnsChangedHandlerProperty =
            DependencyProperty.RegisterAttached(
                "ColumnsChangedHandler",
                typeof(NotifyCollectionChangedEventHandler),
                typeof(DataGridColumnsBehavior),
                new PropertyMetadata(null));

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

            // Unhook old
            if (e.OldValue is INotifyCollectionChanged oldObs)
            {
                var oldHandler = (NotifyCollectionChangedEventHandler)dataGrid.GetValue(ColumnsChangedHandlerProperty);
                if (oldHandler != null)
                    oldObs.CollectionChanged -= oldHandler;
            }

            var newColumns = e.NewValue as IEnumerable<DataGridColumn>;

            // Hook new
            if (e.NewValue is INotifyCollectionChanged newObs && newColumns != null)
            {
                NotifyCollectionChangedEventHandler handler = (_, __) => Refresh(dataGrid, newColumns);
                dataGrid.SetValue(ColumnsChangedHandlerProperty, handler);
                newObs.CollectionChanged += handler;
            }
            else
                dataGrid.ClearValue(ColumnsChangedHandlerProperty);

            Refresh(dataGrid, newColumns);
        }

        private static void Refresh(DataGrid dataGrid, IEnumerable<DataGridColumn>? columns)
        {
            dataGrid.Columns.Clear();

            if (columns is null)
                return;

            foreach (var column in columns)
            {
                // Never add the same instance twice
                if (dataGrid.Columns.Contains(column))
                    continue;

                dataGrid.Columns.Add(column);
            }
        }
    }

}
