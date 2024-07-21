namespace Shared.Core.Events
{
    public class EventHub : IDisposable
    {
        public bool IsDisposed { get; private set; }
        Dictionary<Type, List<(Delegate Callback, object Owner)>> _callbackList = new();

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
