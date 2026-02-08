using System.Threading;
using ArcCreate.Selection.SoundEffect;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    public class PackCell : Cell
    {
        [SerializeField] private StorageData storage;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text title;
        [SerializeField] private RawImage image;

        private Pack pack;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(SelectSelf);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(SelectSelf);
        }

        public override void SetCellData(CellData cellData)
        {
            var data = cellData as PackCellData;
            pack = data.Pack;
            title.text = pack?.name ?? string.Empty;

            if (storage.TryAssignPackJacketFromCache(image, pack)) MarkFullyLoaded();
        }

        public override async UniTask LoadCellFully(CellData cellData, CancellationToken cancellationToken)
        {
            await StorageData.AssignPackJacket(image, pack);
        }

        private void SelectSelf()
        {
            if (storage.IsTransitioning) return;

            storage.SelectedPack.Value = pack;
            Services.SoundEffect.Play(Sound.CellSelect);
        }
    }
}