namespace ArcCreate.SceneTransition
{
    public static class SceneNames
    {
        public const string BootScene = "Boot";
        public const string GreetingScene = "Greeting";
        public const string GameplayScene = "Gameplay";
        public const string SelectScene = "Select";
        public const string ResultScene = "Result";
        public const string StorageScene = "Storage";

        public const string DefaultScene = GreetingScene;

#if UNITY_EDITOR || !UNITY_STANDALONE
        public static readonly string[] RequiredScenes = { StorageScene };
#else
        public static readonly string[] RequiredScenes = new string[0];
#endif
    }
}