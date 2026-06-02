using UnityEngine;
using UnityEngine.Pool;

namespace BFunCoreKit
{
    public abstract class PoolBase<T> : MonoBehaviour where T : Component
    {
        [Header("Pooling Settings")]
        [SerializeField] private T prefab;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxCapacity = 100;

        private ObjectPool<T> pool;

        protected virtual void Awake()
        {
            pool = new ObjectPool<T>(
                CreateFunc,
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyPooledObject,
                true,
                defaultCapacity,
                maxCapacity
            );
        }

        /// <summary>
        /// Spawn an object from the pool.
        /// </summary>
        public T Get()
        {
            return pool.Get();
        }

        /// <summary>
        /// Return object back to the pool.
        /// </summary>
        public void Release(T instance)
        {
            pool.Release(instance);
        }

        // =====================
        // Pooling Lifecycle
        // =====================

        protected virtual T CreateFunc()
        {
            var obj = Instantiate(prefab, transform);
            obj.gameObject.SetActive(false);
            return obj;
        }

        protected virtual void OnGetFromPool(T obj)
        {
            obj.gameObject.SetActive(true);
        }

        protected virtual void OnReleaseToPool(T obj)
        {
            obj.gameObject.SetActive(false);
        }

        protected virtual void OnDestroyPooledObject(T obj)
        {
            Destroy(obj.gameObject);
        }
    }
}
