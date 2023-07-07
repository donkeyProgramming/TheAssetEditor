using Common;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CommonControls.Events.Global
{
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
