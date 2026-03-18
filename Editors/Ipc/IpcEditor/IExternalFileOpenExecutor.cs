using Shared.Core.PackFiles.Models;

namespace Editors.Ipc
{
    public interface IExternalFileOpenExecutor
    {
        Task OpenAsync(PackFile file, bool bringToFront, bool openInExistingKitbashTab, CancellationToken cancellationToken);
    }
}
