using System;
using System.Threading;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Utility.Animation;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    public class OffsetCalibrateDialog : MonoBehaviour
    {
        private const double PlayDelay = 1.25;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Button mainButton;
        [SerializeField] private RectTransform mainButtonRect;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        //[SerializeField] private AudioSource audioSource;
        [SerializeField] private int[] expectedHitTimings;
        [SerializeField] private TMP_Text[] offsetTexts;
        [SerializeField] private Toggle[] offsetToggles;
        //[SerializeField] private AudioPreview audioPreview;
        [SerializeField] private ScriptedAnimator hitAnimator;
        [SerializeField] private ScriptedAnimator dialogAnimator;
        private CancellationTokenSource cts = new();
        private bool confirmPressed;
        private bool retryPressed;

        private void Awake()
        {
            mainButton.onClick.AddListener(StartCalibration);
            confirmButton.onClick.AddListener(ConfirmCalibration);
            cancelButton.onClick.AddListener(CancelCalibration);
            confirmButton.interactable = false;
            BassAudioService.Instance.LoadCalibrationStream().Forget();
        }

        private void OnDestroy()
        {
            mainButton.onClick.RemoveListener(StartCalibration);
            confirmButton.onClick.RemoveListener(ConfirmCalibration);
            cancelButton.onClick.RemoveListener(CancelCalibration);
            BassAudioService.Instance.CalibrationStream?.Dispose();
        }

        private void ConfirmCalibration()
        {
            confirmPressed = true;
        }

        private void RetryCalibration()
        {
            retryPressed = true;
        }

        private void StartCalibration()
        {
            hitAnimator.Show();
            StartCalibrationTask(cts.Token).Forget();
        }

        private void CancelCalibration()
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
            BassAudioService.Instance.CalibrationStream?.Pause();
            BassAudioService.Instance.AudioPreviewStream?.Resume();
            mainButton.onClick.AddListener(StartCalibration);
            dialogAnimator.Hide();
        }

        private async UniTask StartCalibrationTask(CancellationToken ct)
        {
            mainButton.onClick.RemoveAllListeners();
            BassAudioService.Instance.AudioPreviewStream?.Pause();

            while (true)
            {
                confirmButton.interactable = false;

                BassAudioService.Instance.CalibrationStream.Play();
                var hitTimings = new int[expectedHitTimings.Length];
                Array.Fill(hitTimings, 0);
                SetOffsetTextsState(hitTimings);

                for (int i = 0; i < hitTimings.Length; i++)
                {
                    hitTimings[i] = int.MinValue;
                }

                while (BassAudioService.Instance.CalibrationStream.Position <= 0)
                {
                    await UniTask.NextFrame();
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }
                }

                while (BassAudioService.Instance.CalibrationStream.IsPlaying)
                {
                    var hit = Input.GetMouseButtonDown(0) && 
                              RectTransformUtility.RectangleContainsScreenPoint(mainButtonRect, Input.mousePosition);

                    var touchCount = Input.touchCount;
                    for (var t = 0; t < touchCount; t++)
                    {
                        var touch = Input.GetTouch(t);
                        hit |= touch.phase == TouchPhase.Began &&
                            RectTransformUtility.RectangleContainsScreenPoint(mainButtonRect, touch.position);
                        
                        if (hit)
                        {
                            break;
                        }
                    }
                    
                    if (hit)
                    {
                        var timing = BassAudioService.Instance.CalibrationStream.Position;
                        var minDiff = int.MaxValue;
                        var minDiffIndex = 0;
                        for (var i = 0; i < expectedHitTimings.Length; i++)
                        {
                            var diff = Mathf.Abs(expectedHitTimings[i] - timing);
                            if (diff >= minDiff) continue;
                            minDiff = diff;
                            minDiffIndex = i;
                        }

                        hitTimings[minDiffIndex] = timing;
                        SetOffsetTextsState(hitTimings);
                        hitAnimator.Show();
                    }

                    await UniTask.NextFrame();
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }
                }

                int avgOffset = 0;
                for (int i = 0; i < expectedHitTimings.Length; i++)
                {
                    int offset = hitTimings[i] - expectedHitTimings[i];
                    avgOffset += offset;
                }

                avgOffset /= expectedHitTimings.Length;
                confirmButton.interactable = true;
                confirmPressed = false;
                retryPressed = false;
                mainButton.onClick.AddListener(RetryCalibration);
                while (!confirmPressed)
                {
                    await UniTask.NextFrame();
                    if (cts.IsCancellationRequested)
                    {
                        return;
                    }

                    if (retryPressed)
                    {
                        break;
                    }
                }

                if (retryPressed)
                {
                    hitAnimator.Show();
                    continue;
                }

                Settings.GlobalAudioOffset.Value = avgOffset;
                BassAudioService.Instance.CalibrationStream?.Pause();
                BassAudioService.Instance.AudioPreviewStream?.Resume();
                mainButton.onClick.AddListener(StartCalibration);
                dialogAnimator.Hide();
                return;
            }
        }

        private void SetOffsetTextsState(int[] hitTimings)
        {
            var max = -1;
            for (var i = 0; i < hitTimings.Length; i++)
            {
                var text = offsetTexts[i];
                var hit = hitTimings[i];
                if (hit <= 0)
                {
                    text.text = "-";
                }
                else
                {
                    max = Mathf.Max(max, i);
                    var offset = hit - expectedHitTimings[i];
                    text.text = offset.ToString();
                }
            }

            for (var i = 0; i < offsetToggles.Length; i++)
            {
                offsetToggles[i].isOn = i <= max;
            }
        }
    }
}