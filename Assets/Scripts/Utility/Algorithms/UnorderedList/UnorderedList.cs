using System.Collections.Generic;

namespace ArcCreate.Utility
{
    /// <summary>
    /// A list that does not keep a consistent order of elements, but has O(1) addition and removal.
    /// </summary>
    /// <remarks>
    /// Removing element is done by swapping the element with the last element, and deleting it.
    /// Therefore, iterating this list while removing elements should be done backward
    /// (i.e from the last element to the first element).
    /// </remarks>
    /// <typeparam name="T">The type of the list elements.</typeparam>
    public struct UnorderedList<T>
    {
        private List<T> list;

        public UnorderedList(int capacity)
        {
            list = new List<T>(capacity);
        }

        public int Count => list.Count;

        public T this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }

        public void Add(T element)
        {
            list.Add(element);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > list.Count - 1)
            {
                return;
            }

            list[index] = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
        }

        public bool Contains(T element)
        {
            return list.Contains(element);
        }

        public void Clear()
        {
            list.Clear();
        }
        
        /// <summary>
        /// Creates a deep copy of this UnorderedList.
        /// </summary>
        /// <returns>A new UnorderedList with the same elements.</returns>
        public UnorderedList<T> Clone()
        {
            UnorderedList<T> copy = new UnorderedList<T>(list.Count);
            copy.list = new List<T>(list); // 深拷贝列表
            return copy;
        }
    }
}