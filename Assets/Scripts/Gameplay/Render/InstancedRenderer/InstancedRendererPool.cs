using System.Collections.Generic;
using UnityEngine;

namespace ArcCreate.Gameplay.Render
{
    public class InstancedRendererPool
    {
        private readonly Material material;
        private readonly List<Material> materials = new();
        private readonly Mesh mesh;
        private readonly List<InstancedRenderer> renderers = new();
        private readonly bool useProperties;

        private int index;

        public InstancedRendererPool(Material material, Mesh mesh, bool useProperties)
        {
            this.material = material;
            this.mesh = mesh;
            this.useProperties = useProperties;
            CreateNewRenderer();
        }

        public void RegisterInstance(Matrix4x4 matrix, Color color, Vector4 property = default)
        {
            var accepted = renderers[index].RegisterInstance(matrix, color, property);
            if (!accepted)
            {
                index += 1;
                if (index >= renderers.Count) CreateNewRenderer();

                RegisterInstance(matrix, color, property);
            }
        }

        public void Draw(Camera camera, LayerMask layerMask)
        {
            for (var i = 0; i <= index; i++)
            {
                var renderer = renderers[i];
                renderer.Draw(camera, layerMask);
            }

            index = 0;
        }

        public void Dispose()
        {
            for (var i = 0; i < materials.Count; i++)
            {
                var mat = materials[i];
                Object.Destroy(mat);
            }

            materials.Clear();
        }

        private void CreateNewRenderer()
        {
            var newMat = Object.Instantiate(material);
            materials.Add(newMat);
            renderers.Add(new InstancedRenderer(newMat, mesh, useProperties));
        }
    }
}