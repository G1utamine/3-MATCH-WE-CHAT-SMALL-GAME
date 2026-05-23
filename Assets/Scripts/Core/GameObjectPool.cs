using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Match3.Core
{
    public class GameObjectPool<T> where T : Component
    {
        private static GameObjectPool<T> _instance;
        public static GameObjectPool<T> Instance => _instance ??= new GameObjectPool<T>();

        private readonly Dictionary<int, ObjectPool<T>> _pools = new Dictionary<int, ObjectPool<T>>();
        private Transform _poolRoot;

        private Transform GetPoolRoot()
        {
            if (_poolRoot == null)
            {
                var go = new GameObject($"[Pool_{typeof(T).Name}]");
                Object.DontDestroyOnLoad(go);
                _poolRoot = go.transform;
            }
            return _poolRoot;
        }

        /// <summary>
        /// 从池子中获取一个对象（已修正每次复用强制设置 Parent 的 Bug）
        /// </summary>
        public T Get(T prefab, Transform parent = null)
        {
            if (prefab == null) return null;

            int prefabId = prefab.gameObject.GetInstanceID();

            if (!_pools.ContainsKey(prefabId))
            {
                _pools[prefabId] = new ObjectPool<T>(
                    createFunc: () => Object.Instantiate(prefab),
                    actionOnGet: (obj) => {
                        if (obj != null) obj.gameObject.SetActive(true);
                    },
                    actionOnRelease: (obj) => {
                        if (obj != null)
                        {
                            obj.gameObject.SetActive(false);
                            obj.transform.SetParent(GetPoolRoot()); // 回收时放回池子根节点
                        }
                    },
                    actionOnDestroy: (obj) => {
                        if (obj != null) Object.Destroy(obj.gameObject);
                    },
                    collectionCheck: true,
                    defaultCapacity: 20,
                    maxSize: 100
                );
            }

            // 1. 从专属池里拿出一个（可能是全新的，也可能是回收的旧物体）
            T spawnedObj = _pools[prefabId].Get();

            // 2. 🔥【核心修正】无论它是新生产的还是复用的，只要你传了 parent，就强行再设置一遍父物体！
            if (parent != null)
            {
                // 第二个参数为 false，确保它在新的 UI 容器下能正确继承缩放和局部位移，不会乱飞
                spawnedObj.transform.SetParent(parent, false);
            }

            return spawnedObj;
        }

        public void Release(T prefab, T obj)
        {
            if (prefab == null || obj == null) return;

            int prefabId = prefab.gameObject.GetInstanceID();
            if (_pools.TryGetValue(prefabId, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                Object.Destroy(obj.gameObject);
            }
        }

        public void ClearAll()
        {
            foreach (var pool in _pools.Values) pool.Clear();
            _pools.Clear();
            if (_poolRoot != null) Object.Destroy(_poolRoot.gameObject);
        }
    }
}