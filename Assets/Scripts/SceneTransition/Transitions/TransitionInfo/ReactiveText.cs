using TMPro;
using UnityEngine;

namespace ArcCreate.SceneTransition
{
    [RequireComponent(typeof(TMP_Text))]
    public class ReactiveText : MonoBehaviour
    {
        [SerializeField] private StringSO stringSO;

        protected TMP_Text CachedText { get; private set; }

        private void Awake()
        {
            CachedText = GetComponent<TMP_Text>();
            stringSO.OnValueChange.AddListener(OnTextChange);
            OnTextChange(stringSO.Value);
        }

        private void OnDestroy()
        {
            stringSO.OnValueChange.RemoveListener(OnTextChange);
        }

        protected virtual void OnTextChange(string text)
        {
            CachedText.text = text;
        }
    }
}