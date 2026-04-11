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
        public SingletonScopeEventHub(IScopeRepository scopeRepository) : base(scopeRepository, nameof(IGlobalEventHub), true)
        {
        }
    }

    class LocalScopeEventHub : EventHub, IEventHub
    {
        public LocalScopeEventHub(IScopeRepository scopeRepository) : base(scopeRepository, nameof(IEventHub), false)
        {
        }
    }

    abstract class EventHub : IDisposable
    {
        private readonly ILogger _logger = Logging.Create<EventHub>();
        bool _isDisposed = false;
        
        Dictionary<Type, List<(Delegate Callback, object Owner)>> _callbackList = new();
        private readonly IScopeRepository _scopeRepository;
        private readonly string _hubName;
        private readonly bool _isGlobal;

        public EventHub(IScopeRepository scopeRepository, string hubName, bool isGlobal)
        {
            _scopeRepository = scopeRepository;
            _hubName = hubName;
            _isGlobal = isGlobal;
        }

        public void PublishGlobalEvent<T>(T e)
        {
            // If gloabl, send to self and all children
            if (_isGlobal)
            {
                _logger.Here().Information($"Publshing global event {e.GetType().Name} on {_hubName}");

                // Send to global subscribers 
                Publish(e);

                // get the event hub that lives in the "main" scope
                var mainScope = _scopeRepository.GetRequiredServiceRootScope<IEventHub>();
                mainScope.Publish(e);

                // Send to all editors
                var handles = _scopeRepository.GetEditorHandles();
                foreach (var editorHandle in handles)
                {
                    var scopedEventHub = _scopeRepository.GetRequiredService<IEventHub>(editorHandle);
                    if (scopedEventHub != null)
                        scopedEventHub.Publish(e);
                }
            }
            // If local, send to global
            else
            {
                var globalEventHub = _scopeRepository.GetRequiredServiceRootScope<IGlobalEventHub>();
                if (globalEventHub != null)
                    globalEventHub.PublishGlobalEvent(e);
            }
        }

        public void Publish<T>(T instance)
        {
            foreach (var callbackItem in _callbackList)
            {
                var subscribedType = callbackItem.Key;
                var isSameType = subscribedType == typeof(T);
                var isAssignableFrom = subscribedType.IsAssignableFrom(typeof(T));

                if (isSameType || isAssignableFrom)
                {
                    foreach (var subscriber in callbackItem.Value)
                    {
                        var action = (Action<T>)subscriber.Callback;
                        action(instance);
                    }
                }

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
            _callbackList?.Clear();
        }
    }
}
