using System;
using Core.Scripts.Helpers;
using Events;
using UnityEngine;

namespace Service
{
    public class Bootstrap : MonoBehaviour
    {
        public void Start()
        {
            DontDestroyOnLoad(gameObject);
            
            EventBus.Instance.Publish(new ProjectBootstrapedEvent());
            
            EventBus.Instance.Publish(new ServicesRegisteredEvent(""));
            
            EventBus.Instance.Publish(new WorldBuildedEvent(""));
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            EventBus.Instance.Publish(new ProjectCloseEvent());   
        }
#else
        private void OnApplicationQuit()
        {
            EventBus.Instance.Publish(new ProjectCloseEvent());   
        }
#endif
    }
}