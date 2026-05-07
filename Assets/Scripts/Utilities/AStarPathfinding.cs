using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Core.Scripts.Helpers
{
    public class AStarPathfinding
    {

        public class PathNode : IComparable<PathNode>
        {
            public int2     Position;
            public PathNode Parent;
            public int2     Direction; // Направление прихода (int2)

            public float G;
            public float H;
            public float F => G + H;

            public PathNode(int2 pos)
            {
                Position = pos;
            }

            public int CompareTo(PathNode other)
            {
                int compare               = F.CompareTo(other.F);
                if (compare == 0) compare = H.CompareTo(other.H);
                return compare;
            }
        }

        private readonly int2 _startPos;
    private readonly int2 _endPos;
    private readonly float _maxDistanceSq;
    private readonly float _turnPenalty;
    
    // Внешняя функция теперь возвращает не просто список соседей, 
    // а заполняет доступность ячеек в радиусе.
    private readonly Action<int2, int, Dictionary<int2, bool>> _scanArea;
    
    // Кэш: позиция -> можно ли ходить (true/false)
    private Dictionary<int2, bool> _walkabilityCache = new Dictionary<int2, bool>();
    private const int SCAN_RADIUS = 5; // Радиус сканирования (дает сетку 11x11)

    public AStarPathfinding(
        int2 start, 
        int2 end, 
        float maxDistance, 
        float turnPenalty,
        Action<int2, int, Dictionary<int2, bool>> scanArea)
    {
        _startPos = start;
        _endPos = end;
        _maxDistanceSq = maxDistance * maxDistance;
        _turnPenalty = turnPenalty;
        _scanArea = scanArea;
    }

    public List<int2> FindPath()
    {
        var openSet = new List<PathNode>();
        var closedSet = new HashSet<int2>();

        openSet.Add(new PathNode(_startPos) { G = 0, H = math.distancesq((float2)_startPos, (float2)_endPos) });

        while (openSet.Count > 0)
        {
            openSet.Sort();
            PathNode current = openSet[0];
            openSet.RemoveAt(0);

            if (math.all(current.Position == _endPos))
                return RetracePath(current);

            closedSet.Add(current.Position);

            // Генерируем соседей (стандартный крест 3x3)
            foreach (int2 dir in GetFourDirections())
            {
                int2 neighborPos = current.Position + dir;

                if (closedSet.Contains(neighborPos) || 
                    math.distancesq((float2)_startPos, (float2)neighborPos) > _maxDistanceSq)
                    continue;

                // ПРОВЕРКА КЭША: Если мы еще не знаем про эту клетку — сканируем область вокруг неё
                if (!_walkabilityCache.ContainsKey(neighborPos))
                {
                    // Вызываем тяжелую функцию раз в 5-10 клеток
                    _scanArea(neighborPos, SCAN_RADIUS, _walkabilityCache);
                }

                // Теперь берем данные из кэша
                if (!_walkabilityCache[neighborPos]) continue;

                // Дальше стандартный A* ...
                float moveCost = 1f;
                float currentTurnPenalty = 0;
                if (!math.all(current.Direction == 0) && !math.all(dir == current.Direction))
                    currentTurnPenalty = _turnPenalty;

                float newG = current.G + moveCost + currentTurnPenalty;
                
                // (Обновление openSet как в предыдущем примере)
                ProcessNeighbor(openSet, current, neighborPos, dir, newG);
            }
        }
        return null;
    }

    private int2[] GetFourDirections() => new[] { 
        new int2(0, 1), new int2(0, -1), new int2(1, 0), new int2(-1, 0) 
    };

    private void ProcessNeighbor(List<PathNode> openSet, PathNode current, int2 pos, int2 dir, float newG)
    {
        PathNode node = openSet.Find(n => math.all(n.Position == pos));
        if (node == null)
        {
            openSet.Add(new PathNode(pos) {
                G = newG,
                H = math.distancesq((float2)pos, (float2)_endPos),
                Parent = current,
                Direction = dir
            });
        }
        else if (newG < node.G)
        {
            node.G = newG;
            node.Parent = current;
            node.Direction = dir;
        }
    }

        private List<int2> RetracePath(PathNode endNode)
        {
            var path = new List<int2>();
            var curr = endNode;
            while (curr != null)
            {
                path.Add(curr.Position);
                curr = curr.Parent;
            }

            path.Reverse();
            return path;
        }
    }
}