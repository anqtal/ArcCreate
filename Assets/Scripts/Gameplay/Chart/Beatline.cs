using UnityEngine;

namespace ArcCreate.Gameplay.Chart
{
    public class Beatline
    {
        private readonly float thickness;
        private BeatlineBehaviour instance;

        public Beatline(int timing, double floorPosition, float thickness, Color color)
        {
            this.Timing = timing;
            this.FloorPosition = floorPosition;
            this.Color = color;
            this.thickness = thickness;
        }

        public int Timing { get; }

        public bool IsAssignedInstance => instance != null;

        public double FloorPosition { get; }

        public Color Color { get; }

        public void AssignInstance(BeatlineBehaviour behaviour)
        {
            instance = behaviour;
        }

        public BeatlineBehaviour RevokeInstance()
        {
            var result = instance;
            instance = null;
            return result;
        }

        public void UpdateInstance(double floorPosition)
        {
            if (instance != null)
            {
                var z = ArcFormula.FloorPositionToZ(FloorPosition - floorPosition);
                instance.transform.localPosition = new Vector3(0, 0, z);
                instance.transform.localScale = new Vector3(
                    instance.transform.localScale.x,
                    ArcFormula.CalculateBeatlineSizeScalar(thickness, z),
                    1);
            }

            instance.SetColor(Color);
        }
    }
}