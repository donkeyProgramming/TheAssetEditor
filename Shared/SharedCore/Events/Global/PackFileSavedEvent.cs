using Shared.Core.PackFiles.Models;

namespace Shared.Core.Events.Global
{
    public class PackFileSavedEvent
    {
    }

    public record PackFileLookUpEvent(string FileName, PackFileContainer? Container, bool Found);


    public record PackFileContainerAddedEvent(PackFileContainer Container);
    public record PackFileContainerRemovedEvent(PackFileContainer Container);



    public record PackFileContainerSetAsMainEditable(PackFileContainer? Container);


    public class BeforePackFileContainerRemovedEvent
    {
        public PackFileContainer Removed { get; internal set; }
        public bool AllowClose { get; set; } = true;

        public BeforePackFileContainerRemovedEvent(PackFileContainer removed)
        {
            Removed = removed;
        }
    }


}
