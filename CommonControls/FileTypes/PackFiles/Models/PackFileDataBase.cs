// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using CommonControls.Common;

namespace CommonControls.FileTypes.PackFiles.Models
{
    public delegate void ContainerUpdatedDelegate(PackFileContainer container);
    public delegate void PackFileContainerLoadedDelegate(PackFileContainer container);
    public delegate bool PackFileContainerRemovedDelegate(PackFileContainer container);


    public delegate void FileUpdated(PackFileContainer container, PackFile file);
    public delegate void PackFileUpdatedDelegate(PackFileContainer container, List<PackFile> file);
    public delegate void PackFileFolderUpdatedDelegate(PackFileContainer container, string folder);
    public delegate void PackFileFolderRenamedDelegate(PackFileContainer container, string folder);


    public class PackFileDataBase : NotifyPropertyChangedImpl
    {
        public event ContainerUpdatedDelegate ContainerUpdated;
        public event PackFileContainerLoadedDelegate PackFileContainerLoaded;
        public event PackFileContainerRemovedDelegate PackFileContainerRemoved;
        public event PackFileContainerRemovedDelegate BeforePackFileContainerRemoved;

        public event PackFileUpdatedDelegate PackFilesUpdated;
        public event PackFileUpdatedDelegate PackFilesAdded;
        public event PackFileUpdatedDelegate PackFilesRemoved;
        public event PackFileFolderUpdatedDelegate PackFileFolderRemoved;
        public event PackFileFolderRenamedDelegate PackFileFolderRenamed;

        // File updated
        // File added
        // FileRemoved

        public List<PackFileContainer> PackFiles { get; set; } = new List<PackFileContainer>();
        public PackFileContainer PackSelectedForEdit { get; set; }

        public void AddPackFile(PackFileContainer pf)
        {
            PackFiles.Add(pf);
            PackFileContainerLoaded?.Invoke(pf);
        }

        public void RemovePackFile(PackFileContainer pf)
        {
            var canUnload = BeforePackFileContainerRemoved?.Invoke(pf);
            if (canUnload.HasValue == false || canUnload.HasValue == true && canUnload.Value == true)
            {
                PackFiles.Remove(pf);

                if (PackSelectedForEdit == pf)
                    PackSelectedForEdit = null;

                PackFileContainerRemoved?.Invoke(pf);
            }
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

        public void TriggerPackFileFolderRenamed(PackFileContainer container, string path)
        {
            PackFileFolderRenamed?.Invoke(container, path);
        }
    }
}
