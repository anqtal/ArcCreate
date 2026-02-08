using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcCreate.Utility.InfiniteScroll
{
    public class InfiniteScroll : MonoBehaviour, IDragHandler
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float marginTop;
        [SerializeField] private float marginBottom;
        [SerializeField] private float marginLeft;
        [SerializeField] private float marginRight;
        [SerializeField] private float spacing;
        [SerializeField] private RectTransform.Axis axis;
        [SerializeField] private bool useTwoStageLoading;
        [SerializeField] private float maxVelocityForSecondStage;

        private readonly List<Cell> visibleCells = new();
        private RectTransform containerRect;
        private RectTransform contentRect;
        private Vector2 previousContentRectPosition;
        private bool setup;

        public float Value
        {
            get =>
                IsVertical
                    ? contentRect.anchoredPosition.y + containerRect.rect.height / 2
                    : -contentRect.anchoredPosition.x + containerRect.rect.width / 2;

            set
            {
                if (Hierarchy.Count == 0) return;

                if (IsVertical)
                {
                    var halfContainerHeight = containerRect.rect.height / 2;
                    var contentHeight = contentRect.rect.height;
                    var max = contentHeight < containerRect.rect.height
                        ? halfContainerHeight
                        : contentHeight - halfContainerHeight;
                    value = Mathf.Clamp(value, halfContainerHeight, max);
                    contentRect.anchoredPosition = new Vector2(
                        contentRect.anchoredPosition.x,
                        value - halfContainerHeight);
                }
                else
                {
                    var halfContainerWidth = containerRect.rect.width / 2;
                    var contentWidth = contentRect.rect.width;
                    var max = contentWidth < containerRect.rect.width
                        ? halfContainerWidth
                        : contentWidth - halfContainerWidth;
                    value = Mathf.Clamp(value, halfContainerWidth, max);
                    contentRect.anchoredPosition = new Vector2(
                        -value + containerRect.rect.width / 2,
                        contentRect.anchoredPosition.y);
                }
            }
        }

        /// <summary>
        ///     User defined cell data.
        /// </summary>
        public List<CellData> Data { get; } = new();

        /// <summary>
        ///     Store hierarchy data necessary for rendering. Should be a 1-1 correspondance with dataSource.
        /// </summary>
        public List<HierarchyData> Hierarchy { get; } = new();

        private bool IsVertical => axis == RectTransform.Axis.Vertical;

        private void Awake()
        {
            scrollRect.onValueChanged.AddListener(OnScroll);
            contentRect = scrollRect.content;
            containerRect = scrollRect.GetComponent<RectTransform>();

            if (IsVertical)
            {
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
            }
            else
            {
                contentRect.anchorMin = new Vector2(0, 0);
                contentRect.anchorMax = new Vector2(0, 1);
            }

            contentRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = Vector2.zero;
            setup = true;
        }

        private void Update()
        {
            if (useTwoStageLoading)
            {
                var velocity = contentRect.anchoredPosition - previousContentRectPosition;
                velocity /= Time.deltaTime;
                LoadSecondStage(IsVertical ? velocity.y : velocity.x);

                previousContentRectPosition = contentRect.anchoredPosition;
            }
        }

        private void OnDestroy()
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnPointerEvent?.Invoke();
        }

        public event Action OnPointerEvent;

        public void SetData(List<CellData> data)
        {
            SetDataWithoutRebuild(data);
            Rebuild();
            LoadSecondStage(0);
        }

        public void SetDataWithoutRebuild(List<CellData> data)
        {
            if (!setup) Awake();

            this.Data.Clear();
            Hierarchy.Clear();

            foreach (var cell in visibleCells) cell.CellData.Pool.Return(cell);

            visibleCells.Clear();

            foreach (var cellData in data) AddCell(cellData);

            RecalculateCellsState();
        }

        /// <summary>
        ///     Collapse a cell. Any children cell will be hidden from view.
        /// </summary>
        /// <param name="cellIndex">Flat index of cell.</param>
        public void ToggleCollapse(int cellIndex)
        {
            Hierarchy[cellIndex].IsCollapsed = !Hierarchy[cellIndex].IsCollapsed;
            RecalculateCellsState();
            Rebuild(true);
        }

        public void OnDrag(BaseEventData eventData)
        {
            OnPointerEvent?.Invoke();
        }

        /// <summary>
        ///     Add cell to cells list along with their children.
        /// </summary>
        private void AddCell(CellData cellData, int parent = -1, int indent = 0)
        {
            var index = Data.Count;

            Data.Add(cellData);
            Hierarchy.Add(new HierarchyData(index, cellData.Size, indent, parent)
            {
                IsCollapsed = cellData.CollapsedByDefault,
                SecondStageStarted = false
            });

            if (cellData.Children != null)
                foreach (var child in cellData.Children)
                    AddCell(child, index, indent + 1);
        }

        private void OnScroll(Vector2 val)
        {
            Rebuild();
        }

        private void RecalculateCellsState()
        {
            var positionSoFar = IsVertical ? marginTop : marginLeft;
            for (var i = 0; i < Hierarchy.Count; i++)
            {
                var item = Hierarchy[i];
                item.IsVisible = IsVisible(i);
                item.PositionInRect = positionSoFar;
                if (item.IsVisible) positionSoFar += item.Size + spacing;
            }

            positionSoFar += IsVertical ? marginBottom : marginRight;
            contentRect.SetSizeWithCurrentAnchors(axis, positionSoFar);

            for (var i = 0; i < visibleCells.Count; i++)
            {
                var cell = visibleCells[i];
                var item = cell.HierarchyData;
                ApplyCellRect(cell, item);
            }
        }

        private bool IsVisible(int index)
        {
            var cell = Hierarchy[index];
            if (cell.ParentIndex == -1) return true;

            var parent = Hierarchy[cell.ParentIndex];
            if (parent.IsCollapsed) return false;

            return IsVisible(cell.ParentIndex);
        }

        public void Rebuild(bool checkInbetween = false)
        {
            var minVisiblePositionInRect = IsVertical
                ? contentRect.anchoredPosition.y
                : -contentRect.anchoredPosition.x;
            var maxVisiblePositionInRect = IsVertical
                ? contentRect.anchoredPosition.y + containerRect.rect.height
                : -contentRect.anchoredPosition.x + containerRect.rect.width;

            if (visibleCells.Count == 0)
            {
                for (var i = 0; i <= Hierarchy.Count - 1; i++)
                {
                    var item = Hierarchy[i];
                    var cellVisible = item.PositionInRect + item.Size >= minVisiblePositionInRect
                                      && item.PositionInRect <= maxVisiblePositionInRect;
                    if (!cellVisible || !item.IsVisible) continue;

                    var cellData = Data[i];
                    var cell = cellData.Pool.Get(contentRect);
                    ApplyCell(cell, cellData, item);
                    visibleCells.Add(cell);
                }

                return;
            }

            var minVisibleIndex = Hierarchy.Count;
            var maxVisibleIndex = -1;

            for (var i = visibleCells.Count - 1; i >= 0; i--)
            {
                var cell = visibleCells[i];
                var item = cell.HierarchyData;
                var cellVisible = item.PositionInRect + item.Size >= minVisiblePositionInRect
                                  && item.PositionInRect <= maxVisiblePositionInRect;

                if (cellVisible)
                {
                    minVisibleIndex = Mathf.Min(minVisibleIndex, item.IndexFlat);
                    maxVisibleIndex = Mathf.Max(maxVisibleIndex, item.IndexFlat);
                }

                if (!cellVisible || !item.IsVisible)
                {
                    visibleCells.RemoveAt(i);
                    cell.CellData.Pool.Return(cell);
                    cell.CancelLoadCellFully();
                    item.SecondStageStarted = false;
                    item.IsFullyLoaded = false;
                }
            }

            if (checkInbetween)
                for (var i = minVisibleIndex; i < maxVisibleIndex; i++)
                {
                    var item = Hierarchy[i];
                    var cellVisible = item.PositionInRect + item.Size >= minVisiblePositionInRect
                                      && item.PositionInRect <= maxVisiblePositionInRect;

                    if (!item.IsVisible || !cellVisible) continue;

                    var isAlreadyVisible = false;
                    foreach (var visible in visibleCells)
                        if (visible.HierarchyData.IndexFlat == i)
                        {
                            isAlreadyVisible = true;
                            break;
                        }

                    if (!isAlreadyVisible)
                    {
                        var cellData = Data[i];
                        var cell = cellData.Pool.Get(contentRect);
                        ApplyCell(cell, cellData, item);
                        visibleCells.Add(cell);
                    }
                }

            for (var i = minVisibleIndex - 1; i >= 0; i--)
            {
                var item = Hierarchy[i];
                var cellVisible = item.PositionInRect + item.Size >= minVisiblePositionInRect
                                  && item.PositionInRect <= maxVisiblePositionInRect;
                if (!cellVisible) break;

                if (!item.IsVisible) continue;

                var cellData = Data[i];
                var cell = cellData.Pool.Get(contentRect);
                ApplyCell(cell, cellData, item);
                visibleCells.Add(cell);
            }

            for (var i = maxVisibleIndex + 1; i < Hierarchy.Count; i++)
            {
                var item = Hierarchy[i];
                var cellVisible = item.PositionInRect + item.Size >= minVisiblePositionInRect
                                  && item.PositionInRect <= maxVisiblePositionInRect;
                if (!cellVisible) break;

                if (!item.IsVisible) continue;

                var cellData = Data[i];
                var cell = cellData.Pool.Get(contentRect);
                ApplyCell(cell, cellData, item);
                visibleCells.Add(cell);
            }
        }

        private void ApplyCell(Cell cell, CellData cellData, HierarchyData hierarchyData)
        {
            ApplyCellRect(cell, hierarchyData);
            cell.Scroll = this;
            cell.HierarchyData = hierarchyData;
            cell.CellData = cellData;
            cell.SetCellData(cellData);
        }

        private void ApplyCellRect(Cell cell, HierarchyData hierarchyData)
        {
            if (IsVertical)
            {
                cell.RectTransform.anchorMin = new Vector2(0, 1);
                cell.RectTransform.anchorMax = new Vector2(1, 1);
                cell.RectTransform.offsetMin = new Vector2(marginLeft, 0);
                cell.RectTransform.offsetMax = new Vector2(marginRight, 0);
                cell.RectTransform.anchoredPosition = new Vector2(0, -hierarchyData.PositionInRect);
            }
            else
            {
                cell.RectTransform.anchorMin = new Vector2(0, 0);
                cell.RectTransform.anchorMax = new Vector2(0, 1);
                cell.RectTransform.offsetMin = new Vector2(0, marginBottom);
                cell.RectTransform.offsetMax = new Vector2(0, marginTop);
                cell.RectTransform.anchoredPosition = new Vector2(hierarchyData.PositionInRect, 0);
            }

            cell.RectTransform.SetSizeWithCurrentAnchors(axis, hierarchyData.Size);
        }

        private void LoadSecondStage(float velocity)
        {
            if (Mathf.Abs(velocity) > maxVelocityForSecondStage) return;

            var minVisiblePositionInRect = IsVertical
                ? contentRect.anchoredPosition.y
                : -contentRect.anchoredPosition.x;
            var maxVisiblePositionInRect = IsVertical
                ? contentRect.anchoredPosition.y + containerRect.rect.height
                : -contentRect.anchoredPosition.x + containerRect.rect.width;

            foreach (var cell in visibleCells)
            {
                var predictedMinVisibleAfterLoad = minVisiblePositionInRect + velocity * cell.PredictedLoadTime;
                var predictedMaxVisibleAfterLoad = maxVisiblePositionInRect + velocity * cell.PredictedLoadTime;

                var item = cell.HierarchyData;
                var willBeVisible = item.PositionInRect + item.Size >= predictedMinVisibleAfterLoad
                                    && item.PositionInRect <= predictedMaxVisibleAfterLoad;

                if (willBeVisible && !item.SecondStageStarted)
                {
                    cell.SetCellDataFully(cell.CellData).Forget();
                    item.SecondStageStarted = true;
                }

                if (!willBeVisible && item.SecondStageStarted && !item.IsFullyLoaded)
                {
                    cell.CancelLoadCellFully();
                    item.SecondStageStarted = false;
                }
            }
        }
    }
}