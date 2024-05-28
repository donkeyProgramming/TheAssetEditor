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
using Shared.Ui.BaseDialogs.WindowHandling;

namespace KitbasherEditor.Views.EditorViews
{
    /// <summary>
    /// Interaction logic for SaveDialogView.xaml
    /// </summary>
    public partial class SaveDialogView : AssetEditorControl
    {
        public SaveDialogView()
        {
            InitializeComponent();
        }

        private void Button_Save(object sender, RoutedEventArgs e) => TriggerRequestOk();
        private void Button_Cancel(object sender, RoutedEventArgs e) => TriggerRequestClose();
    }
}
