using ArcCreate.Gameplay.Chart;
using UnityEngine;

namespace ArcCreate.Gameplay.Data
{
    public struct ArcSegmentData
    {
        public int Timing;

        public int EndTiming;

        public Vector3 StartPosition;

        public Vector3 EndPosition;

        public double FloorPosition;

        public double EndFloorPosition;

        public float From;

        public float CalculateZPos(double currentFloorPosition)
        {
            return ArcFormula.FloorPositionToZ(FloorPosition - currentFloorPosition);
        }

        public float CalculateEndZPos(double currentFloorPosition)
        {
            return ArcFormula.FloorPositionToZ(EndFloorPosition - currentFloorPosition);
        }

        public (Matrix4x4 body, Matrix4x4 shadow, Vector4 cornerOffset) GetMatrices(double floorPosition,
            Vector3 fallDirection, float baseZ, float baseY)
        {
            var startZ = ArcFormula.FloorPositionToZ(FloorPosition - floorPosition);
            var endZ = ArcFormula.FloorPositionToZ(EndFloorPosition - floorPosition);
            var startPos = StartPosition + (startZ - baseZ) * fallDirection;
            var endPos = EndPosition + (endZ - baseZ) * fallDirection;
            startPos = (endPos - startPos) * From + startPos;
            var dir = endPos - startPos;

            var bodyMatrix = new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(dir.x, dir.y, dir.z, 0),
                new Vector4(startPos.x, startPos.y, startPos.z, 1));

            var shadowMatrix = new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(dir.x, 0, dir.z, 0),
                new Vector4(startPos.x, -baseY, startPos.z, 1));

            return (bodyMatrix, shadowMatrix, Vector4.zero);
        }

        public (Matrix4x4 body, Matrix4x4 shadow, Vector4 cornerOffset)
            GetMatricesSlam(double floorPosition, Vector3 fallDirection, float baseZ, Vector3 basePos,
                TimingGroup group, Arc next, float offset)
        {
            if (From > 0) return (Matrix4x4.zero, Matrix4x4.zero, Vector4.zero);

            var startZ = ArcFormula.FloorPositionToZ(FloorPosition - floorPosition);
            var startPos = StartPosition + (startZ - baseZ) * fallDirection;
            var endPos = EndPosition + (startZ - baseZ) * fallDirection;
            var dir = endPos - startPos;

            var bodyMatrix = new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(dir.x, dir.y, dir.z, 0),
                new Vector4(startPos.x, startPos.y, startPos.z, 1));

            // Accounting for angled arcs being less wide than straight arcs
            var zLength = offset * 2;
            if (next != null && next.TryGetFirstSegement(out var nextFirstSeg)
                             && nextFirstSeg.EndTiming > nextFirstSeg.Timing)
            {
                var dz = Mathf.Abs(
                    ArcFormula.FloorPositionToZ(nextFirstSeg.EndFloorPosition - nextFirstSeg.FloorPosition));
                var dx = Mathf.Abs(nextFirstSeg.StartPosition.x - nextFirstSeg.EndPosition.x);
                zLength = Mathf.Sqrt(4 * offset * offset * dz * dz / (dz * dz + dx * dx));
            }

            var fpOffset = ArcFormula.ZToFloorPosition(zLength);

            // Arc might go backward or forward in time, need to check both direction.
            var slamForwardTiming = group.GetTimingFromFloorPosition(FloorPosition + fpOffset);
            var slamBackwardTiming = group.GetTimingFromFloorPosition(FloorPosition - fpOffset);
            var endOfSlamTiming = Mathf.Max(slamForwardTiming, slamBackwardTiming);

            // Likely speed=0. Do not render shadow.
            if (endOfSlamTiming <= Timing) return (bodyMatrix, Matrix4x4.zero, Vector4.zero);

            var isPositiveSpeed = slamForwardTiming > slamBackwardTiming;

            // Get the necessary coords.
            //         nf                                   ^ Z axis
            //    +    x     +flx - - - - - - - - +frx      |
            //    |    |     |       shadow       .         |
            //    | next arc |               pf   .         |
            //    +----x-----+nlx - -  +-----x----+nrx      |
            //         nn              | prev arc |         |
            //                         |     |    |         |
            //                         |     x    |         |
            //                               pn             |
            // ----------------------------------> X axis
            var pf = startPos.x;
            var nn = endPos.x;
            var nf = next == null || next.EndTiming <= next.Timing
                ? nn
                : next.WorldSegmentedXAt(endOfSlamTiming) - basePos.x;

            float nrx, nlx, frx, flx = 0;
            if (nn < pf)
            {
                nrx = pf + offset;
                nlx = nn + offset;
                flx = Mathf.Min(nf + offset, nrx);
                frx = Mathf.Max(nf + offset, nrx);
            }
            else
            {
                nlx = pf - offset;
                nrx = nn - offset;
                flx = Mathf.Min(nf - offset, nlx);
                frx = Mathf.Max(nf - offset, nlx);
            }

            zLength *= isPositiveSpeed ? 1 : -1;
            var zn = startPos.z;
            var zf = zn + zLength;

            var width = (nrx - nlx) / (offset * 2);
            var shadowMatrix = new Matrix4x4(
                new Vector4(width, 0, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(0, 0, zLength, 0),
                new Vector4((nlx + nrx) / 2, -basePos.y, 0, 1));

            var cornerOffset = new Vector4(0, frx - nrx, 0, flx - nlx) / width;
            return (bodyMatrix, shadowMatrix, cornerOffset);
        }
    }
}