using System;
using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using Core.Scripts.Helpers;
using Mechanics.World_Designer;
using UnityEngine.InputSystem;

public class PathfindingTest : MonoBehaviour
{
    public Transform StartPoint;
    public Transform EndPoint;
    
    [Header("Settings")]
    public float MaxSearchDistance = 50f;
    public float TurnPenalty = 5.0f;
    public LayerMask ObstacleLayer;

    private List<int2> _path;

    InputSystem_Actions actions;

    private void OnEnable()
    {
        actions = new InputSystem_Actions();
        actions.Enable();
    }

    private void OnDisable()
    {
        actions.Disable();
        actions.Dispose();
    }

    void Update()
    {
        if (actions.Player.Jump.IsPressed())
        {
            ExecutePathfinding();
        }
    }

    private void ExecutePathfinding()
    {
        // Конвертируем мировые координаты в целочисленные координаты сетки
        int2 start = new int2((int)StartPoint.position.x, (int)StartPoint.position.z);
        int2 end = new int2((int)EndPoint.position.x, (int)EndPoint.position.z);

        // Инициализируем алгоритм
        // Передаем GetNeighbors как делегат для сбора графа на лету
        var astar = new AStarPathfinding(start, end, MaxSearchDistance, TurnPenalty, GetNeighbors);

        _path = astar.FindPath();

        if (_path != null)
            Debug.Log($"Путь найден! Узлов: {_path.Count}");
        else
            Debug.LogWarning("Путь не найден или вне дистанции.");
    }

    // Твоя функция сбора графа (недетерминированное пространство)
    private void GetNeighbors(int2 center, int radius, Dictionary<int2, bool> cache)
    {
        // Сканируем область (например, 11x11)
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int2 point = center + new int2(x, y);
            
                // Если мы уже сканировали эту точку ранее, пропускаем
                if (cache.ContainsKey(point)) continue;

                var colliders = Physics.OverlapBox(new float3(point.x, 0, point.y), Vector3.one / 4.0f, Quaternion.identity,
                    LayerMask.GetMask("Default"),
                    QueryTriggerInteraction.Collide);
                // Тяжелая проверка (физика, Raycast или обращение к БД)
                bool isWalkable = colliders.Count(x => x.gameObject.CompareTag("Room")) == 0;
            
                cache[point] = isWalkable;
            }
        }
    }

    // Визуализация пути в Scene View
    private void OnDrawGizmos()
    {
        if (_path == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < _path.Count - 1; i++)
        {
            Vector3 from = new Vector3(_path[i].x,     0, _path[i].y);
            Vector3 to   = new Vector3(_path[i + 1].x, 0, _path[i + 1].y);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawSphere(from, 0.1f);
        }
    }
}