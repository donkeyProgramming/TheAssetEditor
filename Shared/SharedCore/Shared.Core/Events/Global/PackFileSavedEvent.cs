using Shared.Core.PackFiles.Models;

namespace Shared.Core.Events.Global
{
    public record PackFileSavedEvent(PackFile File);
    public record PackFileContainerSavedEvent(IPackFileContainer Container);
    public record PackFileLookUpEvent(string FileName, IPackFileContainer? Container, bool Found);

    public abstract record PackFileContainerManipulationEvent();
    public record PackFileContainerAddedEvent(IPackFileContainer Container) : PackFileContainerManipulationEvent;
    public record PackFileContainerRemovedEvent(IPackFileContainer Container) : PackFileContainerManipulationEvent;
    public record PackFileContainerSetAsMainEditableEvent(IPackFileContainer? Container);
    public record PackFileContainerFilesUpdatedEvent(IPackFileContainer Container, List<PackFile> ChangedFiles) : PackFileContainerManipulationEvent;
    public record PackFileContainerFilesAddedEvent(IPackFileContainer Container, List<PackFile> AddedFiles) : PackFileContainerManipulationEvent;
    public record PackFileContainerFilesRemovedEvent(IPackFileContainer Container, List<PackFile> RemovedFiles) : PackFileContainerManipulationEvent;
    public record PackFileContainerFolderRemovedEvent(IPackFileContainer Container, string Folder) : PackFileContainerManipulationEvent;
    public record PackFileContainerFolderRenamedEvent(IPackFileContainer Container, string NewNodePath) : PackFileContainerManipulationEvent;

    public class BeforePackFileContainerRemovedEvent(IPackFileContainer removed)
    {
        public IPackFileContainer Removed { get; internal set; } = removed;
        public bool AllowClose { get; set; } = true;
    }
}
