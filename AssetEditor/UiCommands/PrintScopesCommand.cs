using Shared.Core.DependencyInjection;
using Shared.Core.Events;

namespace AssetEditor.UiCommands
{
    public class PrintScopesCommand : IUiCommand
    {
        private readonly IScopeRepository _scopeRepository;

        public PrintScopesCommand(IScopeRepository scopeRepository)
        {
            _scopeRepository = scopeRepository;
        }

        public void Execute() => _scopeRepository.Print();
    }
}
