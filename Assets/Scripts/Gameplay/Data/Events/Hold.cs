using System;
using System.Collections.Generic;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Gameplay.Judgement;
using ArcCreate.Utility;
using ArcCreate.Utility.Extension;
using UnityEngine;

namespace ArcCreate.Gameplay.Data
{
    public class Hold : LongNote, ILongNote, ILaneTapJudgementReceiver, ILaneHoldJudgementReceiver
    {
        private int flashCount;
        private bool highlight;
        private bool holdHighlightRequestSent;
        private int longParticleUntil = int.MinValue;
        private int numHoldJudgementRequestsSent;
        private bool spawnedParticleThisFrame;
        private bool tapJudgementRequestSent;
        private Texture texture;

        public int Lane { get; set; }

        public bool IsLocked { get; private set; } = true;

        public void ProcessLaneHoldJudgement(bool isExpired, bool isJudgement, GroupProperties props,
            int startTiming = 0)
        {
            var currentTiming = Services.Audio.ChartTiming;
            if (!isJudgement) holdHighlightRequestSent = false;

            if (isExpired)
            {
                longParticleUntil = int.MinValue;
                highlight = false;
                var result = props.MapJudgementResult(JudgementResult.MissLate);

                if (isJudgement)
                {
                    Services.Score.ProcessJudgement(result, Option<int>.None());
                    if (!spawnedParticleThisFrame)
                    {
                        PlayParticle(result, props.CurrentJudgementOffset);
                        spawnedParticleThisFrame = true;
                    }
                }
            }
            else
            {
                longParticleUntil = currentTiming + Values.HoldParticlePersistDuration;
                highlight = true;
                var result = props.MapJudgementResult(JudgementResult.Max);

                if (isJudgement)
                {
                    Services.Score.ProcessJudgement(result, Option<int>.None());
                    if (!spawnedParticleThisFrame)
                    {
                        PlayParticle(result, props.CurrentJudgementOffset);
                        spawnedParticleThisFrame = true;
                    }
                }
            }
        }

        public void ProcessLaneTapJudgement(int offset, GroupProperties props)
        {
            var currentTiming = Services.Audio.ChartTiming;
            if (currentTiming >= EndTiming + Values.GoodJudgeWindow) return;

            IsLocked = false;
            tapJudgementRequestSent = false;

            longParticleUntil = currentTiming + Values.HoldParticlePersistDuration;
            highlight = true;
            BassAudioService.Instance.PlayTapHitSound(Timing);
            Services.InputFeedback.LaneFeedback(Lane);
            Services.Particle.PlayHoldParticle(this,
                new Vector3(ArcFormula.LaneToWorldX(Lane), 0, 0) + props.CurrentJudgementOffset);


            // Extend the note back
            if (currentTiming < Timing) FloorPosition = TimingGroupInstance.GetFloorPosition(currentTiming);
        }

        public void ResetJudgeTo(int timing)
        {
            RecalculateJudgeTimings();
            IsLocked = true;
            highlight = false;
            longParticleUntil = int.MinValue;
            tapJudgementRequestSent = false;
            numHoldJudgementRequestsSent = ComboAt(timing);
            holdHighlightRequestSent = false;
            FloorPosition = TimingGroupInstance.GetFloorPosition(Timing);
        }

        public void Rebuild()
        {
            RecalculateFloorPosition();
            RecalculateJudgeTimings();
        }

        public void ReloadSkin()
        {
            texture = Services.Skin.GetHoldSkin(this);
        }

        public void UpdateJudgement(int currentTiming, GroupProperties groupProperties)
        {
            if (currentTiming >= Timing - Values.MissJudgeWindow && IsLocked && !tapJudgementRequestSent)
            {
                RequestTapJudgement(groupProperties);
                tapJudgementRequestSent = true;
            }

            if (currentTiming >= Timing) RequestHoldJudgement(groupProperties);

            if (currentTiming >= Timing && !holdHighlightRequestSent)
            {
                RequestHoldHighlight(currentTiming, groupProperties);
                holdHighlightRequestSent = true;
            }

            spawnedParticleThisFrame = false;
        }

