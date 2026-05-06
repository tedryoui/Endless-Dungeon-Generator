using System;
using System.Collections.Generic;

namespace Core.Scripts.Helpers
{
    public class EventBus
    {
        private static EventBus _instance;
        public static EventBus Instance => _instance ??= new EventBus();

        private readonly Dictionary<Type, List<Delegate>> _eventHandlers = new();

        private EventBus() { }

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);
            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Delegate>();
            }

            _eventHandlers[eventType].Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType].Remove(handler);
                if (_eventHandlers[eventType].Count == 0)
                {
                    _eventHandlers.Remove(eventType);
                }
            }
        }

        public void Publish<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            if (_eventHandlers.TryGetValue(eventType, out var actions))
            {
                foreach (var handler in actions.ToArray())
                {
                    if (handler is Action<T> action)
                    {
                        action(eventData);
                    }
                }
            }
        }
    }
}