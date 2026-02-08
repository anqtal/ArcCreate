using System.Collections.Generic;
using ArcCreate.Data;
using ArcCreate.Storage;
using UnityEngine;

namespace ArcCreate.Selection.Interface
{
    public class PlayResultList : MonoBehaviour
    {
        [SerializeField] private Transform parent;
        [SerializeField] private GameObject itemPrefab;
        private readonly List<PlayResultItem> items = new();

        public void Display(SongList level, Difficulty chart, IEnumerable<PlayResult> plays)
        {
            foreach (var item in items) Destroy(item.gameObject);

            items.Clear();

            foreach (var play in plays)
            {
                var go = Instantiate(itemPrefab, parent);
                var item = go.GetComponent<PlayResultItem>();
                item.Display(level, chart, play);
                items.Add(item);
            }
        }
    }
}