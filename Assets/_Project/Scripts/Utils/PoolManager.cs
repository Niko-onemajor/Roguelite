using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>静态对象池：Spawn(clone)→Return(归池)；未携带 Poolable 的直接销毁。</summary>
    public static partial class PoolManager
    {
        static readonly Dictionary<int, Queue<GameObject>> pool = new Dictionary<int, Queue<GameObject>>();

        public static GameObject Spawn(GameObject prototype, Vector3 pos, Quaternion rot)
        {
            if (prototype == null) return null;
            int key = prototype.GetInstanceID();
            if (!pool.TryGetValue(key, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pool[key] = queue;
            }

            GameObject go = null;
            while (queue.Count > 0)
            {
                GameObject candidate = queue.Dequeue();
                if (candidate != null) { go = candidate; break; } // 跳过已被销毁的残留
            }
            if (go == null) go = Object.Instantiate(prototype);
            var poolable = go.GetComponent<Poolable>();
            if (poolable != null) poolable.Key = key;
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }

        public static void Return(GameObject go)
        {
            if (go == null) return;
            var poolable = go.GetComponent<Poolable>();
            if (poolable == null || poolable.Key == 0)
            {
                Object.Destroy(go);
                return;
            }
            go.SetActive(false);
            if (go.TryGetComponent(out Rigidbody2D rb) && rb.bodyType == RigidbodyType2D.Dynamic)
                rb.velocity = Vector2.zero;
            if (pool.TryGetValue(poolable.Key, out Queue<GameObject> queue))
                queue.Enqueue(go);
            else
                pool[poolable.Key] = new Queue<GameObject>(new[] { go });
        }

        public static void ClearAll()
        {
            foreach (var pair in pool)
                foreach (var go in pair.Value)
                    if (go != null) Object.DestroyImmediate(go); // EditMode 测试中 Destroy 会报错，统一用即时销毁
            pool.Clear();
        }
    }
}