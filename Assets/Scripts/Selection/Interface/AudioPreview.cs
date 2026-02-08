using ArcCreate.Gameplay.Audio;
using ArcCreate.Storage;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ArcCreate.Selection.Interface
{
    public class AudioPreview : MonoBehaviour
    {
        [SerializeField] private StorageData storage;
        private string lastSongId;

        private void Awake()
        {
            storage.SelectedChart.OnValueChange += OnChartChange;
            storage.OnStorageChange += OnStorageChange;

            if (storage.IsLoaded) OnStorageChange();
        }

        private void OnDestroy()
        {
            storage.SelectedChart.OnValueChange -= OnChartChange;
            storage.OnStorageChange -= OnStorageChange;
        }

        private void OnStorageChange()
        {
            OnChartChange(storage.SelectedChart.Value);
        }

        private void OnChartChange((SongList level, Difficulty difficulty) obj)
        {
            var level = obj.level;
            if (level == null) return;
            if (level.id == lastSongId) return;

            lastSongId = level.id;
            BassAudioService.Instance.PlayAudioPreview(level.id).Forget();
        }
    }
}