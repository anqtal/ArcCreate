using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class ParticlePool<T>
    where T : Component
{
    private readonly Transform parent;
    private readonly GameObject prefab;
    private int index;
    private T[] pool;

    public ParticlePool(GameObject prefab, Transform parent, int poolSize)
    {
        this.prefab = prefab;
        this.parent = parent;
        pool = new T[poolSize];
        for (var i = 0; i < poolSize; i++)
        {
            var go = Object.Instantiate(prefab, parent);
            pool[i] = go.GetComponent<T>();
        }
    }

    public T Get()
    {
        var result = pool[index];
        index += 1;
        if (index >= pool.Length) index = 0;

        return result;
    }

    public void Destroy()
    {
        for (var i = 0; i < pool.Length; i++)
            if (pool[i] != null)
                Object.Destroy(pool[i].gameObject);

        pool = new T[0];
    }

    public void Resize(int newPoolSize)
    {
        if (newPoolSize > pool.Length)
        {
            var newPool = new T[newPoolSize];
            Array.Copy(pool, newPool, pool.Length);
            for (var i = pool.Length; i < newPoolSize; i++)
            {
                var go = Object.Instantiate(prefab, parent);
                newPool[i] = go.GetComponent<T>();
            }

            pool = newPool;
        }
        else if (newPoolSize < pool.Length)
        {
            var newPool = new T[newPoolSize];
            Array.Copy(pool, newPool, newPoolSize);
            for (var i = newPoolSize; i < pool.Length; i++) Object.Destroy(pool[i].gameObject);

            pool = newPool;
        }
    }
}