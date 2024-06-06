using System.Windows;
using CommonControls.BaseDialogs;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using TextureEditor.Views;
using static TextureEditor.ViewModels.TexturePreviewController;

namespace TextureEditor.ViewModels
{
    public static class ImageWindowCreator
    {
        public static void CreateWindow(string imagePath, PackFileService packFileService, EventHub eventHub)
        {
            //TexturePreviewViewModel viewModel = new TexturePreviewViewModel();
            //viewModel.ImagePath.Value = imagePath;
            //
            //using (var controller = new TexturePreviewController(imagePath, viewModel, packFileService, eventHub))
            //{
            //    var containingWindow = new ControllerHostWindow(false, ResizeMode.CanResize);
            //    containingWindow.Title = "Texture Preview Window";
            //    containingWindow.Content = new TexturePreviewView() { DataContext = new ViewModelWrapper() { ViewModel = viewModel } };
            //    containingWindow.ShowDialog();
            //}
        }
    }
}
