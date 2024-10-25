using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Shared.Core.DependencyInjection
{
    public class ScopeRepository
    {
        public Dictionary<EditorInterfaces, IServiceScope> Scopes { get; private set; } = new Dictionary<EditorInterfaces, IServiceScope>();

        public void Add(EditorInterfaces owner, IServiceScope scope)
        {
            if (Scopes.ContainsKey(owner))
                throw new ArgumentException("Owner already added!");

            Scopes.Add(owner, scope);
        }

        public void RemoveScope(EditorInterfaces owner)
        {
            Scopes[owner].Dispose();
            Scopes.Remove(owner);
        }
    }
}
