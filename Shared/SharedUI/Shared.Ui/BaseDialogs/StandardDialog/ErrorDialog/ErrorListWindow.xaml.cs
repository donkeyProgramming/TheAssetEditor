// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Shared.Core.ErrorHandling;
using Shared.Ui.BaseDialogs.ErrorListDialog;
using static Shared.Ui.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.BaseDialogs.ErrorListDialog
{
    /// <summary>
    /// Interaction logic for ErrorListWindow.xaml
    /// </summary>
    public partial class ErrorListWindow : Window
    {
        public ErrorListWindow()
        {
            Owner = System.Windows.Application.Current.MainWindow;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveReport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.Title = "Save a File";
            if (saveFileDialog.ShowDialog() == true)
            {
                var dataContext = (ErrorListViewModel)DataContext;
                var items = dataContext.ErrorItems;
                var output = $"Report of {dataContext.WindowTitle}\n";
                foreach (var item in items)
                {
                    output += "========================\n";
                    output += $"Error: {item.ErrorType}\n";
                    output += $"Item : {item.ItemName}\n";
                    output += $"What : {item.Description}\n";
                }

                File.WriteAllText(saveFileDialog.FileName, output);
            }
        }

        public static void ShowDialog(string titel, ErrorList errorItems, bool modal = true)
        {
            var window = new ErrorListWindow();
            var sortedErrors = errorItems.Errors.OrderByDescending(x => x.IsError).ToList();
            window.DataContext = new ErrorListViewModel()
            {
                WindowTitle = titel + " (" + errorItems.Errors.Count(x => x.IsError) + ")",
                ErrorItems = new ObservableCollection<ErrorListDataItem>(sortedErrors)
            };
            if (modal)
                window.ShowDialog();
            else
                window.Show();
        }
    }
}
