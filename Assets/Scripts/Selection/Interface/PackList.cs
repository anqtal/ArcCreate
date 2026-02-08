using System;
using System.Linq;
using ArcCreate.SceneTransition;
using ArcCreate.Storage;
using ArcCreate.Utility.Animation;
using ArcCreate.Utility.InfiniteScroll;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    public class PackList : MonoBehaviour
    {
        private static bool lastWasInPackList = true;

        [SerializeField] private StorageData storageData;
        [SerializeField] private InfiniteScroll scroll;
        [SerializeField] private GameObject packCellPrefab;
        [SerializeField] private float packCellSize;
        [SerializeField] private float autoScrollDuration = 0.3f;
        [SerializeField] private ScriptedAnimator packListAnimator;
        [SerializeField] private ScriptedAnimator levelListAnimator;
        [SerializeField] private ScriptedAnimator hideUIAnimator;
        [SerializeField] private Transform listButtonsParent;
        [SerializeField] private Transform packButtonsParent;
        [SerializeField] private CanvasGroup packListCanvasGroup;
        [SerializeField] private Button backToPackListButton;
        [SerializeField] private Button allSongsPack;
        [SerializeField] private Button loadChartsPack;
        private Pool<Cell> packCellPool;
        private Tween scrollTween;

        private void Awake()
        {
            if (scroll == null || packCellPrefab == null)
            {
                Debug.LogWarning("PackList missing scroll or packCellPrefab reference.");
                return;
            }

            packCellPool = Pools.New<Cell>("PackCell", packCellPrefab, scroll.transform, 5);

            storageData.OnStorageChange += RebuildList;
            storageData.OnSwitchToGameplayScene += HideUI;
            storageData.SelectedPack.OnValueChange += OnSelectedPack;
            if (backToPackListButton != null) backToPackListButton.onClick.AddListener(BackToPackList);

            if (allSongsPack != null) allSongsPack.onClick.AddListener(SelectAllSongsPack);

            if (loadChartsPack != null) loadChartsPack.onClick.AddListener(OpenChartPicker);
            storageData.OnSwitchToGameplaySceneException += OnGameplayException;

            if (storageData.IsLoaded) RebuildList();

            if (lastWasInPackList)
                StartupAnimation().Forget();
            else
                OnSelectedPack(storageData.SelectedPack.Value);
        }

        private void OnDestroy()
        {
            Pools.Destroy<Cell>("PackCell");

            Settings.SelectionSortPackStrategy.OnValueChanged.RemoveListener(OnSortChange);
            storageData.OnStorageChange -= RebuildList;
            storageData.OnSwitchToGameplayScene -= HideUI;
            storageData.SelectedPack.OnValueChange -= OnSelectedPack;
            if (backToPackListButton != null) backToPackListButton.onClick.RemoveListener(BackToPackList);

            if (allSongsPack != null) allSongsPack.onClick.RemoveListener(SelectAllSongsPack);

            if (loadChartsPack != null) loadChartsPack.onClick.RemoveListener(OpenChartPicker);
            storageData.OnSwitchToGameplaySceneException -= OnGameplayException;
        }

        public void BackToPackList()
        {
            packListAnimator.Show();
            levelListAnimator.Hide();
            packButtonsParent.SetAsLastSibling();
            packListCanvasGroup.interactable = true;
            packListCanvasGroup.blocksRaycasts = true;
            lastWasInPackList = true;
        }

        private async UniTask StartupAnimation()
        {
            Settings.SelectionSortPackStrategy.OnValueChanged.AddListener(OnSortChange);
            hideUIAnimator.HideImmediate();
            packListAnimator.HideImmediate();
            await UniTask.DelayFrame(2);
            packListAnimator.Show();
            hideUIAnimator.Show();
            levelListAnimator.HideImmediate();
            packListCanvasGroup.interactable = true;
            packListCanvasGroup.blocksRaycasts = true;
            lastWasInPackList = true;
        }

        private void OnSortChange(string arg0)
        {
            RebuildList();
        }

        private void SelectAllSongsPack()
        {
            var wasNull = storageData.SelectedPack.Value == null;
            storageData.SelectedPack.Value = null;
            if (wasNull) OnSelectedPack(null);
        }

        private void OnGameplayException(Exception e)
        {
            ShowUI();
        }

        private void ShowUI()
        {
            if (lastWasInPackList)
                packListAnimator.Show();
            else
                levelListAnimator.Show();

            hideUIAnimator.Show();
            new TransitionSequence()
                .AddTransition(new TriangleTileTransition())
                .Show().Forget();
        }

        private void HideUI()
        {
            if (packListAnimator.IsShown) packListAnimator.Hide();

            if (levelListAnimator.IsShown) levelListAnimator.Hide();

            hideUIAnimator.Hide();
        }

        private void OpenChartPicker()
        {
            storageData.NotifyOpenFilePicker();
        }

        private void OnSelectedPack(Pack pack)
        {
            if (pack != null && StorageData.GetSongsForPack(pack).Count == 0) return;

            packListAnimator.Hide();
            levelListAnimator.Show();
            listButtonsParent.SetAsLastSibling();
            packListCanvasGroup.interactable = false;
            packListCanvasGroup.blocksRaycasts = false;
            lastWasInPackList = false;
        }

        private void RebuildList()
        {
            var packs = StorageData.GetAllPacks().ToList();
            var sortPack = GetSortPackStrategy(Settings.SelectionSortPackStrategy.Value);
            var data = packs.Select((pack, index) => new PackCellData
            {
                Pack = pack,
                PackIndex = index,
                Pool = packCellPool,
                Size = packCellSize
            }).ToList();

            scroll.SetData(sortPack.Sort(data).ToList<CellData>());
            FocusOnPack(storageData.SelectedPack.Value);
        }

        private static ISortPackStrategy GetSortPackStrategy(string value)
        {
            return new SortPackByName();
            // switch(value)
            // {
            //     case SortPackByName.Typename:
            //         return new SortPackByName();
            //     case SortPackByPublisher.Typename:
            //         return new SortPackByPublisher();
            //     case SortPackByAddedDate.Typename:
            //         return new SortPackByAddedDate();
            //     default:
            //         return new SortPackByName();
            // }
        }

        private void FocusOnPack(Pack pack)
        {
            var scrollFrom = scroll.Value;
            float scrollTo = 0;
            if (pack != null)
            {
                for (var i = 0; i < scroll.Data.Count; i++)
                {
                    var data = scroll.Data[i];
                    if (data is PackCellData packCell && packCell.Pack != null && packCell.Pack.id == pack.id)
                    {
                        scrollTo = scroll.Hierarchy[i].ValueToCenterCell;
                        break;
                    }
                }

                scrollTween?.Kill();
                scrollTween = DOTween.To(val => scroll.Value = val, scrollFrom, scrollTo, autoScrollDuration)
                    .SetEase(Ease.OutExpo);
            }
        }
    }
}