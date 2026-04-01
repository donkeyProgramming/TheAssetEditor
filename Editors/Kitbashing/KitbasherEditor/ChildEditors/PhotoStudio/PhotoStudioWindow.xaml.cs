using System.Windows;
using Editors.KitbasherEditor.ChildEditors.MeshFitter;
using WindowHandling;

namespace Editors.KitbasherEditor.ChildEditors.PhotoStudio
{
    /// <summary>
    /// Interaction logic for PhotoStudioWindow.xaml
    /// </summary>
    public partial class PhotoStudioWindow : AssetEditorWindow
    {
        public PhotoStudioViewModel ViewModel { get; set; }

        public PhotoStudioWindow(PhotoStudioViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        private void Window_OnContentRendered(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
    }
}
