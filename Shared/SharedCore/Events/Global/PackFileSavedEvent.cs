using Shared.Core.PackFiles.Models;

namespace Shared.Core.Events.Global
{
    public class PackFileSavedEvent
    {
    }

    public record PackFileLookUpEvent(string FileName, PackFileContainer? Container, bool Found);


    public record PackFileContainerAddedEvent(PackFileContainer Container);
    public record PackFileContainerRemovedEvent(PackFileContainer Container);


}
