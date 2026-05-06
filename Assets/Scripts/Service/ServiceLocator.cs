using System;
using System.Collections.Generic;
using UnityEngine;

namespace Service
{
    public class ServiceLocator
    {
        [Serializable]
        public struct ServiceIdentity : IEquatable<ServiceIdentity>
        {
            public string ID;

            public bool Equals(ServiceIdentity other)
            {
                return ID == other.ID;
            }
        }

        private static GameObject _holderRoot;
        private static ServiceLocator _instance;

        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceLocator();
                    
                    _holderRoot = new GameObject("===SERVICES===");
                    GameObject.DontDestroyOnLoad(_holderRoot);
                }
                return _instance;
            }
        }
    
        private Dictionary<ServiceIdentity, IService> _services = new Dictionary<ServiceIdentity, IService>();

        public void RegisterService<T>(T service = null, ServiceIdentity? serviceIdentity = null)
            where T : class, IService, new()
        {
            var identity = serviceIdentity;
            var instance = service;

            if (identity == null)
            {
                identity = new ServiceIdentity()
                {
                    ID = typeof(T).Name
                };
            }

            if (instance == null)
            {
                instance = new T();
            }

            RegisterServiceCore(instance, identity.Value, typeof(T).Name);
        }

        public void RegisterService(IService service, ServiceIdentity? serviceIdentity = null)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var identity = serviceIdentity;
            if (identity == null)
            {
                identity = new ServiceIdentity
                {
                    ID = service.GetType().Name
                };
            }

            RegisterServiceCore(service, identity.Value, service.GetType().Name);
        }

        public void RegisterService(Type serviceType, ServiceIdentity? serviceIdentity = null)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (!typeof(IService).IsAssignableFrom(serviceType))
                throw new ArgumentException($"{serviceType.FullName} must implement {nameof(IService)}.", nameof(serviceType));
            if (serviceType.IsAbstract || serviceType.IsInterface)
                throw new ArgumentException($"{serviceType.FullName} must be a concrete type to be constructed.", nameof(serviceType));

            var identity = serviceIdentity;
            if (identity == null)
            {
                identity = new ServiceIdentity
                {
                    ID = serviceType.Name
                };
            }

            IService instance;
            try
            {
                instance = (IService)Activator.CreateInstance(serviceType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create instance of {serviceType.FullName}. A public parameterless constructor is required.", ex);
            }

            RegisterServiceCore(instance, identity.Value, serviceType.Name);
        }

        private void RegisterServiceCore(IService instance, ServiceIdentity identity, string duplicateWarningTypeName)
        {
            var hasSame = _services.TryGetValue(identity, out _);
            if (hasSame)
            {
                Debug.LogWarning($"You are trying to register a service twice! [{duplicateWarningTypeName}]");
                return;
            }

            instance.Initialize();
            _services.Add(identity, instance);
        }

        public void RemoveService(ServiceIdentity serviceIdentity)
        {
            var identity = serviceIdentity;

            var hasSame = _services.TryGetValue(identity, out var other);
            if (!hasSame)
            {
                Debug.LogWarning($"You are trying to remove unexisting service [{identity.ID}]");
                return;
            }

            other.Dispose();
            _services.Remove(identity);
        }

        public void RemoveService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            RemoveService(new ServiceIdentity { ID = serviceType.Name });
        }

        public void BindToRoot(GameObject serviceRoot)
        {
            if (serviceRoot == null)
                throw new ArgumentNullException(nameof(serviceRoot));
            
            serviceRoot.transform.SetParent(_holderRoot.transform);
        }

        public T GetService<T>(ServiceIdentity identity)
        {
            if (_services.TryGetValue(identity, out var service))
            {
                return (T)service;
            }
            
            throw new KeyNotFoundException($"No service of type {typeof(T).Name} was found.");
        }
    }
}
