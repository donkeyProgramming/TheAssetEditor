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
using System.Windows.Shapes;
using Editors.ImportExport.Exporting.Presentation;
using Editors.ImportExport.Importing.Presentation;
using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Importing.Presentation
{
    public partial class ImportWindow : Window
    {
        private readonly ImporterCoreViewModel _viewModel;

        public ImportWindow(ImporterCoreViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        internal void Initialize(PackFileContainer packFileContainer, string packPath, string diskFile)
        {
            _viewModel.Initialize(packFileContainer, packPath, diskFile);            
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PathValidator.IsValid(_viewModel.SystemPath))
            {
                MessageBox.Show("Invalid or empty path", "Error");
                return;
            }

            _viewModel.Import();
            Close();
        }       
    }
}
