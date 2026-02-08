using System.Collections.Generic;
using UnityEngine;

namespace ArcCreate.Gameplay.InputFeedback
{
    public class InputFeedbackService : MonoBehaviour
    {
        [SerializeField] private GameObject floatLinePrefab;
        [SerializeField] private GameObject laneHitPrefab;
        [SerializeField] private Transform laneHitParent;
        [SerializeField] private Transform floatLineParent;
        [SerializeField] private Transform skyInput;
        [SerializeField] private int floatLinePoolCount = 4;
        private readonly Dictionary<int, SpriteRenderer> laneFeedbacks = new();

        private Pool<Transform> floatLinePool;

        private void Awake()
        {
            Pools.New<Transform>("FloatLineInputFeedback", floatLinePrefab, floatLineParent, floatLinePoolCount);
            floatLinePool = Pools.Get<Transform>("FloatLineInputFeedback");
        }

        private void OnDestroy()
        {
            Pools.Destroy<Transform>("FloatLineInputFeedback");
        }

        public void UpdateInputFeedback()
        {
            var alphaDecrease = Values.LaneFeedbackMaxAlpha * (Time.deltaTime / Values.LaneFeedbackFadeoutDuration);
            foreach (var laneFeedback in laneFeedbacks.Values)
                laneFeedback.color = new Color(1, 1, 1, Mathf.Max(laneFeedback.color.a - alphaDecrease, 0));

            floatLinePool.ReturnAll();
        }

        public void LaneFeedback(int lane)
        {
            if (lane >= Values.LaneFrom || lane <= Values.LaneTo)
            {
                var laneFeedback = GetLaneFeedback(lane);
                laneFeedback.color = new Color(1, 1, 1, Values.LaneFeedbackMaxAlpha);
                laneFeedback.transform.localPosition = new Vector3(ArcFormula.LaneToWorldX(lane), 0, 0);
            }
        }

        public void FloatlineFeedback(float y)
        {
            if (y >= Values.MinVerticalFeedbackY)
            {
                var verticalFeedback = floatLinePool.Get();
                y = Mathf.Min(y, skyInput.position.y);
                verticalFeedback.localPosition = new Vector3(0, y, 0);
            }
        }

        private SpriteRenderer GetLaneFeedback(int lane)
        {
            if (!laneFeedbacks.ContainsKey(lane))
            {
                var go = Instantiate(laneHitPrefab, laneHitParent);
                var sprite = go.GetComponent<SpriteRenderer>();
                laneFeedbacks.Add(lane, sprite);
                return sprite;
            }

            return laneFeedbacks[lane];
        }
    }
}