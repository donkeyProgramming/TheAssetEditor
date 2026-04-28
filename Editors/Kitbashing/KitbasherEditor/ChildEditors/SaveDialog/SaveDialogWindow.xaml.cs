using System.Windows;
using GameWorld.Core.Services.SceneSaving;
using KitbasherEditor.ViewModels.SaveDialog;
using WindowHandling;

namespace Editors.KitbasherEditor.ViewModels.SaveDialog
{
    public partial class SaveDialogWindow : AssetEditorWindow
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

        private void Window_OnContentRendered(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
    }
}
