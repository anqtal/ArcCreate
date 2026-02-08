using System.Collections.Generic;
using System.Linq;
using ArcCreate.ChartFormat;
using ArcCreate.Gameplay.Data;
using UnityEngine;

namespace ArcCreate.Gameplay.Chart
{
    public class ChartService : MonoBehaviour
    {
        [SerializeField] private GameplayData gameplayData;

        [SerializeField] private GameObject beatlinePrefab;
        [SerializeField] private Transform beatlineParent;
        [SerializeField] private Color beatlineColor;
        [SerializeField] private int beatlineCapacity;

        private BeatlineDisplay beatlineDisplay;

        public bool IsLoaded { get; private set; }

        public bool EnableArcRebuildSegment
        {
            get => Values.EnableArcRebuildSegment;
            set => Values.EnableArcRebuildSegment = value;
        }

        public int NoteCount => Services.Score.NoteCount;

        public List<TimingGroup> TimingGroups { get; } = new();

        private void Awake()
        {
            var beatlinePool = Pools.New<BeatlineBehaviour>(Values.BeatlinePoolName, beatlinePrefab, beatlineParent,
                beatlineCapacity);

            Settings.GlobalAudioOffset.OnValueChanged.AddListener(OnGlobalOffsetChange);
            gameplayData.BaseBpm.OnValueChange += OnBaseBpm;
            gameplayData.TimingPointDensityFactor.OnValueChange += OnTimingPointDensityFactor;
            gameplayData.AudioOffset.OnValueChange += OnChartAudioOffset;
            gameplayData.AudioClip.OnValueChange += OnAudioClipChange;
            beatlineDisplay = new BeatlineDisplay(new GameplayBeatlineGenerator(beatlineColor), beatlinePool);
        }

        private void OnDestroy()
        {
            Pools.Destroy<BeatlineBehaviour>(Values.BeatlinePoolName);

            Settings.GlobalAudioOffset.OnValueChanged.RemoveListener(OnGlobalOffsetChange);
            gameplayData.BaseBpm.OnValueChange -= OnBaseBpm;
            gameplayData.TimingPointDensityFactor.OnValueChange -= OnTimingPointDensityFactor;
            gameplayData.AudioOffset.OnValueChange -= OnChartAudioOffset;
            gameplayData.AudioClip.OnValueChange -= OnAudioClipChange;
        }

        public void ReloadSkin()
        {
            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                tg.ReloadSkin();
            }
        }

        public void ResetJudge()
        {
            var currentCombo = 0;
            var timing = Services.Audio.ChartTiming;
            var totalCombo = 0;
            var inputMode = (InputMode)Settings.InputMode.Value;
            var isAuto = inputMode == InputMode.Auto || inputMode == InputMode.AutoController;

            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                tg.ResetJudgeTo(timing);
                if (isAuto) currentCombo += tg.ComboAt(Services.Audio.ChartTiming);

                totalCombo += tg.TotalCombo();
            }

