using System;
using System.Collections.Generic;
using Core.Scripts.Helpers;
using Events;
using UnityEngine;

namespace Service
{
    public class ServiceBootstrapModule : MonoBehaviour
    {
        [Serializable]
        public struct ServiceRegistration
        {
            public enum RegistrationMode { UseMonoService, UseConstructedService }
            
            public ServiceLocator.ServiceIdentity Identity;
            public RegistrationMode Mode;
            public MonoService MonoService;
            
            [SerializeField] private AssemblyTypeNameEnum _assemblyTypeName;
            public string AssemblyTypeName => AssemblyTypeNameFabric.Resolve(_assemblyTypeName);
        }
        
        public List<ServiceRegistration> Registrations;

        private void Awake()
        {
            EventBus.Instance.Subscribe<ProjectBootstrapedEvent>(RegisterServices);
            EventBus.Instance.Subscribe<ProjectCloseEvent>(RemoveServices);
        }

        private void RemoveServices(ProjectCloseEvent data)
        {
            foreach (var registration in Registrations)
            {
                var rData = registration;
                var identity = rData.Identity;
                
                ServiceLocator.Instance.RemoveService(identity);
                
                if (rData.Mode is ServiceRegistration.RegistrationMode.UseMonoService)
                    Destroy(rData.MonoService.gameObject);
            }
            
            EventBus.Instance.Unsubscribe<ProjectCloseEvent>(RemoveServices);
        }

        private void RegisterServices(ProjectBootstrapedEvent data)
        {
            foreach (var registration in Registrations)
            {
                var rData = registration;
                var identity = rData.Identity;

                if (rData.Mode is ServiceRegistration.RegistrationMode.UseMonoService)
                {
                    ServiceLocator.Instance.RegisterService(rData.MonoService, identity);
                    ServiceLocator.Instance.BindToRoot(rData.MonoService.gameObject);
                }
                else
                {
                    var t = Type.GetType(rData.AssemblyTypeName);
                    if (t == null)
                    {
                        Debug.LogError(
                            $"ServiceBootstrapModule: could not resolve type '{rData.AssemblyTypeName}' for identity '{identity.ID}'.");
                        continue;
                    }

                    ServiceLocator.Instance.RegisterService(t, identity);
                }
            }
            
            EventBus.Instance.Unsubscribe<ProjectBootstrapedEvent>(RegisterServices);
        }
    }
}