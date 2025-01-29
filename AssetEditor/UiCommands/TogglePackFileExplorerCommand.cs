using System.Windows;
using AssetEditor.ViewModels;
using Shared.Core.Events;

namespace AssetEditor.UiCommands
{
    public class TogglePackFileExplorerCommand : IUiCommand
    {
        private readonly MainViewModel _mainViewModel;

        public TogglePackFileExplorerCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public void Execute()
        {
            _mainViewModel.IsPackFileExplorerVisible = !_mainViewModel.IsPackFileExplorerVisible;
            _mainViewModel.FileTreeColumnWidth = _mainViewModel.IsPackFileExplorerVisible ? new GridLength(0.28, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
        }
    }
}
