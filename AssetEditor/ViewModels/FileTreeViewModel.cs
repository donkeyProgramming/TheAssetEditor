using CommonControls.PackFileBrowser;
using CommonControls.Services;

namespace AssetEditor.ViewModels
{
   class FileTreeViewModel : PackFileBrowserViewModel
   {
       public FileTreeViewModel(PackFileService packFileService) : base(packFileService)
       {
       }
   }
}
