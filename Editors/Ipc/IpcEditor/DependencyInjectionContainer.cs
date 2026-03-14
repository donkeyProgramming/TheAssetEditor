using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editors.Ipc
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IExternalPackFileLookup, ExternalPackFileLookup>();
            serviceCollection.AddTransient<IExternalPackLoader, ExternalPackLoader>();
            serviceCollection.AddTransient<IIpcUserNotifier, IpcUserNotifier>();
            serviceCollection.AddTransient<IExternalFileOpenExecutor, ExternalFileOpenExecutor>();
            serviceCollection.AddTransient<IIpcRequestHandler, IpcRequestHandler>();
            serviceCollection.AddSingleton<AssetEditorIpcServer>();
        }

        public override void RegisterTools(IEditorDatabase factory)
        {
    
        }
    }
}
