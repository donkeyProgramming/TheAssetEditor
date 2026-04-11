using Shared.Core.PackFiles.Models;

namespace Shared.Core.Events.Global
{
    public record PackFileSavedEvent(PackFile File);
    public record PackFileContainerSavedEvent(PackFileContainer Container);
    public record PackFileLookUpEvent(string FileName, PackFileContainer? Container, bool Found);

    public abstract record PackFileContainerManipulationEvent();
    public record PackFileContainerAddedEvent(PackFileContainer Container) : PackFileContainerManipulationEvent;
    public record PackFileContainerRemovedEvent(PackFileContainer Container) : PackFileContainerManipulationEvent;
    public record PackFileContainerSetAsMainEditableEvent(PackFileContainer? Container);
    public record PackFileContainerFilesUpdatedEvent(PackFileContainer Container, List<PackFile> ChangedFiles) : PackFileContainerManipulationEvent;
    public record PackFileContainerFilesAddedEvent(PackFileContainer Container, List<PackFile> AddedFiles) : PackFileContainerManipulationEvent;
    public record PackFileContainerFilesRemovedEvent(PackFileContainer Container, List<PackFile> RemovedFiles) : PackFileContainerManipulationEvent;
    public record PackFileContainerFolderRemovedEvent(PackFileContainer Container, string Folder) : PackFileContainerManipulationEvent;
    public record PackFileContainerFolderRenamedEvent(PackFileContainer Container, string NewNodePath) : PackFileContainerManipulationEvent;

    public class BeforePackFileContainerRemovedEvent(PackFileContainer removed)
    {
        public PackFileContainer Removed { get; internal set; } = removed;
        public bool AllowClose { get; set; } = true;
    }
}
