using System.Threading;
using System.Threading.Tasks;
using Shared.Core.PackFiles.Models;

namespace AssetEditor.Services.Ipc
{
    public interface IExternalFileOpenExecutor
    {
        Task OpenAsync(PackFile file, bool bringToFront, bool openInExistingKitbashTab, CancellationToken cancellationToken);
    }
}
