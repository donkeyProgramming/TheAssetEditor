using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;

namespace Shared.Core.Events
{
    public interface IEventHub
    {
        void PublishGlobalEvent<T>(T e);
        void Publish<T>(T e);

        void Register<T>(object owner, Action<T> action);
        void UnRegister(object owner);
    }


    public interface IGlobalEventHub
    {
        void PublishGlobalEvent<T>(T e);
        void Register<T>(object owner, Action<T> action);
        void UnRegister(object owner);
    }

    class SingletonScopeEventHub : EventHub, IGlobalEventHub
    {
        public SingletonScopeEventHub(ScopeRepository scopeRepository) : base(scopeRepository, nameof(IGlobalEventHub))
        {
        }
    }

    class LocalScopeEventHub : EventHub, IEventHub
    {
        public LocalScopeEventHub(ScopeRepository scopeRepository) : base(scopeRepository, nameof(IEventHub))
        {
        }
    }

    abstract class EventHub : IDisposable
    {
        private readonly ILogger _logger = Logging.Create<EventHub>();
        bool _isDisposed = false;
        
        Dictionary<Type, List<(Delegate Callback, object Owner)>> _callbackList = new();
        private readonly ScopeRepository _scopeRepository;
        private readonly string _hubName;

        public EventHub(ScopeRepository scopeRepository, string hubName)
        {
            _scopeRepository = scopeRepository;
            _hubName = hubName;
        }

        public void PublishGlobalEvent<T>(T e)
        {
            _logger.Here().Information($"Publshing event {e.GetType().Name} on {_hubName}");

            // Send to all editors
            foreach (var scope in _scopeRepository.Scopes.Values)
            {
                var handler = scope.ServiceProvider.GetRequiredService<IEventHub>();
                if (handler != null)
                    handler.Publish(e);
            }

            // Send to the root scope, which the main windows, packfiles and a few other things live in
            var rootHandler = _scopeRepository.Root.ServiceProvider.GetRequiredService<IEventHub>();
            if (rootHandler != null)
                rootHandler.Publish(e);

            // Send to other globals
            Publish(e);
        }

        public void Publish<T>(T instance)
        {
            _callbackList.TryGetValue(typeof(T), out var callbackItems);
            if (callbackItems == null)
                return;
            foreach (var callbackItem in callbackItems)
            {
                var action = (Action<T>)callbackItem.Callback;
                action(instance);
            }
        }

        public void UnRegister(object owner)
        {
            _logger.Here().Information($"UnRegistering all events from {owner.GetType().Name} from {_hubName}");

            foreach (var callbackTypeList in _callbackList)
            {
                var itemsToRemove = new List<(Delegate Callback, object Owner)>();

                foreach (var item in callbackTypeList.Value)
                {
                    if (item.Owner == owner)
                        itemsToRemove.Add(item);
                }

                foreach (var removeItem in itemsToRemove)
                    callbackTypeList.Value.Remove(removeItem);
            }
        }

        public void Register<T>(object owner, Action<T> action)
        {
            _logger.Here().Information($"Registering event {typeof(T).Name} to {owner.GetType().Name} on {_hubName}");

            if (_callbackList.ContainsKey(typeof(T)) == false)
                _callbackList.Add(typeof(T), new());

            _callbackList[typeof(T)].Add((action, owner));
        }

        public void Dispose()
        {
            _isDisposed = true;
            _callbackList.Clear();
            _callbackList = null;
        }
    }
}
