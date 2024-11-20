using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;

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
        public SingletonScopeEventHub(ScopeRepository scopeRepository) : base(scopeRepository)
        {
        }
    }

    class LocalScopeEventHub : EventHub, IEventHub
    {
        public LocalScopeEventHub(ScopeRepository scopeRepository) : base(scopeRepository)
        {
        }
    }

    abstract class EventHub : IDisposable
    {
        public bool IsDisposed { get; private set; }
        
        Dictionary<Type, List<(Delegate Callback, object Owner)>> _callbackList = new();
        private readonly ScopeRepository _scopeRepository;

        public EventHub(ScopeRepository scopeRepository)
        {
            _scopeRepository = scopeRepository;
        }

        public void PublishGlobalEvent<T>(T e)
        {
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
            if (_callbackList.ContainsKey(typeof(T)) == false)
                _callbackList.Add(typeof(T), new());

            _callbackList[typeof(T)].Add((action, owner));
        }

        public void Dispose()
        {
            IsDisposed = true;
            _callbackList.Clear();
            _callbackList = null;
        }
    }
}
