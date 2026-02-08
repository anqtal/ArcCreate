using System.Collections.Generic;
using ArcCreate.Gameplay.Chart;
using ArcCreate.Gameplay.Data;
using ArcCreate.Utility.Extension;
using UnityEngine;

namespace ArcCreate.Gameplay.GameplayCamera
{
    public class CameraService : MonoBehaviour
    {
        [SerializeField] private Camera backgroundCamera;
        [SerializeField] private Camera overlayCamera;
        [SerializeField] private RectTransform backgroundRect;
        private float currentArcPos;
        private float currentTilt;

        private float fieldOfViewExternal;
        private bool isReset;
        private Quaternion rotationExternal;
        private float tiltFactorExternal;
        private Vector3 translationExternal;

        public Camera GameplayCamera => backgroundCamera;

        public Camera UICamera => overlayCamera;

        public List<CameraEvent> Events { get; private set; } = new();

        public bool IsEditorCamera { get; set; }

        public Vector3 EditorCameraPosition { get; set; }

        public Vector3 EditorCameraRotation { get; set; }

        public bool IsOrthographic
        {
            get => backgroundCamera.orthographic;
            set
            {
                backgroundCamera.orthographic = value;
                overlayCamera.orthographic = value;
            }
        }

        public float OrthographicSize
        {
            get => backgroundCamera.orthographicSize;
            set
            {
                backgroundCamera.orthographicSize = value;
                overlayCamera.orthographicSize = value;
            }
        }

        public Camera[] RenderingCameras { get; private set; }

        /// <summary>
        ///     Gets 0 on 16:9, 1 on 4:3 (16:12), value is unclamped, use with Mathf.Clerp().
        /// </summary>
        private float AspectAdjustment
        {
            get
            {
                var height = backgroundCamera.pixelHeight / (backgroundCamera.pixelWidth / 16f);
                return (height - 9) / 3f;
            }
        }

        private Vector3 ResetPosition
            => new(0f, Values.CameraY, Mathf.Lerp(Values.CameraZ, Values.CameraZTablet, AspectAdjustment));

        private Vector3 ResetRotation
            => new(Mathf.Lerp(Values.CameraRotX, Values.CameraRotXTablet, AspectAdjustment), 180f, 0f);

        private float ResetFOV
            => Mathf.Lerp(50, 65, AspectAdjustment);

        private void Awake()
        {
            currentArcPos = 0;
            isReset = true;
            RenderingCameras = new[] { backgroundCamera, overlayCamera };
        }

        public void Load(List<CameraEvent> cameras)
        {
            Events = cameras;
            RebuildList();
        }

        public void Clear()
        {
            Events.Clear();
        }

        public void Add(IEnumerable<CameraEvent> events)
        {
            this.Events.AddRange(events);
            RebuildList();
        }

        public void Change(IEnumerable<CameraEvent> events)
        {
            RebuildList();
        }

        public void Remove(IEnumerable<CameraEvent> events)
        {
            foreach (var cam in events) this.Events.Remove(cam);

            RebuildList();
        }

        public void RemoveTimingGroup(TimingGroup group)
        {
            Events.RemoveAll(e => e.TimingGroup == group.GroupNumber);

            foreach (var cam in Events)
                if (cam.TimingGroup > group.GroupNumber)
                {
                    cam.TimingGroup -= 1;
                    cam.ResetTimingGroupChangedFrom();
                }

            RebuildList();
        }

        public void InsertTimingGroup(TimingGroup group)
        {
            foreach (var cam in Events)
                if (cam.TimingGroup >= group.GroupNumber)
                {
                    cam.TimingGroup += 1;
                    cam.ResetTimingGroupChangedFrom();
                }

            RebuildList();
        }

        public IEnumerable<CameraEvent> FindByTiming(int from, int to)
        {
            for (var i = 0; i < Events.Count; i++)
            {
                var cam = Events[i];
                if (cam.Timing >= from && cam.Timing >= from && cam.Timing <= to) yield return cam;
            }
        }

