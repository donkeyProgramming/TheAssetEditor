using System.Windows;
using GameWorld.Core.Services.SceneSaving;
using KitbasherEditor.ViewModels.SaveDialog;

namespace Editors.KitbasherEditor.ViewModels.SaveDialog
{
    /// <summary>
    /// Interaction logic for SaveDialogWindow.xaml
    /// </summary>
    public partial class SaveDialogWindow : Window
    {
        private readonly SaveDialogViewModel _saveDialogViewModel;

        public SaveDialogWindow(SaveDialogViewModel saveDialogViewModel)
        {
            InitializeComponent();
            _saveDialogViewModel = saveDialogViewModel;
            DataContext = _saveDialogViewModel;
        }

        public void Initialize(GeometrySaveSettings saveSettings)
        {
            _saveDialogViewModel.Initialize(saveSettings);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _saveDialogViewModel.ApplySettings();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
