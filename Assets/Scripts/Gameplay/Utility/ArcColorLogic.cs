using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcCreate.Gameplay
{
    /// <summary>
    ///     Handler for a single arc color.
    /// </summary>
    public class ArcColorLogic
    {
        public const int UnassignedFingerId = int.MinValue;

        // In an attempt to contain all the logic into a single class,
        // expect the innerworkings of this to be very messy...
        private static readonly List<ArcColorLogic> Instances = new();
        private static int frameTiming;
        private bool assignedFingerExistsThisFrame;
        private int assignedFingerId = UnassignedFingerId;
        private bool assignedFingerMissedThisFrame;
        private bool existsArcWithinRangeThisFrame;
        private int graceUntil = int.MinValue;
        private bool isAssigningThisFrame;
        private int lockUntil = int.MinValue;
        private float minDistanceThisFrame = float.MaxValue;
        private float redArcDuration = 1;
        private bool wrongFingerHitThisFrame;

        private ArcColorLogic(int color)
        {
            this.Color = color;
        }

        public static int MaxColor => Instances.Count - 1;

        /// <summary>
        ///     Gets the color id of this instance.
        /// </summary>
        public int Color { get; }

        /// <summary>
        ///     Gets the currently assigned finger id to this color.
        /// </summary>
        public int AssignedFingerId
        {
            get => assignedFingerId;
            private set
            {
                assignedFingerId = value;
                IsFingerAssigned = value != UnassignedFingerId;
            }
        }

        /// <summary>
        ///     Gets the red arc value between 0 and 1.
        /// </summary>
        public float RedArcValue { get; private set; }

        private bool IsFingerAssigned { get; set; }

        private bool IsInputLocked
            => frameTiming <= lockUntil;

        private bool IsGraceActive
            => frameTiming <= graceUntil;

        /// <summary>
        ///     Get the instance for a color.
        /// </summary>
        /// <param name="color">The arc color.</param>
        /// <returns>The instance for the color.</returns>
        public static ArcColorLogic Get(int color)
        {
            if (color < 0) throw new Exception();

            while (color >= Instances.Count) Instances.Add(new ArcColorLogic(Instances.Count));

            return Instances[color];
        }

        /// <summary>
        ///     Remove all saved color instances.
        /// </summary>
        public static void ResetAll()
        {
            for (var i = 0; i < Instances.Count; i++) Services.Skin.ApplyRedArcValue(i, 0);

            Instances.Clear();
        }

        public static void ApplyRedValue()
        {
            for (var i = 0; i < Instances.Count; i++)
            {
                var color = Instances[i];
                Services.Skin.ApplyRedArcValue(i, color.RedArcValue);
            }
        }

        /// <summary>
        ///     Notify all colors that a new frame has started at the timing.
        ///     It's important that this method is called BEFORE any other method each frame.
        /// </summary>
        /// <param name="timing">The timing of the frame.</param>
        public static void NewFrame(int timing)
        {
            var lastFrameTiming = frameTiming;
            frameTiming = timing;
            for (var i = 0; i < Instances.Count; i++)
            {
                var color = Instances[i];
                color.ResetIntraframeState();
                color.UpdateRedArcValue(frameTiming - lastFrameTiming);
            }
        }

        /// <summary>
        ///     Notify that two arcs of different colors collided and a grace period should start.
        /// </summary>
        public static void StartGracePeriodForAllColors()
        {
            for (var i = 0; i < Instances.Count; i++)
            {
                var color = Instances[i];
                color.StartGracePeriod();
            }
        }

        /// <summary>
        ///     Notify that two arcs of different colors collided and a grace period should start.
        /// </summary>
        public void StartGracePeriod()
        {
            graceUntil = frameTiming + Values.ArcGraceDuration;
            UnlockInput();
            ResetAssignedFinger();
        }

        /// <summary>
        ///     Notify that a finger is no longer touching the screen.
        /// </summary>
        /// <param name="fingerId">The finger id.</param>
        /// <param name="arcJudgeInterval">The judgement interval of an arc of this color.</param>
        public void FingerLifted(int fingerId, float arcJudgeInterval)
        {
            if (fingerId == AssignedFingerId)
            {
                ResetAssignedFinger();

                if (existsArcWithinRangeThisFrame) LockInput(arcJudgeInterval);
            }
        }

        /// <summary>
        ///     Notify that a finger has hit an arc of this color.
        /// </summary>
        /// <param name="fingerId">The finger id.</param>
        /// <param name="distance">The distance between the finger and an arc of this color.</param>
        /// <param name="arcJudgeInterval">The judgement interval of an arc of this color.</param>
        public void FingerHit(int fingerId, float distance, float arcJudgeInterval)
        {
            if (IsFingerAssigned)
            {
                if (!IsGraceActive && assignedFingerMissedThisFrame)
                {
                    FlashRedArc(arcJudgeInterval);
                }
                else
                {
                    if (fingerId == AssignedFingerId)
                        ResetRedArcValue();
                    else if (!IsGraceActive) wrongFingerHitThisFrame = true;
                }
            }

            if (IsInputLocked) FlashRedArc(arcJudgeInterval);

            if (!IsFingerAssigned || isAssigningThisFrame)
            {
                if (!IsGraceActive && IsFingerAssignedToAnotherColor(fingerId))
                {
                    ConstantRedArc();
                }
                else if (!IsInputLocked && !IsGraceActive)
                {
                    if (distance < minDistanceThisFrame)
                    {
                        minDistanceThisFrame = distance;
                        AssignedFingerId = fingerId;
                        assignedFingerExistsThisFrame = true;
                        ResetRedArcValue();
                    }

                    isAssigningThisFrame = true;
                }
            }

            Services.Judgement.Debug.ShowFingerHit(Color, fingerId);
        }

        /// <summary>
        ///     Notify that a finger has not hit an arc of this color.
        /// </summary>
        /// <param name="fingerId">The finger id.</param>
        /// <param name="arcJudgeInterval">The judgement interval of an arc of this color.</param>
        public void FingerMiss(int fingerId, float arcJudgeInterval)
        {
            if (fingerId == AssignedFingerId)
            {
                assignedFingerMissedThisFrame = true;
                if (wrongFingerHitThisFrame) FlashRedArc(arcJudgeInterval);
            }

            Services.Judgement.Debug.ShowFingerMiss(Color, fingerId);
        }

        /// <summary>
        ///     Notify whether or not there are arcs within judgement range.
        ///     It's important that this method is called BEFORE any finger state notifying methods.
        /// </summary>
        /// <param name="exists">Whether or not there are arc within judgement range.</param>
        public void ExistsArcWithinRange(bool exists)
        {
            if (!exists)
            {
                UnlockInput();
                ResetRedArcValue();
            }

            Services.Judgement.Debug.ShowExistsArc(Color, exists);
            existsArcWithinRangeThisFrame = exists;
        }

        /// <summary>
        ///     Notify whether or not a finger exist this frame.
        ///     It's important that this method is called BEFORE any finger state notifying methods.
        /// </summary>
        /// <param name="id">The finger id.</param>
        public void FingerExists(int id)
        {
            if (id == assignedFingerId) assignedFingerExistsThisFrame = true;
        }

        /// <summary>
        ///     Check whether or not to accept input from a finger id.
        /// </summary>
        /// <param name="fingerId">The finger id.</param>
        /// <returns>Whether or not to accept input from the finger id.</returns>
        public bool ShouldAcceptInput(int fingerId)
        {
            if (IsInputLocked) return false;

            if (IsGraceActive) return true;

            // Just to be safe
            if (fingerId == AssignedFingerId)
            {
                ResetRedArcValue();
                return true;
            }

            return false;
        }

        private void ResetIntraframeState()
        {
            minDistanceThisFrame = float.MaxValue;
            wrongFingerHitThisFrame = false;
            assignedFingerMissedThisFrame = false;
            existsArcWithinRangeThisFrame = false;
            isAssigningThisFrame = false;

            if (!assignedFingerExistsThisFrame && IsFingerAssigned) ResetAssignedFinger();

            assignedFingerExistsThisFrame = false;

            var lockVal = lockUntil == int.MinValue ? 0 : (float)(lockUntil - frameTiming) / Values.ArcLockDuration;
            var graceVal = graceUntil == int.MinValue ? 0 : (float)(graceUntil - frameTiming) / Values.ArcGraceDuration;
            Services.Judgement.Debug.ShowInputLock(Color, Mathf.Clamp(lockVal, 0, 1));
            Services.Judgement.Debug.ShowGrace(Mathf.Clamp(graceVal, 0, 1));
            Services.Judgement.Debug.ShowAssignedFinger(Color, AssignedFingerId);
        }

        private void ResetAssignedFinger()
        {
            AssignedFingerId = UnassignedFingerId;
        }

        private void LockInput(float arcJudgeInterval)
        {
            var lockDuration = ArcFormula.CalculateArcLockDuration(arcJudgeInterval);
            lockUntil = frameTiming + lockDuration;
        }

        private void UnlockInput()
        {
            lockUntil = int.MinValue;
        }

        private bool IsFingerAssignedToAnotherColor(int fingerId)
        {
            for (var i = 0; i < Instances.Count; i++)
            {
                var color = Instances[i];
                if (color != this && color.AssignedFingerId == fingerId) return true;
            }

            return false;
        }

        private void FlashRedArc(float arcJudgeInterval)
        {
            if (RedArcValue > 0) return;

            RedArcValue = 1;
            redArcDuration = ArcFormula.CalculateArcLockDuration(arcJudgeInterval);
        }

        private void ConstantRedArc()
        {
            RedArcValue = 1;
        }

        private void ResetRedArcValue()
        {
            RedArcValue = 0;
        }

        private void UpdateRedArcValue(float deltaTime)
        {
            var deltaValue = deltaTime / redArcDuration;
            RedArcValue -= deltaValue;
            if (RedArcValue < 0) ResetRedArcValue();
        }
    }
}