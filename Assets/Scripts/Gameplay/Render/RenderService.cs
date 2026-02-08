using System.Collections.Generic;
using ArcCreate.Gameplay.Utility;
using UnityEngine;

namespace ArcCreate.Gameplay.Render
{
    public class RenderService : MonoBehaviour
    {
        [SerializeField] private Camera notesCamera;
        [SerializeField] private int layer;

        [Header("Meshes")] [SerializeField] private Mesh arctapMesh;

        [SerializeField] private Mesh arctapSfxMesh;
        [SerializeField] private Mesh arctapShadowMesh;
        [SerializeField] private Mesh tapMesh;
        [SerializeField] private Mesh holdMesh;
        [SerializeField] private Mesh connectionLineMesh;
        [SerializeField] private Mesh heightIndicatorMesh;
        [SerializeField] private Mesh arcCapMesh;
        [SerializeField] private Mesh arcCapControllerMesh;

        [Header("Materials")] [SerializeField] private Material baseArctapMaterial;

        [SerializeField] private Material baseTapMaterial;
        [SerializeField] private Material baseHoldMaterial;
        [SerializeField] private Material baseArcCapMaterial;
        [SerializeField] private Material arctapShadowMaterial;
        [SerializeField] private Material heightIndicatorMaterial;
        [SerializeField] private Material connectionLineMaterial;
        private readonly Dictionary<Texture, InstancedRendererPool> arcCapDrawers = new();
        private readonly IComparer<ArcDrawCall> arcDrawCallComparer = new ArcDrawCallComparer();

        // Arctap
        private readonly Dictionary<Texture, InstancedRendererPool> arctapDrawers = new();
        private readonly Dictionary<Texture, InstancedRendererPool> arctapSfxDrawers = new();

        private readonly List<Material> generatedMaterials = new();
        private readonly Dictionary<Texture, InstancedRendererPool> holdDrawers = new();

        // Arc & trace
        private readonly List<ArcDrawCall> queuedArcDrawCalls = new();
        private readonly List<ArcDrawCall> queuedTraceDrawCalls = new();

        // Floor
        private readonly Dictionary<Texture, InstancedRendererPool> tapDrawers = new();
        private InstancedRendererPool arcHeadDrawer;
        private InstancedRendererPool arcSegmentDrawer;
        private InstancedRendererPool arcShadowDrawer;
        private InstancedRendererPool arctapShadowDrawer;
        private InstancedRendererPool connectionLineDrawer;
        private InstancedRendererPool heightIndicatorDrawer;
        private InstancedRendererPool traceHeadDrawer;
        private InstancedRendererPool traceSegmentDrawer;
        private InstancedRendererPool traceShadowDrawer;

        public bool IsLoaded { get; private set; }

        public Mesh TapMesh => tapMesh;

        public Mesh HoldMesh => holdMesh;

        public Mesh ArcTapMesh => arctapMesh;

        private void Awake()
        {
            Shader.WarmupAllShaders();
            connectionLineDrawer = new InstancedRendererPool(
                connectionLineMaterial,
                connectionLineMesh,
                false);

            UpdateLoadedState();
        }

        private void OnDestroy()
        {
            foreach (var material in generatedMaterials) Destroy(material);

            generatedMaterials.Clear();

            foreach (var pair in holdDrawers) pair.Value.Dispose();

            connectionLineDrawer.Dispose();
            foreach (var pair in tapDrawers) pair.Value.Dispose();

            traceShadowDrawer.Dispose();
            arcShadowDrawer.Dispose();

            traceSegmentDrawer.Dispose();
            traceHeadDrawer.Dispose();
            arctapShadowDrawer.Dispose();

            heightIndicatorDrawer.Dispose();
            foreach (var pair in arcCapDrawers) pair.Value.Dispose();

            arcSegmentDrawer.Dispose();

            foreach (var pair in arctapDrawers) pair.Value.Dispose();

            foreach (var pair in arctapSfxDrawers) pair.Value.Dispose();

            arcHeadDrawer.Dispose();
        }

