using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcCreate.SceneTransition
{
    public class TitleSceneManager : MonoBehaviour
    {
        public async void LoadPlayer()
        {
            print("Loading Player...");
            SceneManager.UnloadSceneAsync(1);
            await SceneTransitionManager.Instance.LoadSceneAdditive(SceneNames.SelectScene);
        }
    }
}