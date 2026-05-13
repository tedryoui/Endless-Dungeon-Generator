using System;
using Core.Scripts.Helpers;
using Events;
using Service;
using Service.Concrete;
using Unity.Mathematics;
using UnityEngine;

namespace Entry
{
    public class DefaultEntryPoint : MonoBehaviour
    {
        private WorldDesignService _worldDesignService;
        private UserInterfaceService _userInterfaceService;
        
        private void Awake()
        {
            EventBus.Instance.Subscribe<WorldBuildedEvent>(OnWorldBuilded);
            EventBus.Instance.Subscribe<ProjectCloseEvent>(OnProjectClosed);
        }

        private void OnProjectClosed(ProjectCloseEvent obj)
        {
            if (_worldDesignService != null) _worldDesignService.onWorldChanged -= OnWorldChanged;
        }

        private void OnWorldBuilded(WorldBuildedEvent obj)
        {
            _worldDesignService = ServiceLocator.Instance.GetService<WorldDesignService>(new ServiceLocator.ServiceIdentity
            {
                ID = "World Design Service"
            });
            
            _userInterfaceService = ServiceLocator.Instance.GetService<UserInterfaceService>(new ServiceLocator.ServiceIdentity()
            {
                ID = "User Interface Service"
            });

            _worldDesignService.onWorldChanged += OnWorldChanged;
            RefreshMap();
            
            EventBus.Instance.Unsubscribe<WorldBuildedEvent>(OnWorldBuilded);
        }

        private void OnWorldChanged()
        {
            RefreshMap();
        }

        private void RefreshMap()
        {
            var matrix = _worldDesignService.GetWorldMatrixPartition(new int3(-16, 0, -16), new int3(16, 0, 16), out var size);
            
            _userInterfaceService.GUIViewModel.RefreshMap(matrix, size);   
        }
    }
}