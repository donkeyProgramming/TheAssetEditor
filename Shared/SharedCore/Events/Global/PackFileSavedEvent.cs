using Shared.Core.PackFiles.Models;

namespace Shared.Core.Events.Global
{
    public record PackFileSavedEvent(PackFile File);
    public record PackFileLookUpEvent(string FileName, PackFileContainer? Container, bool Found);
    public record PackFileContainerAddedEvent(PackFileContainer Container);
    public record PackFileContainerRemovedEvent(PackFileContainer Container);
    public record PackFileContainerSetAsMainEditableEvent(PackFileContainer? Container);


    public class BeforePackFileContainerRemovedEvent(PackFileContainer removed)
    {
        public PackFileContainer Removed { get; internal set; } = removed;
        public bool AllowClose { get; set; } = true;
    }

    public record PackFileContainerFilesUpdatedEvent(PackFileContainer Container, List<PackFile> ChangedFiles);
    public record PackFileContainerFilesAddedEvent(PackFileContainer Container, List<PackFile> AddedFiles);
    public record PackFileContainerFilesRemovedEvent(PackFileContainer Container, List<PackFile> RemovedFiles);
    public record PackFileContainerFolderRemovedEvent(PackFileContainer Container, string Folder);
    public record PackFileContainerFolderRenamedEvent(PackFileContainer Container, string NewNodePath);
}
