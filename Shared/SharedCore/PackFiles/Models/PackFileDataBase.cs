using Shared.Core.Misc;

namespace Shared.Core.PackFiles.Models
{
   // public delegate void ContainerUpdatedDelegate(PackFileContainer container);
   // public delegate void PackFileContainerLoadedDelegate(PackFileContainer container);
   // public delegate bool PackFileContainerRemovedDelegate(PackFileContainer container);
   //
   //
   // public delegate void FileUpdated(PackFileContainer container, PackFile file);
   // public delegate void PackFileUpdatedDelegate(PackFileContainer container, List<PackFile> file);
   // public delegate void PackFileFolderUpdatedDelegate(PackFileContainer container, string folder);
   // public delegate void PackFileFolderRenamedDelegate(PackFileContainer container, string folder);




    public class PackFileDataBase : NotifyPropertyChangedImpl
    {
        //public event PackFileContainerLoadedDelegate PackFileContainerLoaded;
        //public event PackFileContainerRemovedDelegate PackFileContainerRemoved;


        //public event PackFileUpdatedDelegate PackFilesUpdated;
        //public event PackFileUpdatedDelegate PackFilesAdded;
        //public event PackFileUpdatedDelegate PackFilesRemoved;
        //public event PackFileFolderUpdatedDelegate PackFileFolderRemoved;
        //public event PackFileFolderRenamedDelegate PackFileFolderRenamed;

        // File updated
        // File added
        // FileRemoved

        public List<PackFileContainer> PackFiles { get; set; } = new List<PackFileContainer>();
        public PackFileContainer? PackSelectedForEdit { get; set; }

        public PackFileDataBase(bool allowEvents = true)
        {
  
        }

       // public void AddPackFileContainer(PackFileContainer pf)
       // {
       //     PackFiles.Add(pf);
       //     if(_allowEvents)
       //         PackFileContainerLoaded?.Invoke(pf);
       // }


       // public void TriggerPackFilesUpdated(PackFileContainer container, List<PackFile> files)
       // {
       //     if (_allowEvents)
       //         PackFilesUpdated?.Invoke(container, files);
       // }

       // public void TriggerPackFileAdded(PackFileContainer container, List<PackFile> files)
       // {
       //     if (_allowEvents)
       //         PackFilesAdded?.Invoke(container, files);
       // }
       //
       //public void TriggerPackFileRemoved(PackFileContainer container, List<PackFile> files)
       //{
       //    if (_allowEvents)
       //        PackFilesRemoved?.Invoke(container, files);
       //}

       //public void TriggerPackFileFolderRemoved(PackFileContainer container, string path)
       //{
       //    if (_allowEvents)
       //        PackFileFolderRemoved?.Invoke(container, path);
       //}
       //
       //public void TriggerPackFileFolderRenamed(PackFileContainer container, string path)
       //{
       //    if (_allowEvents)
       //        PackFileFolderRenamed?.Invoke(container, path);
       //}
    }
}
