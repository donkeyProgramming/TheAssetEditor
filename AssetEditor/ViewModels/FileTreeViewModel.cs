using Common;
using CommonControls.PackFileBrowser;
using CommonControls.Simple;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System.Windows.Input;

namespace AssetEditor.ViewModels
{
   class FileTreeViewModel : PackFileBrowserViewModel
   {
       public FileTreeViewModel(PackFileService packFileService) : base(packFileService)
       {
            ContextMenuVisibility = System.Windows.Visibility.Hidden;
       }
   }
}
