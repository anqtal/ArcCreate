using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate
{
    public class SettingsEnum : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text text;
        private Array enumValues;
        private string i18nKey;
        private IntSetting setting;

        private void Awake()
        {
            button.onClick.AddListener(OnUIChange);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnUIChange);
            setting?.OnValueChanged.RemoveListener(OnSettingChange);
        }

        public void Setup(IntSetting setting, Type enumType, string i18nKey)
        {
            enumValues = Enum.GetValues(enumType);
            this.setting = setting;
            this.i18nKey = i18nKey;
            setting.OnValueChanged.AddListener(OnSettingChange);
            OnSettingChange(setting.Value);
        }

        private void OnSettingChange(int value)
        {
            for (var i = 0; i < enumValues.Length; i++)
            {
                var obj = enumValues.GetValue(i);
                if ((int)obj == value)
                {
                    text.text = I18n.S($"{i18nKey}.{obj.ToString().ToLower()}");
                    return;
                }
            }
        }

        private void OnUIChange()
        {
            for (var i = 0; i < enumValues.Length; i++)
            {
                var value = (int)enumValues.GetValue(i);
                if (value == setting.Value)
                {
                    var obj = enumValues.GetValue((i + 1) % enumValues.Length);
                    setting.Value = (int)obj;
                    return;
                }
            }
        }
    }
}