using UnityEngine;

namespace ArcCreate.Gameplay
{
    public static class Projection
    {
        /// <summary>
        ///     Cast ray onto vertical input plane and track.
        /// </summary>
        /// <param name="cameraRay">The camera ray to cast.</param>
        /// <returns>Tuple containing: The lane hit, the vertical position hit.</returns>
        public static (int lane, float verticalX, float verticalY) CastRayOntoPlayfield(Ray cameraRay)
        {
            var origin = cameraRay.origin;
            var dir = cameraRay.direction.normalized;

            // Cast ray onto xy plane at z=0
            var zratio = -origin.z / dir.z;
            var verticalX = origin.x + dir.x * zratio;
            var verticalY = origin.y + dir.y * zratio;

            // Cast ray onto xz plane at y=0
            var yratio = -origin.y / dir.y;
            var lProjPosX = origin.x + dir.x * yratio;
            var lProjPosZ = origin.z + dir.z * yratio;

            var lane = Values.InvalidLane;

            // Check if cast falls out of acceptable range
            if (lProjPosZ >= -Values.MinInputLaneZ && lProjPosZ <= Values.TrackLengthBackward)
            {
                lane = ArcFormula.WorldXToLane(lProjPosX);
                lane = (int)Mathf.Clamp(lane, Values.LaneFrom, Values.LaneTo);
            }

            return (lane, verticalX, verticalY);
        }
    }
}