using ArcCreate.Gameplay.Audio;
using ArcCreate.Gameplay.Chart;
using ArcCreate.Gameplay.GameplayCamera;
using ArcCreate.Gameplay.InputFeedback;
using ArcCreate.Gameplay.Judgement;
using ArcCreate.Gameplay.Particle;
using ArcCreate.Gameplay.Render;
using ArcCreate.Gameplay.Scenecontrol;
using ArcCreate.Gameplay.Score;
using ArcCreate.Gameplay.Skin;
using UnityEngine;

namespace ArcCreate.Gameplay
{
    internal class Services : MonoBehaviour
    {
        [SerializeField] private SkinService skin;
        [SerializeField] private new AudioService audio;
        [SerializeField] private new CameraService camera;
        [SerializeField] private ChartService chart;
        [SerializeField] private ParticleService particle;
        [SerializeField] private JudgementService judgement;
        [SerializeField] private InputFeedbackService inputFeedback;
        [SerializeField] private ScoreService score;
        [SerializeField] private ScenecontrolService scenecontrol;
        [SerializeField] private RenderService render;

        public static SkinService Skin { get; private set; }

        public static ChartService Chart { get; private set; }

        public static CameraService Camera { get; private set; }

        public static AudioService Audio { get; private set; }

        public static ParticleService Particle { get; private set; }

        public static JudgementService Judgement { get; private set; }

        public static InputFeedbackService InputFeedback { get; private set; }

        public static ScenecontrolService Scenecontrol { get; private set; }

        public static ScoreService Score { get; private set; }

        public static RenderService Render { get; private set; }


        private void Awake()
        {
            Skin = skin;
            Chart = chart;
            Particle = particle;
            Judgement = judgement;
            Audio = audio;
            Score = score;
            InputFeedback = inputFeedback;
            Scenecontrol = scenecontrol;
            Camera = camera;
            Render = render;
        }
    }
}