            Services.Score.ResetScoreTo(currentCombo, totalCombo);
            Services.Judgement.ResetJudge();
            Services.Scenecontrol.UpdateScenecontrol(timing);
            Services.Camera.UpdateCamera(timing);
            //Services.Hitsound.ResetHitsoundHistory();
        }

        public IEnumerable<T> FindByTiming<T>(int from, int to)
            where T : ArcEvent
        {
            if (typeof(T) == typeof(ScenecontrolEvent))
                foreach (var note in Services.Scenecontrol.FindByTiming(from, to))
                    yield return note as T;

            if (typeof(T) == typeof(CameraEvent))
                foreach (var note in Services.Camera.FindByTiming(from, to))
                    yield return note as T;

            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                var groupNotes = tg.FindByTiming<T>(from, to);
                foreach (var note in groupNotes) yield return note;
            }
        }

        public IEnumerable<T> FindByEndTiming<T>(int from, int to)
            where T : LongNote
        {
            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                var groupNotes = tg.FindByEndTiming<T>(from, to);
                foreach (var note in groupNotes) yield return note;
            }
        }

        public IEnumerable<T> FindEventsWithinRange<T>(int from, int to, bool overlapCompletely)
            where T : ArcEvent
        {
            if (typeof(T) == typeof(ScenecontrolEvent))
                foreach (var note in Services.Scenecontrol.FindWithinRange(from, to))
                    yield return note as T;

            if (typeof(T) == typeof(CameraEvent))
                foreach (var note in Services.Camera.FindWithinRange(from, to, overlapCompletely))
                    yield return note as T;

            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                var groupNotes = tg.FindEventsWithinRange<T>(from, to, overlapCompletely);
                foreach (var note in groupNotes) yield return note;
            }
        }

        public IEnumerable<T> GetAll<T>()
            where T : ArcEvent
        {
            if (typeof(T) == typeof(ScenecontrolEvent))
                foreach (var note in Services.Scenecontrol.Events)
                    yield return note as T;

            if (typeof(T) == typeof(CameraEvent))
                foreach (var note in Services.Camera.Events)
                    yield return note as T;

            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                var groupNotes = tg.GetEventType<T>();
                foreach (var note in groupNotes) yield return note;
            }
        }

        public IEnumerable<Note> GetRenderingNotes()
        {
            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                if (!tg.GroupProperties.Visible || !tg.IsVisible) continue;

                var groupNotes = tg.GetRenderingNotes();

                foreach (var note in groupNotes) yield return note;
            }
        }

        public void Clear()
        {
            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                tg.Clear();
            }

            TimingGroups.Clear();
            Services.Camera.Clear();
            Services.Scenecontrol.Clear();
        }

        public void LoadChart(ChartReader reader)
        {
            LoadChart(new ArcChart(reader));
            IsLoaded = true;
            Services.Audio.AudioTiming = 0;
        }

        public void LoadChart(ArcChart chart)
        {
            Clear();

            for (var j = 0; j < chart.TimingGroups.Count; j++)
            {
                var tg = chart.TimingGroups[j];
                var newTg = new TimingGroup(j);
                TimingGroups.Add(newTg);
                newTg.Load(tg);
            }

            Services.Camera.Load(chart.Cameras);
            Services.Scenecontrol.Load(chart.SceneControls);

            gameplayData.AudioOffset.Value = chart.AudioOffset;
            gameplayData.TimingPointDensityFactor.Value = chart.TimingPointDensity;

            ResetJudge();
        }

        public void ReloadBeatline(int audioLength)
        {
            beatlineDisplay.LoadFromTimingGroup(0, audioLength);
        }

        public void ReloadBeatline()
        {
            var length = 0;
            if (gameplayData.AudioClip.Value != null)
                length = Mathf.RoundToInt(gameplayData.AudioClip.Value.length * 1000);

            ReloadBeatline(length);
        }

        public void AddEvents(IEnumerable<ArcEvent> e)
        {
            foreach (var n in e)
                if (n.TimingGroup >= TimingGroups.Count)
                    GetTimingGroup(n.TimingGroup);

            var cameraEvents = e.Where(n => n is CameraEvent).Cast<CameraEvent>();
            var scEvents = e.Where(n => n is ScenecontrolEvent).Cast<ScenecontrolEvent>();

            if (cameraEvents.Any())
            {
                Services.Camera.Add(cameraEvents);
                gameplayData.NotifyChartCameraEdit();
            }

            if (scEvents.Any())
            {
                Services.Scenecontrol.Add(scEvents);
                gameplayData.NotifyChartScenecontrolEdit();
            }

            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                tg.AddEvents(e.Where(n => n.TimingGroup == tg.GroupNumber));
            }

            if (e.Any(n => n is TimingEvent)) gameplayData.NotifyChartTimingEdit();

            gameplayData.NotifyChartEdit();
        }

        public void RemoveEvents(IEnumerable<ArcEvent> e)
        {
            var cameraEvents = e.Where(n => n is CameraEvent).Cast<CameraEvent>();
            var scEvents = e.Where(n => n is ScenecontrolEvent).Cast<ScenecontrolEvent>();

            if (cameraEvents.Any())
            {
                Services.Camera.Remove(cameraEvents);
                gameplayData.NotifyChartCameraEdit();
            }

            if (scEvents.Any())
            {
                Services.Scenecontrol.Remove(scEvents);
                gameplayData.NotifyChartScenecontrolEdit();
            }

            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                tg.RemoveEvents(e.Where(n => n.TimingGroup == tg.GroupNumber));
            }

            if (e.Any(n => n is TimingEvent)) gameplayData.NotifyChartTimingEdit();

            gameplayData.NotifyChartEdit();
        }

        public void UpdateEvents(IEnumerable<ArcEvent> e)
        {
            var cameraEvents = e.Where(n => n is CameraEvent).Cast<CameraEvent>();
            var scEvents = e.Where(n => n is ScenecontrolEvent).Cast<ScenecontrolEvent>();

            if (cameraEvents.Any())
            {
                Services.Camera.Change(cameraEvents);
                gameplayData.NotifyChartCameraEdit();
            }

            if (scEvents.Any())
            {
                Services.Scenecontrol.Change(scEvents);
                gameplayData.NotifyChartScenecontrolEdit();
            }

            var tgChanged = e.Where(n => n.TimingGroupChanged).ToList();
            if (tgChanged.Count > 0)
            {
                tgChanged.Sort((a, b) => a.TimingGroupChangedFrom == b.TimingGroupChangedFrom
                    ? a.TimingGroup.CompareTo(b.TimingGroup)
                    : a.TimingGroupChangedFrom.CompareTo(b.TimingGroupChangedFrom));

                var from = tgChanged[0].TimingGroupChangedFrom;
                var to = tgChanged[0].TimingGroup;

                var currentTgChange = new List<ArcEvent>();
                for (var i = 0; i < tgChanged.Count; i++)
                {
                    var n = tgChanged[i];
                    if (from == n.TimingGroupChangedFrom && to == n.TimingGroup)
                    {
                        currentTgChange.Add(n);
                    }
                    else
                    {
                        GetTimingGroup(from).RemoveEvents(currentTgChange);
                        var target = GetTimingGroup(to);
                        foreach (var note in currentTgChange) note.TimingGroup = target.GroupNumber;

                        target.AddEvents(currentTgChange);

                        from = n.TimingGroupChangedFrom;
                        to = n.TimingGroup;
                        currentTgChange.Clear();
                        currentTgChange.Add(n);
                    }

                    n.ResetTimingGroupChangedFrom();
                }

                {
                    GetTimingGroup(from).RemoveEvents(currentTgChange);
                    var target = GetTimingGroup(to);
                    foreach (var note in currentTgChange)
                    {
                        note.TimingGroup = target.GroupNumber;
                        note.ResetTimingGroupChangedFrom();
                    }

                    target.AddEvents(currentTgChange);
                }
            }

            var tgUnchanged = e.Where(n => !n.TimingGroupChanged).ToList();
            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                tg.UpdateEvents(tgUnchanged.Where(n => n.TimingGroup == tg.GroupNumber));
            }

            if (e.Any(n => n is TimingEvent)) gameplayData.NotifyChartTimingEdit();

            gameplayData.NotifyChartEdit();
        }

        public TimingGroup GetTimingGroup(int tg)
        {
            if (tg < 0) return TimingGroups[0];

            if (tg >= TimingGroups.Count)
            {
                var newTg = new TimingGroup(TimingGroups.Count);
                newTg.Load();
                TimingGroups.Add(newTg);
                if (string.IsNullOrEmpty(newTg.GroupProperties.FileName))
                    newTg.GroupProperties.FileName = TimingGroups[0].GroupProperties.FileName;

                return newTg;
            }

            return TimingGroups[tg];
        }

        public void RemoveTimingGroup(TimingGroup group)
        {
            Services.Camera.RemoveTimingGroup(group);
            Services.Scenecontrol.RemoveTimingGroup(group);
            for (var i = 0; i < TimingGroups.Count; i++)
                if (TimingGroups[i] == group)
                {
                    TimingGroups.Remove(group);
                    for (var j = i; j < TimingGroups.Count; j++) TimingGroups[j].SetGroupNumber(j);

                    gameplayData.NotifyChartEdit();
                    break;
                }
        }

        public void InsertTimingGroup(TimingGroup group)
        {
            Services.Camera.InsertTimingGroup(group);
            Services.Scenecontrol.InsertTimingGroup(group);

            if (string.IsNullOrEmpty(group.GroupProperties.FileName))
                group.GroupProperties.FileName = TimingGroups[0].GroupProperties.FileName;

            if (group.GroupNumber < 1) group.SetGroupNumber(1);

            if (group.GroupNumber > TimingGroups.Count) group.SetGroupNumber(TimingGroups.Count);

            TimingGroups.Insert(group.GroupNumber, group);
            for (var i = group.GroupNumber + 1; i < TimingGroups.Count; i++) TimingGroups[i].SetGroupNumber(i);

            gameplayData.NotifyChartEdit();
        }

        public void UpdateChartJudgement(int currentTiming)
        {
            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                tg.UpdateGroupJudgement(currentTiming);
            }
        }

        public void UpdateChartRender(int currentTiming)
        {
            for (var i = 0; i < TimingGroups.Count; i++)
            {
                var tg = TimingGroups[i];
                tg.UpdateGroupRender(currentTiming);

                if (i == 0) beatlineDisplay.UpdateBeatlines(tg.GetFloorPosition(currentTiming));
            }
        }

        public void NotifyEdit()
        {
            gameplayData.NotifyChartEdit();
        }

        private void OnAudioClipChange(AudioClip obj)
        {
            ReloadBeatline(Mathf.RoundToInt(obj.length * 1000));
        }

        private void OnTimingPointDensityFactor(float value)
        {
            Values.TimingPointDensity = value;
            ResetJudge();
        }

        private void OnBaseBpm(float value)
        {
            Values.BaseBpm = value;
            ResetJudge();
        }

        private void OnChartAudioOffset(int value)
        {
            Values.ChartAudioOffset = value;
            ResetJudge();
        }

        private void OnGlobalOffsetChange(int offset)
        {
            ResetJudge();
        }
    }
}