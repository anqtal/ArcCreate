using System.Collections.Generic;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Gameplay.Judgement;
using ArcCreate.Utility.Extension;
using UnityEngine;

namespace ArcCreate.Gameplay.Data
{
    public class Tap : Note, INote, ILaneTapJudgementReceiver
    {
        private Color connectionLineColor;
        private bool isHit;
        private bool judgementRequestSent;
        private Texture texture;

        public HashSet<ArcTap> ConnectedArcTaps { get; } = new();

        public int Lane { get; set; }

        public void ProcessLaneTapJudgement(int offset, GroupProperties props)
        {
            var judgeOffset = props.CurrentJudgementOffset;
            var result = props.MapJudgementResult(offset.CalculateJudgeResult());
            Services.Particle.PlayTapParticle(new Vector3(ArcFormula.LaneToWorldX(Lane), 0) + judgeOffset, result);
            Services.Particle.PlayTextParticle(new Vector3(ArcFormula.LaneToWorldX(Lane), 0) + judgeOffset, result,
                offset);
            Services.Score.ProcessJudgement(result, offset);
            isHit = true;
            if (!result.IsMiss())
            {
                Services.InputFeedback.LaneFeedback(Lane);
                BassAudioService.Instance.PlayTapHitSound(Timing);
            }
        }

        public void ResetJudgeTo(int timing)
        {
            judgementRequestSent = timing > Timing;
            isHit = timing > Timing;
        }

        public void Rebuild()
        {
            RecalculateFloorPosition();
        }

        public void ReloadSkin()
        {
            (texture, connectionLineColor) = Services.Skin.GetTapSkin(this);
        }

        public int CompareTo(INote other)
        {
            return Timing.CompareTo(other.Timing);
        }

        public void UpdateJudgement(int currentTiming, GroupProperties groupProperties)
        {
            if (!judgementRequestSent && currentTiming >= Timing)
            {
            }

            if (!judgementRequestSent && currentTiming <= Timing)
            {
                RequestJudgement(groupProperties);
                judgementRequestSent = true;
            }
        }

        public void UpdateRender(int currentTiming, double currentFloorPosition, GroupProperties groupProperties)
        {
            if (isHit && !groupProperties.NoClip) return;

            if (texture == null) ReloadSkin();

            var z = ZPos(currentFloorPosition);
            var basePos = new Vector3(ArcFormula.LaneToWorldX(Lane), 0, 0);
            var pos = groupProperties.FallDirection * z + basePos;
            var rot = groupProperties.RotationIndividual;
            var scl = groupProperties.ScaleIndividual;
            scl.z *= ArcFormula.CalculateTapSizeScalar(z);
            var matrix = groupProperties.GroupMatrix * Matrix4x4.TRS(pos, rot, scl);

            var alpha = ArcFormula.CalculateFadeOutAlpha(z);
            var color = groupProperties.Color;
            var connectionColor = connectionLineColor;
            color.a *= alpha;
            connectionColor.a *= alpha;

            Services.Render.DrawTap(texture, matrix, color, IsSelected);

            if (!groupProperties.NoConnection)
                foreach (var arctap in ConnectedArcTaps)
                {
                    if (arctap.TimingGroupInstance.GroupProperties.NoConnection) return;

                    var arctapPos = new Vector3(arctap.WorldX, arctap.WorldY, 0);
                    var direction = arctapPos - basePos;

                    var lineMatrix = matrix * Matrix4x4.TRS(
                        Vector3.zero,
                        Quaternion.LookRotation(direction, Vector3.up),
                        new Vector3(1, 1, direction.magnitude));
                    Services.Render.DrawConnectionLine(lineMatrix, connectionColor);
                }
        }

        public override ArcEvent Clone()
        {
            return new Tap
            {
                Timing = Timing,
                TimingGroup = TimingGroup,
                Lane = Lane
            };
        }

        public override void Assign(ArcEvent newValues)
        {
            base.Assign(newValues);
            var e = newValues as Tap;
            Lane = e.Lane;
        }

        public override void GenerateColliderTriangles(int timing, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = Services.Render.TapMesh;
            vertices.Clear();
            triangles.Clear();
            mesh.GetVertices(vertices);
            mesh.GetTriangles(triangles, 0);

            var z = ZPos(TimingGroupInstance.GetFloorPosition(timing));
            var basePos = new Vector3(ArcFormula.LaneToWorldX(Lane), 0, 0);
            var pos = TimingGroupInstance.GroupProperties.FallDirection * z + basePos;
            var scl = TimingGroupInstance.GroupProperties.ScaleIndividual;
            scl.z *= ArcFormula.CalculateTapSizeScalar(z);

            for (var i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                v = v.Multiply(scl);
                v += pos;
                vertices[i] = v;
            }
        }

        private void RequestJudgement(GroupProperties props)
        {
            Services.Judgement.Request(
                new LaneTapJudgementRequest
                {
                    ExpireAtTiming = Timing + Values.MissJudgeWindow,
                    AutoAtTiming = Timing,
                    Lane = Lane,
                    Receiver = this,
                    Properties = props
                });
        }
    }
}