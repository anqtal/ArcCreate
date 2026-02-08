using DG.Tweening;
using UnityEngine;

namespace ArcCreate.Utility.Animation
{
    public class ScriptedAnimator : MonoBehaviour
    {
        [SerializeField] private ScriptedAnimatorComponent[] components = new ScriptedAnimatorComponent[0];
        [SerializeField] private bool disableGameObject;
        private bool isSetup;

        public float Length { get; private set; }

        public bool IsShown { get; private set; }

        private void Awake()
        {
            SetupComponents();
        }

        public void Reset()
        {
            if (!isSetup) SetupComponents();

            foreach (var c in components) c.Reset();
        }

        public void ShowImmediate()
        {
            foreach (var c in components) c.ShowImmediate();
        }

        public void HideImmediate()
        {
            foreach (var c in components) c.HideImmediate();
        }

        public void Show()
        {
            if (disableGameObject) gameObject.SetActive(true);

            IsShown = true;
            GetShowTween(out var _).Play();
        }

        public void Hide()
        {
            GetHideTween(out var _).Play().OnComplete(() =>
            {
                if (disableGameObject) gameObject.SetActive(false);

                IsShown = false;
            });
        }

        public void RegisterDefaultValues()
        {
            if (!isSetup) SetupComponents();

            foreach (var c in components) c.RegisterDefaultValues();
        }

        public Tween GetShowTween(out float duration)
        {
            if (!isSetup) SetupComponents();

            var sequence = DOTween.Sequence();
            foreach (var c in components) sequence = sequence.Insert(0, c.GetShowTween());

            duration = Length;
            return sequence;
        }

        public Tween GetHideTween(out float duration)
        {
            if (!isSetup) SetupComponents();

            var sequence = DOTween.Sequence();
            foreach (var c in components) sequence = sequence.Insert(0, c.GetHideTween());

            duration = Length;
            return sequence;
        }

        public void SetupComponents()
        {
            foreach (var c in components)
            {
                c.SetupComponents();
                Length = Mathf.Max(Length, c.AnimationLength);
            }

            isSetup = true;
        }
    }
}