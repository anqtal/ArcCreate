using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Gameplay.Audio.Practice
{
    public class PracticeTimingControl : MonoBehaviour
    {
        [SerializeField] private GameplayData gameplayData;
        [SerializeField] private Button backButton;
        [SerializeField] private Button forwardButton;

        private void Awake()
        {
            backButton.onClick.AddListener(JumpBack);
            forwardButton.onClick.AddListener(JumpForward);
        }

        private void OnDestroy()
        {
            backButton.onClick.RemoveListener(JumpBack);
            forwardButton.onClick.RemoveListener(JumpForward);
        }

        private void JumpBack()
        {
            var timing = Services.Audio.AudioTiming;
            var duration = JumpDuration(gameplayData.PlaybackSpeed.Value);
            var newTiming = Mathf.Clamp(timing - duration, 0, Services.Audio.AudioLength);
            Services.Audio.Pause();
            Services.Audio.PlayWithDelay(newTiming, Values.DelayBeforeAudioResume);
        }

        private void JumpForward()
        {
            var timing = Services.Audio.AudioTiming;
            var duration = JumpDuration(gameplayData.PlaybackSpeed.Value);
            var newTiming = Mathf.Clamp(timing + duration, 0, Services.Audio.AudioLength);
            Services.Audio.Pause();
            Services.Audio.PlayWithDelay(newTiming, Values.DelayBeforeAudioResume);
        }

        private int JumpDuration(float speed)
        {
            return Mathf.RoundToInt(5000 * speed);
        }
    }
}