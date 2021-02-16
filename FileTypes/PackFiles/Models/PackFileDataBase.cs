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

    public class PackFileDataBase : NotifyPropertyChangedImpl
    {
        public event ContainerUpdatedDelegate ContainerUpdated;
        public event PackFileContainerLoadedDelegate PackFileContainerLoaded;
        public event PackFileContainerRemovedDelegate PackFileContainerRemoved;

        public List<PackFileContainer> PackFiles { get; set; } = new List<PackFileContainer>();

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

    }
}
