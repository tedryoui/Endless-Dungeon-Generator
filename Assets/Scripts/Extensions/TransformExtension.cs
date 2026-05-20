using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Extensions
{
    public static class TransformExtension
    {
        /// <summary>
        /// Возвращает список всех дочерних объектов в иерархии.
        /// </summary>
        /// <param name="parent">Родительский объект.</param>
        /// <param name="maxDepth">Максимальная глубина поиска. Если null — поиск до самого конца.</param>
        /// <returns>Список всех найденных Transform.</returns>
        public static List<Transform> GetChildren(this Transform parent, int? maxDepth = null)
        {
            List<Transform> result = new List<Transform>();
            AccumulateChildren(parent, result, 0, maxDepth);
            return result;
        }

        private static void AccumulateChildren(Transform current, List<Transform> list, int currentDepth, int? maxDepth)
        {
            // Проверяем, не достигли ли мы лимита глубины
            if (maxDepth.HasValue && currentDepth >= maxDepth.Value)
                return;

            foreach (Transform child in current)
            {
                list.Add(child);
                // Рекурсивно идем глубже
                AccumulateChildren(child, list, currentDepth + 1, maxDepth);
            }
        }
        
        /// <summary>
        /// Вычисляет центр объекта на основе MeshRenderer всех вложенных объектов.
        /// Аналогично режиму "Center" в Unity Editor Scene View.
        /// </summary>
        public static Vector3 GetSceneCenter(this Transform transform)
        {
            // Получаем все рендереры в объекте и его детях
            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                return transform.position;
            }

            // Инициализируем Bounds первым найденным рендерером
            Bounds bounds = renderers[0].bounds;

            // Расширяем Bounds, чтобы включить все остальные рендереры
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.center;
        }
        
        public static void Clear(this Transform transform)
        {
            if (transform == null || transform.childCount == 0) 
                return;

            var children = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                children[i] = transform.GetChild(i);

            foreach (var child in children)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
#if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(child.gameObject);
#else
                Object.DestroyImmediate(child.gameObject);
#endif
                }
            }
        }
    }
}