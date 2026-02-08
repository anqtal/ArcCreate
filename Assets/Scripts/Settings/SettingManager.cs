using System;
using System.IO;
using UnityEngine;

namespace ArcCreate
{
    public class SettingManager
    {
        private SettingsItem settings;

        public SettingManager()
        {
            LoadSettings();
        }

        private static string FilePath => Path.Combine(Application.persistentDataPath, "Settings.json");

        private void LoadSettings()
        {
            if (File.Exists(FilePath))
            {
                var jsonContent = File.ReadAllText(FilePath);
                settings = JsonUtility.FromJson<SettingsItem>(jsonContent);
            }
            else
            {
                settings = new SettingsItem();
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            var jsonContent = JsonUtility.ToJson(settings, true);
            File.WriteAllText(FilePath, jsonContent);
        }

        [Serializable]
        public class SettingsItem
        {
            public AudioSettings audio = new();
            public string token = "";
        }

        [Serializable]
        public class AudioSettings
        {
            public float musicVolume = 1.0f;
            public float guideVolume = 1.0f;
            public float tapVolume = 1.0f;
            public float arcVolume = 1.0f;
        }
    }
}