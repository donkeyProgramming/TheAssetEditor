using System;
using System.Collections.Generic;

namespace Common
{
    public class EventHub : IDisposable
    {
        Dictionary<Type, List<Delegate>> _callbackList = new Dictionary<Type, List<Delegate>>();

        public void Publish<T>(T instance)
        {
            _callbackList.TryGetValue(typeof(T), out var callbackItems);
            if (callbackItems == null)
                return;
            foreach (var callbackItem in callbackItems)
            {
                Action<T> action = (Action<T>)callbackItem;
                action(instance);
            }
        }

        public void UnRegister<T>(Action<T> action)
        {
        }

        public void Register<T>(Action<T> action)
        {
            if (_callbackList.ContainsKey(typeof(T)) == false)
                _callbackList.Add(typeof(T), new List<Delegate>());

            _callbackList[typeof(T)].Add(action);
        }

        public void Dispose()
        {
            _callbackList.Clear();
            _callbackList = null;
        }
    }
}
