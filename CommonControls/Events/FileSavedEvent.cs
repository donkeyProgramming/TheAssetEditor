using Common;
using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public Dictionary<IEditorViewModel, IServiceScope> Scopes { get; private set; } = new Dictionary<IEditorViewModel, IServiceScope>();
        
        public void Add(IEditorViewModel owner, IServiceScope scope)
        {
            if (Scopes.ContainsKey(owner))
                throw new ArgumentException("Owner already added!");

            Scopes.Add(owner, scope);
        }

        public void RemoveScope(IEditorViewModel owner)
        {
            Scopes[owner].Dispose();
            Scopes.Remove(owner);
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
            foreach (var scope in _scopeRepository.Scopes.Values)
            {
                var handler = scope.ServiceProvider.GetService<EventHub>();
                if (handler != null)
                    handler.Publish(e);
            }
        }
    }
}
