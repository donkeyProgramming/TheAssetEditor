using Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace CommonControls.Events
{
    public class FileSavedEvent 
    {
    }


    public class PackFileSavedEvent 
    {
    }


  

    public class ScopeRepository
    {
        public List<IServiceScope> Scopes { get; private set; } = new List<IServiceScope>();
        public void Add(IServiceScope scope) => Scopes.Add(scope);
        public void RemoveScope(IServiceScope scope)
        {
            scope.Dispose();
            Scopes.Remove(scope);
        }
    }

    public class GlobalEventSender
    {
        private readonly ScopeRepository _scopeRepository;

        public GlobalEventSender(ScopeRepository scopeRepository)
        {
            _scopeRepository = scopeRepository;
        }

        public void TriggerEvent<T>(T e)
        {
            foreach (var scope in _scopeRepository.Scopes)
            {
                var handler = scope.ServiceProvider.GetService<EventHub>();
                if (handler != null)
                    handler.Publish(e);
            }
        }
    }
}
