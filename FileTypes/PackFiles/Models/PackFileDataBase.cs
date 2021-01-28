using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FileTypes.PackFiles.Models
{
    public delegate void FileAddedDelegate(IPackFile newNode, IPackFile parentNode);
    public delegate void FileRemovedDelegate(IPackFile deletedNode, IPackFile parentNode);
    public delegate void PackFileContainerLoadedDelegate(PackFileContainer container);

    public class PackFileDataBase : NotifyPropertyChangedImpl
    {
        public event FileAddedDelegate FileAdded;
        public event FileRemovedDelegate FileRemoved;
        public event PackFileContainerLoadedDelegate PackFileContainerLoaded;

        public List<PackFileContainer> PackFiles { get; set; } = new List<PackFileContainer>();

        public void AddPackFile(PackFileContainer pf)
        {
            PackFiles.Add(pf);
            TriggerPackFileContainerLoaded(pf);
        }

        public void Clear()
        {
            PackFiles.Clear();
        }

        public void TriggerFileAdded(IPackFile newNode, IPackFile parentNode)
        {
            FileAdded?.Invoke(newNode, parentNode);
        }

        public void TriggerFileRemoved(IPackFile deletedNode, IPackFile parentNode)
        {
            FileRemoved?.Invoke(deletedNode, parentNode);
        }

        public void TriggerPackFileContainerLoaded(PackFileContainer container)
        {
            PackFileContainerLoaded?.Invoke(container);
        }
    }
}
