using UnityEngine;
using UnityEngine.Events;

namespace ArcCreate.Utility
{
    [CreateAssetMenu(fileName = "Theme", menuName = "ScriptableObject/Theme")]
    public class ThemeGroup : ScriptableObject
    {
        [SerializeField] private string playerPrefKey;
        [SerializeField] private Theme defaultTheme;
        private Option<Theme> overrideValue;

        private Theme value;

        public Option<Theme> OverrideValue
        {
            get => overrideValue;
            set
            {
                overrideValue = value;
                Update();
            }
        }

        public Theme Value
        {
            get
            {
                var theme = value;
                if (overrideValue.HasValue) theme = overrideValue.Value;

                return theme;
            }

            set
            {
                this.value = value;
                Update();
            }
        }

        public Theme LastSelectedTheme => (Theme)PlayerPrefs.GetInt(playerPrefKey, (int)defaultTheme);

        public OnChangeEvent OnValueChange { get; set; } = new();

        private void Update()
        {
            var theme = Value;
            OnValueChange.Invoke(theme);
            PlayerPrefs.SetInt(playerPrefKey, (int)theme);
        }

        public class OnChangeEvent : UnityEvent<Theme>
        {
        }
    }
}