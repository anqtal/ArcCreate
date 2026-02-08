using System;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Gameplay.Judgement;
using UnityEngine;

namespace ArcCreate.Gameplay.Data
{
    /// <summary>
    ///     Partial class for judgement.
    /// </summary>
    public partial class Arc : LongNote, ILongNote, IArcJudgementReceiver
    {
        private bool highlightRequestSent;
        private int numJudgementRequestsSent;
        private bool spawnedParticleThisFrame;

        public void ProcessArcJudgement(bool isExpired, bool isJudgement, GroupProperties props)
        {
            var currentTiming = Services.Audio.ChartTiming;
            highlightRequestSent = false;
            var x = WorldXAt(currentTiming);
            var y = WorldYAt(currentTiming);

            // // 限制范围 [0,6]
            // var xx = Mathf.Clamp(x, -6f, 6f);
            // var yy = Mathf.Clamp(x, -6f, 6f);
            // // 线性插值计算截止频率
            // float cutoffFreq = Mathf.Lerp(10000f, 100f, xx / 12f);
            // float cutoffFreqq = Mathf.Lerp(10000f, 100f, xx / 12f);
            // BassAudioService.Instance.AudioStream.LpfFx(cutoffFreq);
            // BassAudioService.Instance.AudioStream.HpfFx(cutoffFreqq);
            // Debug.Log($"{x} {y} ccc {cutoffFreq}");
            var currentPos = new Vector3(x, y);

            if (isExpired)
            {
                SetGroupHighlight(false, int.MinValue);
                var result = props.MapJudgementResult(JudgementResult.MissLate);

                if (isJudgement)
                {
                    if (!spawnedParticleThisFrame)
                    {
                        Services.Particle.PlayTextParticle(currentPos + props.CurrentJudgementOffset, result,
                            Option<int>.None());
                        spawnedParticleThisFrame = true;
                    }

                    Services.Score.ProcessJudgement(result, Option<int>.None());
                }
            }
            else if (currentTiming <= EndTiming + Values.HoldMissLateJudgeWindow)
            {
                SetGroupHighlight(true, currentTiming + Values.HoldParticlePersistDuration);
                if (!hasBeenHitOnce) BassAudioService.Instance.PlayArcHitSound(Timing);
                //Services.Hitsound.PlayArcHitsound(Timing);
                hasBeenHitOnce = true;
                var result = props.MapJudgementResult(JudgementResult.Max);

                if (isJudgement)
                {
                    if (!spawnedParticleThisFrame)
                    {
                        Services.Particle.PlayTextParticle(currentPos + props.CurrentJudgementOffset, result,
                            Option<int>.None());
                        spawnedParticleThisFrame = true;
                    }

                    Services.Score.ProcessJudgement(result, Option<int>.None());
                }
            }
        }

        public void ResetJudgeTo(int timing)
        {
            RecalculateJudgeTimings();
            highlight = highlight && timing >= Timing && timing <= EndTiming;
            longParticleUntil = int.MinValue;
            numJudgementRequestsSent = ComboAt(timing);
            highlightRequestSent = false;
            arcGroupAlpha = 1;
            hasBeenHitOnce = hasBeenHitOnce && timing >= Timing && timing <= EndTiming;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                segment.From = 0;
                segments[i] = segment;
            }
        }

        public void UpdateJudgement(int currentTiming, GroupProperties groupProperties)
        {
            if (!IsTrace && currentTiming >= Timing && Timing < EndTiming) RequestJudgement(groupProperties);

            if (!IsTrace && currentTiming >= Timing && Timing < EndTiming && !highlightRequestSent)
            {
                RequestHighlight(currentTiming, groupProperties);
                highlightRequestSent = true;
            }

            spawnedParticleThisFrame = false;
        }

        public override void RecalculateJudgeTimings()
        {
            TotalCombo = 0;
            FirstJudgeTime = double.MaxValue;
            TimeIncrement = double.MaxValue;

            if (IsTrace || EndTiming == Timing) return;

            double bpm = TimingGroupInstance.GetBpm(Timing);

            if (bpm == 0) return;

            var duration = EndTiming - Timing;
            bpm = Math.Abs(bpm);
            TimeIncrement = (bpm >= 255 ? 60_000 : 30_000) / bpm / Values.TimingPointDensity;

            var count = (int)(duration / TimeIncrement);
            var whatTheFuckDoesThisMean = (IsFirstArcOfGroup ? 0 : 1) ^ 1;
            if (count <= whatTheFuckDoesThisMean)
            {
                TotalCombo = 1;
                FirstJudgeTime = Timing + duration / 2;
            }
            else
            {
                TotalCombo = count - whatTheFuckDoesThisMean;
                FirstJudgeTime = Timing + whatTheFuckDoesThisMean * TimeIncrement;
            }
        }

        private void RequestJudgement(GroupProperties props)
        {
            for (var t = numJudgementRequestsSent; t < TotalCombo; t++)
            {
                var timing = (int)Math.Round(FirstJudgeTime + t * TimeIncrement);
                Services.Judgement.Request(new ArcJudgementRequest
                {
                    StartAtTiming = timing - Values.GoodJudgeWindow,
                    ExpireAtTiming = timing + Values.HoldMissLateJudgeWindow,
                    AutoAtTiming = timing,
                    Arc = this,
                    IsJudgement = true,
                    Receiver = this,
                    Properties = props
                });
            }

            numJudgementRequestsSent = TotalCombo;
        }

        private void RequestHighlight(int timing, GroupProperties props)
        {
            Services.Judgement.Request(new ArcJudgementRequest
            {
                StartAtTiming = timing,
                ExpireAtTiming = timing + Values.HoldHighlightPersistDuration,
                AutoAtTiming = timing,
                Arc = this,
                IsJudgement = false,
                Receiver = this,
                Properties = props
            });
        }
    }
}