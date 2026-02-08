// Credit: https://stackoverflow.com/questions/754233/is-it-there-any-lru-implementation-of-idictionary#3719378

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ArcCreate.Utility.LRUCache
{
    public class LRUCache<K, V>
    {
        private readonly Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>> cacheMap = new();
        private readonly int capacity;
        private readonly LinkedList<LRUCacheItem<K, V>> lruList = new();
        private readonly Action<V> onRemove;

        public LRUCache(int capacity, Action<V> onRemove = null)
        {
            this.capacity = capacity;
            this.onRemove = onRemove;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public V Get(K key)
        {
            if (cacheMap.TryGetValue(key, out var node))
            {
                var value = node.Value.Value;
                lruList.Remove(node);
                lruList.AddLast(node);
                return value;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(K key, V val)
        {
            if (cacheMap.Count >= capacity) RemoveFirst();

            var cacheItem = new LRUCacheItem<K, V>(key, val);
            var node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);
            lruList.AddLast(node);
            cacheMap.Add(key, node);
        }

        private void RemoveFirst()
        {
            // Remove from LRUPriority
            var node = lruList.First;
            lruList.RemoveFirst();

            // Remove from cache
            cacheMap.Remove(node.Value.Key);
            onRemove?.Invoke(node.Value.Value);
        }
    }
}