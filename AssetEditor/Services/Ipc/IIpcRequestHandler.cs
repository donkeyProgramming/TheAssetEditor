using System.Threading;
using System.Threading.Tasks;
using Shared.Core.PackFiles.Models;

namespace AssetEditor.Services.Ipc
{
    public interface IIpcRequestHandler
    {
        Task<IpcResponse> HandleAsync(IpcRequest request, CancellationToken cancellationToken);
    }

    public interface IExternalPackFileLookup
    {
        PackFile FindByPath(string path);
    }

    public interface IIpcUserNotifier
    {
        Task ShowExternalOpenFailedAsync(string normalizedPath, CancellationToken cancellationToken);
    }

    public interface IExternalPackLoader
    {
        Task<PackLoadResult> EnsureLoadedAsync(string packPathOnDisk, CancellationToken cancellationToken);
    }

    public class PackLoadResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        public static PackLoadResult Ok() => new() { Success = true };
        public static PackLoadResult Fail(string error) => new() { Success = false, Error = error };
    }
}
