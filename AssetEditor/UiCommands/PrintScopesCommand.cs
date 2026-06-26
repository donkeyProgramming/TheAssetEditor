using Shared.Core.Events;
using Shared.Core.ToolCreation;

namespace AssetEditor.UiCommands
{
    public class PrintScopesCommand : IAeCommand
    {
        private readonly IScopeRepository _scopeRepository;

        public PrintScopesCommand(IScopeRepository scopeRepository)
        {
            _scopeRepository = scopeRepository;
        }

        public void Execute() => _scopeRepository.Print();
    }
}
