using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ArcCreate
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class SettingsDropdown : MonoBehaviour
    {
        private TMP_Dropdown dropdown;
        private Array enumValues;
        private IntSetting setting;

        private TMP_Dropdown Dropdown
        {
            get
            {
                dropdown = dropdown == null ? GetComponent<TMP_Dropdown>() : dropdown;
                return dropdown;
            }
        }

        private void Awake()
        {
            Dropdown.onValueChanged.AddListener(OnUIChange);
        }

        private void OnDestroy()
        {
            Dropdown.onValueChanged.RemoveListener(OnUIChange);
            setting?.OnValueChanged.RemoveListener(OnSettingChange);
        }

        public void Setup(IntSetting setting, Type enumType, string i18nKey)
        {
            enumValues = Enum.GetValues(enumType);
            this.setting = setting;
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (var enumValue in enumValues)
                options.Add(new TMP_Dropdown.OptionData(I18n.S($"{i18nKey}.{enumValue.ToString().ToLower()}")));

            Dropdown.options = options;
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
                    Dropdown.SetValueWithoutNotify(i);
                    return;
                }
            }
        }

        private void OnUIChange(int value)
        {
            setting.Value = (int)enumValues.GetValue(value);
        }
    }
}