using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Shared.Core.DependencyInjection
{
    public class ScopeRepository
    {
        public IServiceScope Root { get; set; } // TODO: THis is a bad hack
        public Dictionary<IEditorInterface, IServiceScope> Scopes { get; private set; } = new Dictionary<IEditorInterface, IServiceScope>();

        public void Add(IEditorInterface owner, IServiceScope scope)
        {
            if (Scopes.ContainsKey(owner))
                throw new ArgumentException("Owner already added!");

            Scopes.Add(owner, scope);
        }

        public void RemoveScope(IEditorInterface owner)
        {
            Scopes[owner].Dispose();
            Scopes.Remove(owner);
        }
    }
}
