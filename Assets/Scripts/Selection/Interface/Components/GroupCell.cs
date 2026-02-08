using System.Threading;
using ArcCreate.Selection.SoundEffect;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArcCreate.Selection.Interface
{
    public class GroupCell : Cell, IPointerClickHandler
    {
        [SerializeField] private StorageData storage;
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform icon;
        [SerializeField] private GameObject expandedIcon;
        [SerializeField] private GameObject collapsedIcon;
        [SerializeField] private float offsetLeft;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (storage != null && storage.IsTransitioning) return;

            ToggleCollapse();
            expandedIcon.SetActive(!HierarchyData.IsCollapsed);
            collapsedIcon.SetActive(HierarchyData.IsCollapsed);
            Services.SoundEffect.Play(Sound.CellSelect);
        }

        public override UniTask LoadCellFully(CellData cellData, CancellationToken cancellationToken)
        {
            return default;
        }

        public override void SetCellData(CellData cellData)
        {
            var groupCellData = cellData as GroupCellData;
            text.text = groupCellData.Title;
            var textWidth = text.preferredWidth;
            icon.anchoredPosition = new Vector2(
                textWidth + offsetLeft,
                icon.anchoredPosition.y);
            expandedIcon.SetActive(!HierarchyData.IsCollapsed);
            collapsedIcon.SetActive(HierarchyData.IsCollapsed);
        }
    }
}