        public void DrawTap(Texture texture, Matrix4x4 matrix, Color color, bool selected)
        {
            if (!tapDrawers.ContainsKey(texture))
            {
                var newTap = Instantiate(baseTapMaterial);
                newTap.mainTexture = texture;
                generatedMaterials.Add(newTap);
                tapDrawers.Add(texture, new InstancedRendererPool(
                    newTap,
                    tapMesh,
                    true));
            }

            tapDrawers[texture].RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0));
        }

        public void DrawHold(Texture texture, Matrix4x4 matrix, Color color, bool selected, float from, bool highlight)
        {
            if (!holdDrawers.ContainsKey(texture))
            {
                var newHold = Instantiate(baseHoldMaterial);
                newHold.mainTexture = texture;
                generatedMaterials.Add(newHold);
                holdDrawers.Add(texture, new InstancedRendererPool(
                    newHold,
                    holdMesh,
                    true));
            }

            holdDrawers[texture]
                .RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, from, highlight ? 1 : 0, 0));
        }

        public void DrawConnectionLine(Matrix4x4 matrix, Color color)
        {
            connectionLineDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcSegment(int colorId, bool highlight, Matrix4x4 matrix, Color color, bool selected,
            float redValue, float y, float depth)
        {
            var (high, low) = Services.Skin.GetArcColor(colorId);
            color *= Color.Lerp(Color.Lerp(low, high, (y - 1) / 4.5f), Color.red, redValue);
            var properties = new Vector4(selected ? 1 : 0, highlight ? 1 : 0, 0, 0);
            queuedArcDrawCalls.Add(new ArcDrawCall
            {
                Matrix = matrix,
                Color = color,
                Properties = properties,
                Depth = depth
            });
        }

        public void DrawTraceSegment(Matrix4x4 matrix, Color color, bool selected, float depth)
        {
            queuedTraceDrawCalls.Add(new ArcDrawCall
            {
                Matrix = matrix,
                Color = color,
                Properties = new Vector4(selected ? 1 : 0, 0, 0, 0),
                Depth = depth
            });
        }

        public void DrawArcShadow(Matrix4x4 matrix, Color color, Vector4 cornerOffset)
        {
            arcShadowDrawer.RegisterInstance(matrix, color, cornerOffset);
        }

        public void DrawTraceShadow(Matrix4x4 matrix, Color color)
        {
            traceShadowDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcHead(int colorId, bool highlight, Matrix4x4 matrix, Color color, bool selected,
            float redValue, float y)
        {
            var (high, low) = Services.Skin.GetArcColor(colorId);
            color *= Color.Lerp(Color.Lerp(low, high, (y - 1) / 4.5f), Color.red, redValue);
            var properties = new Vector4(selected ? 1 : 0, highlight ? 1 : 0, 0, 0);
            arcHeadDrawer.RegisterInstance(matrix, color, properties);
        }

        public void DrawTraceHead(Matrix4x4 matrix, Color color, bool selected)
        {
            traceHeadDrawer.RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0));
        }

        public void DrawArcCap(Texture texture, Matrix4x4 matrix, Color color, bool isController)
        {
            if (!arcCapDrawers.ContainsKey(texture))
            {
                var newArcCap = Instantiate(baseArcCapMaterial);
                newArcCap.mainTexture = texture;
                generatedMaterials.Add(newArcCap);
                arcCapDrawers.Add(texture, new InstancedRendererPool(
                    newArcCap,
                    isController ? arcCapControllerMesh : arcCapMesh,
                    false));
            }

            arcCapDrawers[texture].RegisterInstance(matrix, color);
        }

        public void DrawHeightIndicator(Matrix4x4 matrix, Color color)
        {
            heightIndicatorDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcTap(bool sfx, Texture texture, Matrix4x4 matrix, Color color, bool selected)
        {
            var drawer = sfx ? arctapSfxDrawers : arctapDrawers;
            if (!drawer.ContainsKey(texture))
            {
                var newArctap = Instantiate(baseArctapMaterial);
                newArctap.mainTexture = texture;
                generatedMaterials.Add(newArctap);
                drawer.Add(texture, new InstancedRendererPool(
                    newArctap,
                    sfx ? arctapSfxMesh : arctapMesh,
                    true));
            }

            drawer[texture].RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0));
        }

        public void DrawArcTapShadow(Matrix4x4 matrix, Color color)
        {
            arctapShadowDrawer.RegisterInstance(matrix, color);
        }

        public void UpdateRenderers()
        {
            if (!notesCamera.enabled || !notesCamera.gameObject.activeInHierarchy) return;

            foreach (var pair in holdDrawers) pair.Value.Draw(notesCamera, layer);

            connectionLineDrawer.Draw(notesCamera, layer);
            foreach (var pair in tapDrawers) pair.Value.Draw(notesCamera, layer);

            traceShadowDrawer.Draw(notesCamera, layer);
            arcShadowDrawer.Draw(notesCamera, layer);

            queuedTraceDrawCalls.Sort(arcDrawCallComparer);
            foreach (var call in queuedTraceDrawCalls)
                traceSegmentDrawer.RegisterInstance(
                    call.Matrix,
                    call.Color,
                    call.Properties);

            traceSegmentDrawer.Draw(notesCamera, layer);
            queuedTraceDrawCalls.Clear();

            traceHeadDrawer.Draw(notesCamera, layer);
            arctapShadowDrawer.Draw(notesCamera, layer);

            heightIndicatorDrawer.Draw(notesCamera, layer);
            foreach (var pair in arcCapDrawers) pair.Value.Draw(notesCamera, layer);

            queuedArcDrawCalls.Sort(arcDrawCallComparer);
            foreach (var call in queuedArcDrawCalls)
                arcSegmentDrawer.RegisterInstance(
                    call.Matrix,
                    call.Color,
                    call.Properties);

            arcSegmentDrawer.Draw(notesCamera, layer);
            queuedArcDrawCalls.Clear();

            foreach (var pair in arctapDrawers) pair.Value.Draw(notesCamera, layer);

            foreach (var pair in arctapSfxDrawers) pair.Value.Draw(notesCamera, layer);

            arcHeadDrawer.Draw(notesCamera, layer);
        }

        public void SetTraceMaterial(Material material)
        {
            traceSegmentDrawer?.Dispose();
            traceSegmentDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetSegmentMesh(true),
                true);

            traceHeadDrawer?.Dispose();
            traceHeadDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetHeadMesh(true),
                true);

            UpdateLoadedState();
        }

        public void SetShadowMaterial(Material material)
        {
            arcShadowDrawer?.Dispose();
            arcShadowDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetShadowMesh(false),
                true);

            traceShadowDrawer?.Dispose();
            traceShadowDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetShadowMesh(true),
                false);

            UpdateLoadedState();
        }

        public void SetArcMaterial(Material material)
        {
            arcSegmentDrawer?.Dispose();
            arcHeadDrawer?.Dispose();

            arcSegmentDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetSegmentMesh(false),
                true);

            arcHeadDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetHeadMesh(false),
                true);

            UpdateLoadedState();
        }

        public void SetTextures(Texture heightIndicator, Texture arctapShadow)
        {
            heightIndicatorDrawer?.Dispose();
            arctapShadowDrawer?.Dispose();

            var height = Instantiate(heightIndicatorMaterial);
            height.mainTexture = heightIndicator;
            generatedMaterials.Add(height);
            heightIndicatorDrawer = new InstancedRendererPool(
                height,
                heightIndicatorMesh,
                false);

            var shadow = Instantiate(arctapShadowMaterial);
            shadow.mainTexture = arctapShadow;
            generatedMaterials.Add(shadow);
            arctapShadowDrawer = new InstancedRendererPool(
                shadow,
                arctapShadowMesh,
                false);

            UpdateLoadedState();
        }

        private void UpdateLoadedState()
        {
            IsLoaded = connectionLineDrawer != null
                       && arcSegmentDrawer != null
                       && arcHeadDrawer != null
                       && traceSegmentDrawer != null
                       && traceHeadDrawer != null
                       && arcShadowDrawer != null
                       && traceShadowDrawer != null
                       && connectionLineDrawer != null
                       && heightIndicatorDrawer != null
                       && arctapShadowDrawer != null;
        }
    }
}