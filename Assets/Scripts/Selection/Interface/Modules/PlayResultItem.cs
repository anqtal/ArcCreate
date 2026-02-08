using ArcCreate.Data;
using ArcCreate.SceneTransition;
using ArcCreate.Storage;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    public class PlayResultItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text dateTime;
        [SerializeField] private TMP_Text score;
        [SerializeField] private GradeDisplay grade;
        [SerializeField] private ClearResultDisplay clearResult;
        [SerializeField] private Button button;
        private Difficulty chart;
        private SongList level;
        private PlayResult result;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClick);
        }

        public void Display(SongList level, Difficulty chart, PlayResult result)
        {
            this.level = level;
            this.chart = chart;
            this.result = result;
            score.text = result.FormattedScore;
            grade.Display(result.Grade);
            clearResult.Display(result.ClearResult);
            dateTime.text = result.DateTime.ToString("yyyy/MM/dd HH:mm");
        }

        private void OnClick()
        {
            if (SceneTransitionManager.Instance.IsTransitioning) return;

            var transition = new TransitionSequence()
                .OnShow()
                .AddTransition(new TriangleTileTransition());
            SceneTransitionManager.Instance.SetTransition(transition);
            SceneTransitionManager.Instance.SwitchScene(
                SceneNames.ResultScene,
                rep =>
                {
                    rep.PassData(level, chart, result, false);
                    return default;
                }).Forget();
        }
    }
}