        public IEnumerable<CameraEvent> FindWithinRange(int from, int to, bool overlapCompletely)
        {
            for (var i = 0; i < Events.Count; i++)
            {
                var cam = Events[i];
                if ((overlapCompletely && cam.Timing >= from && cam.Timing + cam.Duration <= to)
                    || (!overlapCompletely && (cam.Timing >= from || cam.Timing + cam.Duration <= to)))
                    yield return cam;
            }
        }

        public void AddTiltToCamera(float arcWorldX)
        {
            currentArcPos += arcWorldX;
        }

        public void SetPropertiesExternal(float fieldOfView, float tiltFactor)
        {
            fieldOfViewExternal = fieldOfView;
            tiltFactorExternal = tiltFactor;
        }

        public void SetTransformExternal(Vector3 translation, Quaternion rotation)
        {
            translationExternal = translation;
            rotationExternal = rotation;
        }

        public void UpdateCamera(int currentTiming)
        {
            UpdateBackgroundCanvas();
            if (IsEditorCamera)
            {
                GameplayCamera.transform.localPosition = EditorCameraPosition;
                GameplayCamera.transform.localRotation = Quaternion.Euler(EditorCameraRotation);
                return;
            }

            var prevPosition = backgroundCamera.transform.localPosition;
            var position = ResetPosition + translationExternal;
            var lookAtRotation = Quaternion.LookRotation(
                new Vector3(0, -5.5f, -20) - ResetPosition,
                new Vector3(currentTilt, 1 - currentTilt, 0)) * Quaternion.Inverse(Quaternion.Euler(ResetRotation));
            var fov = ResetFOV + fieldOfViewExternal;
            var rotation = ResetRotation;
            backgroundCamera.fieldOfView = fov;
            overlayCamera.fieldOfView = fov;

            isReset = true;
            for (var i = 0; i < Events.Count; i++)
            {
                var cam = Events[i];
                if (cam.Timing > currentTiming) break;

                isReset = cam.IsReset;
                if (isReset)
                {
                    position = ResetPosition + translationExternal;
                    rotation = ResetRotation;
                }

                var percent = cam.PercentAt(currentTiming);
                position += new Vector3(-cam.Move.x, cam.Move.y, cam.Move.z) * percent / 100f;
                rotation += new Vector3(-cam.Rotate.y, -cam.Rotate.x, cam.Rotate.z) * percent;
            }

            if (position.IsNaN() || rotation.IsNaN())
            {
                backgroundCamera.transform.localPosition = prevPosition;
            }
            else
            {
                backgroundCamera.transform.localPosition = position;
                backgroundCamera.transform.localRotation = lookAtRotation * rotationExternal *
                                                           Quaternion.Euler(0f, 0f, rotation.z) *
                                                           Quaternion.Euler(rotation.x, rotation.y, 0f);
            }

            UpdateCameraTilt();
        }

        public float CalculateDepthSquared(Vector3 fromPos)
        {
            return (backgroundCamera.transform.localPosition - fromPos).sqrMagnitude;
        }

        private void UpdateBackgroundCanvas()
        {
            backgroundRect.pivot = new Vector2(0.5f, Mathf.Lerp(0.87f, 0.5f, AspectAdjustment));
            backgroundRect.anchorMin = new Vector2(0.5f, Mathf.Lerp(1f, 0.5f, AspectAdjustment));
            backgroundRect.anchorMax = new Vector2(0.5f, Mathf.Lerp(1f, 0.5f, AspectAdjustment));
        }

        private void UpdateCameraTilt()
        {
            if (!isReset)
            {
                currentTilt = 0;
                return;
            }

            var pos = -Mathf.Clamp(currentArcPos / Values.LaneWidth, -1, 1) * Values.CameraArcPosScalar;
            var delta = pos - currentTilt;
            if (Mathf.Abs(delta) >= 0.001f)
            {
                var speed = Values.CameraTiltSpeed;
                var deltaTime = Mathf.Min(Time.deltaTime, 0.1f);
                currentTilt += speed * delta * deltaTime;
            }
            else
            {
                currentTilt = pos;
            }

            currentTilt *= tiltFactorExternal;
            currentArcPos = 0;
        }

        private void RebuildList()
        {
            Events.Sort((a, b) => a.CompareTo(b));
        }
    }
}