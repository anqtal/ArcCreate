using UnityEngine;

namespace ArcCreate.Gameplay.Audio
{
    public class DoubleClickPauseHandler : IPauseButtonHandler
    {
        private readonly float maxDuration;
        private readonly PauseButton parent;
        private float lastClickAt = float.MinValue;

        public DoubleClickPauseHandler(PauseButton parent, float maxDuration)
        {
            this.parent = parent;
            this.maxDuration = maxDuration;
        }

        public void OnClick()
        {
        }

        public void OnRelease()
        {
            var oldLast = lastClickAt;
            lastClickAt = Time.time;
            var result = oldLast + maxDuration >= lastClickAt;
            if (result)
            {
                lastClickAt = float.MinValue;
                parent.Activate();
            }
        }
    }
}