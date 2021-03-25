using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FileTypes.PackFiles.Models
{
    public delegate void ContainerUpdatedDelegate(PackFileContainer container);
    public delegate void PackFileContainerLoadedDelegate(PackFileContainer container);
    public delegate void PackFileContainerRemovedDelegate(PackFileContainer container);


    public delegate void FileUpdated(PackFileContainer container, PackFile file);
    public delegate void PackFileUpdatedDelegate(PackFileContainer container, List<PackFile> file);
    public delegate void PackFileFolderUpdatedDelegate(PackFileContainer container, string folder);


    public class PackFileDataBase : NotifyPropertyChangedImpl
    {
        public event ContainerUpdatedDelegate ContainerUpdated;
        public event PackFileContainerLoadedDelegate PackFileContainerLoaded;
        public event PackFileContainerRemovedDelegate PackFileContainerRemoved;


        public event PackFileUpdatedDelegate PackFilesUpdated;
        public event PackFileUpdatedDelegate PackFilesAdded;
        public event PackFileUpdatedDelegate PackFilesRemoved;
        public event PackFileFolderUpdatedDelegate PackFileFolderRemoved;

        // File updated
        // File added
        // FileRemoved

        public List<PackFileContainer> PackFiles { get; set; } = new List<PackFileContainer>();
        public PackFileContainer PackSelectedForEdit { get; set;  }

        public void AddPackFile(PackFileContainer pf)
        {
            PackFiles.Add(pf);
            PackFileContainerLoaded?.Invoke(pf);
        }

        public void RemovePackFile(PackFileContainer pf)
        {
            PackFiles.Remove(pf);
            PackFileContainerRemoved?.Invoke(pf);
        }

        public void Clear()
        {
            PackFiles.Clear();
        }

        public void TriggerContainerUpdated(PackFileContainer container)
        {
            ContainerUpdated?.Invoke(container);
        }

        public void TriggerPackFilesUpdated(PackFileContainer container, List<PackFile> files)
        {
            PackFilesUpdated?.Invoke(container, files);
        }

        public void TriggerPackFileAdded(PackFileContainer container, List<PackFile> files)
        {
            PackFilesAdded?.Invoke(container, files);
        }

        public void TriggerPackFileRemoved(PackFileContainer container, List<PackFile> files)
        {
            PackFilesRemoved?.Invoke(container, files);
        }

        public void TriggerPackFileFolderRemoved(PackFileContainer container, string path)
        {
            PackFileFolderRemoved?.Invoke(container, path);
        }
    }
}
