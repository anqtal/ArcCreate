using System;
using System.Collections.Generic;
using ArcCreate.ChartFormat;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Gameplay.Chart;
using ArcCreate.Gameplay.Data;
using ArcCreate.Utility.Extension;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace ArcCreate.Gameplay.Scenecontrol
{
    public class ScenecontrolService : MonoBehaviour
    {
        private static readonly int OffsetShaderId = Shader.PropertyToID("_Offset");
        [SerializeField] private TMP_FontAsset defaultFont;
        [SerializeField] private List<FontEntry> fonts;
        [SerializeField] private Scene scene;
        [SerializeField] private PostProcessing postProcessing;
        [SerializeField] private SpriteRenderer trackSprite;
        [SerializeField] private SpriteRenderer singleLineL;
        [SerializeField] private SpriteRenderer singleLineR;
        [SerializeField] private GlowingSprite skyInputLine;
        [SerializeField] private GlowingSprite skyInputLabel;
        [SerializeField] private SpriteRenderer laneExtraL;
        [SerializeField] private SpriteRenderer laneExtraR;
        private readonly List<ISceneController> referencedControllers = new();
        private float count;
        private int loopSwitch = 1;
        private float singleLineOffset;
        private float trackOffset;

        public List<ScenecontrolEvent> Events { get; private set; } = new();

        public Scene Scene => scene;

        public PostProcessing PostProcessing => postProcessing;

        public Context Context { get; } = new();

        public string ScenecontrolFolder { get; set; }

        public bool IsLoaded { get; private set; }

        private void Awake()
        {
            foreach (var c in scene.DisabledByDefault) c.Start();
        }

        public void Load(List<ScenecontrolEvent> cameras)
        {
            Events = cameras;
            RebuildList();
        }

        public void Clear()
        {
            Events.Clear();
            Clean();
        }

        public void Add(IEnumerable<ScenecontrolEvent> events)
        {
            this.Events.AddRange(events);
            RebuildList();
        }

        public void Change(IEnumerable<ScenecontrolEvent> events)
        {
            RebuildList();
        }

        public void Remove(IEnumerable<ScenecontrolEvent> events)
        {
            foreach (var sc in events) this.Events.Remove(sc);

            RebuildList();
        }

        public void RemoveTimingGroup(TimingGroup group)
        {
            Events.RemoveAll(e => e.TimingGroup == group.GroupNumber);

            foreach (var sc in Events)
                if (sc.TimingGroup > group.GroupNumber)
                {
                    sc.TimingGroup -= 1;
                    sc.ResetTimingGroupChangedFrom();
                }

            RebuildList();
        }

        public void InsertTimingGroup(TimingGroup group)
        {
            foreach (var sc in Events)
                if (sc.TimingGroup >= group.GroupNumber)
                {
                    sc.TimingGroup += 1;
                    sc.ResetTimingGroupChangedFrom();
                }

            RebuildList();
        }

        public IEnumerable<ScenecontrolEvent> FindByTiming(int from, int to)
        {
            var i = Events.BisectLeft(from, n => n.Timing);
            while (i >= 0 && i < Events.Count && Events[i].Timing >= from && Events[i].Timing <= to)
            {
                yield return Events[i];
                i++;
            }
        }

        public IEnumerable<ScenecontrolEvent> FindWithinRange(int from, int to)
        {
            for (var i = 0; i < Events.Count; i++)
            {
                var sc = Events[i];
                if (sc.Timing >= from && sc.Timing <= to) yield return sc;
            }
        }

        public void UpdateScenecontrol(int currentTiming)
        {
            Values.LaneFrom = laneExtraL.color.a > Mathf.Epsilon && laneExtraL.gameObject.activeInHierarchy ? 0 : 1;
            Values.LaneTo = laneExtraR.color.a > Mathf.Epsilon && laneExtraR.gameObject.activeInHierarchy ? 5 : 4;

            foreach (var c in referencedControllers) c.UpdateController(currentTiming);

            Services.Score.ClearJudgementsThisFrame();

            if (PauseMenu.IsPausing) return;

            var bpm = Services.Chart.GetTimingGroup(0).GetBpm(currentTiming);
            var beatDuration = bpm != 0 ? 60.0f / bpm : Mathf.Infinity;

            bpm = Mathf.Abs(bpm);

            count += Time.deltaTime * loopSwitch;
            if (count >= beatDuration)
            {
                count = beatDuration;
                loopSwitch *= -1;
            }
            else if (count <= 0)
            {
                count = 0;
                loopSwitch *= -1;
            }

            var speed = bpm / Values.BaseBpm;
            var glowAlpha = Mathf.Lerp(0.75f, 1, count / beatDuration);

            trackOffset += Time.deltaTime * speed * 6;
            trackSprite.material.SetFloat(OffsetShaderId, trackOffset);
            singleLineOffset += speed >= 0 ? Time.deltaTime * speed * 6 : Time.deltaTime * 0.6f;
            singleLineL.material.SetFloat(OffsetShaderId, singleLineOffset);
            singleLineR.material.SetFloat(OffsetShaderId, singleLineOffset);
            skyInputLine.ApplyGlow(glowAlpha);
            skyInputLabel.ApplyGlow(glowAlpha);
        }

        public void Clean()
        {
            Scene.ClearCache();
            PostProcessing.DisablePostProcess();
            foreach (var c in referencedControllers) c.CleanController();

            referencedControllers.Clear();
        }

        public string Export()
        {
            var serialization = new ScenecontrolSerialization();
            if (referencedControllers.Count == 0) return null;

            foreach (var c in referencedControllers) serialization.AddUnitAndGetId(c);

            return JsonConvert.SerializeObject(serialization.Result);
        }

        public void Import(string def, IFileAccessWrapper fileAccess)
        {
            if (def == null) return;

            scene.SetFileAccess(fileAccess);
            var units = JsonConvert.DeserializeObject<List<SerializedUnit>>(def);
            var deserialization = new ScenecontrolDeserialization(scene, postProcessing, units);
            foreach (var unit in deserialization.Result)
                if (unit is ISceneController c)
                    AddReferencedController(c);
        }

        public void AddReferencedController(ISceneController c)
        {
            if (!referencedControllers.Contains(c)) referencedControllers.Add(c);
        }

        public void WaitForSceneLoad()
        {
            IsLoaded = false;
            scene.WaitForTasksComplete().ContinueWith(() =>
            {
                IsLoaded = true;
                UpdateScenecontrol(Services.Audio.ChartTiming);
            });
        }

        public TMP_FontAsset GetFont(string font)
        {
            foreach (var entry in fonts)
                if (entry.Name == font || entry.FontAsset.name == font)
                    return entry.FontAsset;

            return defaultFont;
        }

        private void RebuildList()
        {
            Events.Sort((a, b) => a.Timing.CompareTo(b.Timing));
        }

        [Serializable]
        private struct FontEntry
        {
            public string Name;

            public TMP_FontAsset FontAsset;
        }
    }
}