        public void UpdateRender(int currentTiming, double currentFloorPosition, GroupProperties groupProperties)
        {
            if (texture == null) ReloadSkin();

            var z = ZPos(currentFloorPosition);
            var endZ = EndZPos(currentFloorPosition);
            var pos = groupProperties.FallDirection * z + new Vector3(ArcFormula.LaneToWorldX(Lane), 0, 0);
            var rot = groupProperties.RotationIndividual;
            var scl = groupProperties.ScaleIndividual;
            var matrix = groupProperties.GroupMatrix
                         * Matrix4x4.TRS(pos, rot, scl)
                         * MatrixUtility.Shear(groupProperties.FallDirection * (z - endZ));

            float alpha = 1;
            if (highlight)
            {
                flashCount = (flashCount + 1) % Values.HoldFlashCycle;
                if (flashCount == 0) alpha = Values.FlashHoldAlphaScalar;
            }
            else
            {
                if (currentTiming >= Timing)
                {
                    if (groupProperties.FadingHolds)
                    {
                        var lastHit = Mathf.Max(longParticleUntil, Timing);
                        var t = (float)(currentTiming - lastHit - Values.FadingHoldsFadeDelay) /
                                Values.FadingHoldsFadeDuration;
                        alpha = Mathf.Lerp(1, Values.MissedHoldAlphaScalar, t);
                    }
                    else
                    {
                        alpha = Values.MissedHoldAlphaScalar;
                    }
                }
            }

            alpha *= Values.MaxHoldAlpha;
            var color = groupProperties.Color;
            color.a *= alpha;

            float from = 0;
            if ((!IsLocked || groupProperties.NoInput) && !groupProperties.NoClip)
                from = (float)((currentFloorPosition - FloorPosition) / (EndFloorPosition - FloorPosition));

            Services.Render.DrawHold(texture, matrix, color, IsSelected, from, highlight);

            if (currentTiming <= longParticleUntil && currentTiming <= EndTiming)
                Services.Particle.PlayHoldParticle(this,
                    new Vector3(ArcFormula.LaneToWorldX(Lane), 0, 0) + groupProperties.CurrentJudgementOffset);
        }

        public int CompareTo(INote other)
        {
            var note = other as LongNote;
            if (note.Timing == Timing) return EndTiming.CompareTo(note.EndTiming);

            return Timing.CompareTo(note.EndTiming);
        }

        public override ArcEvent Clone()
        {
            return new Hold
            {
                Timing = Timing,
                EndTiming = EndTiming,
                TimingGroup = TimingGroup,
                Lane = Lane
            };
        }

        public override void Assign(ArcEvent newValues)
        {
            base.Assign(newValues);
            var e = newValues as Hold;
            Lane = e.Lane;
        }

        public override void RecalculateJudgeTimings()
        {
            TotalCombo = 0;
            double bpm = TimingGroupInstance.GetBpm(Timing);

            if (bpm == 0 || EndTiming == Timing)
            {
                FirstJudgeTime = double.MaxValue;
                TimeIncrement = double.MaxValue;
                return;
            }

            var duration = EndTiming - Timing;
            bpm = Math.Abs(bpm);
            TimeIncrement = (bpm >= 255 ? 60_000 : 30_000) / bpm / Values.TimingPointDensity;

            var count = (int)(duration / TimeIncrement);
            if (count <= 1)
            {
                TotalCombo = 1;
                FirstJudgeTime = Timing + duration / 2;
            }
            else
            {
                TotalCombo = count - 1;
                FirstJudgeTime = Timing + TimeIncrement;
            }
        }

        public override void GenerateColliderTriangles(int timing, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = Services.Render.HoldMesh;
            vertices.Clear();
            triangles.Clear();
            mesh.GetVertices(vertices);
            mesh.GetTriangles(triangles, 0);

            var fp = TimingGroupInstance.GetFloorPosition(timing);
            var z = ZPos(fp);
            var endZ = EndZPos(fp);
            var basePos = new Vector3(ArcFormula.LaneToWorldX(Lane), 0, 0);
            var pos = TimingGroupInstance.GroupProperties.FallDirection * z + basePos;
            var scl = TimingGroupInstance.GroupProperties.ScaleIndividual;
            scl.z *= z - endZ;

            for (var i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                v = v.Multiply(scl);
                v += pos;
                vertices[i] = v;
            }
        }

        private void RequestTapJudgement(GroupProperties props)
        {
            Services.Judgement.Request(new LaneTapJudgementRequest
            {
                ExpireAtTiming = EndTiming + Values.GoodJudgeWindow,
                AutoAtTiming = Timing,
                Lane = Lane,
                Receiver = this,
                Properties = props
            });
        }

        private void RequestHoldJudgement(GroupProperties props)
        {
            for (var t = numHoldJudgementRequestsSent; t < TotalCombo; t++)
            {
                var timing = (int)Math.Round(FirstJudgeTime + t * TimeIncrement);

                Services.Judgement.Request(new LaneHoldJudgementRequest
                {
                    StartAtTiming = timing - Values.GoodJudgeWindow,
                    ExpireAtTiming = timing + Values.HoldMissLateJudgeWindow,
                    AutoAtTiming = timing,
                    Lane = Lane,
                    IsJudgement = true,
                    Receiver = this,
                    Properties = props
                });
            }

            numHoldJudgementRequestsSent = TotalCombo;
        }

        private void RequestHoldHighlight(int timing, GroupProperties props)
        {
            Services.Judgement.Request(new LaneHoldJudgementRequest
            {
                StartAtTiming = timing,
                ExpireAtTiming = timing + Values.HoldHighlightPersistDuration,
                AutoAtTiming = timing,
                Lane = Lane,
                IsJudgement = false,
                Receiver = this,
                Properties = props
            });
        }

        private void PlayParticle(JudgementResult result, Vector3 judgeOffset)
        {
            Services.Particle.PlayTextParticle(
                new Vector3(ArcFormula.LaneToWorldX(Lane), 0, 0) + judgeOffset,
                result,
                Option<int>.None());
        }
    }
}