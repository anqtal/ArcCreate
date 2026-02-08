using System.Collections.Generic;
using ArcCreate.Gameplay.Data;
using ArcCreate.Utility;
using UnityEngine;

namespace ArcCreate.Gameplay.Judgement.Input
{
    public class TouchInputHandler : IInputHandler
    {
        protected List<TouchInput> CurrentInputs { get; } = new(10);

        public virtual void PollInput()
        {
            CurrentInputs.Clear();
            var count = UnityEngine.Input.touchCount;
            for (var i = 0; i < count; i++)
            {
                var touch = UnityEngine.Input.GetTouch(i);

                var input = new TouchInput(touch, GetCameraRay(touch.position));
                CurrentInputs.Add(input);

                Services.InputFeedback.LaneFeedback(input.Lane);
                Services.InputFeedback.FloatlineFeedback(input.VerticalPos.y);
            }

            Services.Judgement.Debug.SetTouchState(CurrentInputs);
        }

        public void HandleTapRequests(
            int currentTiming,
            UnorderedList<LaneTapJudgementRequest> laneTapRequests,
            UnorderedList<ArcTapJudgementRequest> arcTapRequests)
        {
            foreach (var input in CurrentInputs)
            {
                if (!input.IsTap) continue;
                // if (!(input.IsTap || input.Phase == TouchPhase.Moved))
                // {
                //     continue;
                // }

                var minTimingDifference = int.MaxValue;
                var minPositionDifference = float.MaxValue;

                var applicableLaneRequestExists = false;
                LaneTapJudgementRequest applicableLaneRequest = default;
                var applicableLaneRequestIndex = 0;

                for (var i = laneTapRequests.Count - 1; i >= 0; i--)
                {
                    var req = laneTapRequests[i];
                    var timingDifference = req.AutoAtTiming - currentTiming;
                    if (timingDifference > minTimingDifference) continue;

                    var judgementSize = req.Properties.CurrentJudgementSize;
                    var judgementOffset = req.Properties.CurrentJudgementOffset;

                    var worldPosition = new Vector3(ArcFormula.LaneToWorldX(req.Lane), 0, 0) + judgementOffset;
                    var screenPosition = Services.Camera.GameplayCamera.WorldToScreenPoint(worldPosition);
                    var deltaToNote = screenPosition - input.ScreenPos;
                    var distanceToNote = deltaToNote.sqrMagnitude;
                    if (LaneCollide(input, screenPosition, req.Lane, judgementSize, judgementOffset == Vector3.zero)
                        && (timingDifference < minTimingDifference || distanceToNote <= minPositionDifference))
                    {
                        minTimingDifference = timingDifference;
                        minPositionDifference = distanceToNote;
                        applicableLaneRequestExists = true;
                        applicableLaneRequest = req;
                        applicableLaneRequestIndex = i;
                    }
                }

                var applicableArcTapRequestExists = false;
                ArcTapJudgementRequest applicableArcTapRequest = default;
                var applicableArcTapRequestIndex = 0;

                for (var i = arcTapRequests.Count - 1; i >= 0; i--)
                {
                    var req = arcTapRequests[i];
                    var timingDifference = req.AutoAtTiming - currentTiming;
                    if (timingDifference > minTimingDifference) continue;

                    var judgementSize = req.Properties.CurrentJudgementSize;
                    var judgementOffset = req.Properties.CurrentJudgementOffset;
                    var worldPosition = new Vector3(req.X, req.Y, 0) + judgementOffset;
                    var screenPosition = Services.Camera.GameplayCamera.WorldToScreenPoint(worldPosition);
                    var deltaToNote = screenPosition - input.ScreenPos;
                    var distanceToNote = deltaToNote.sqrMagnitude;

                    if (ArcTapCollide(input, screenPosition, worldPosition, req.Width, judgementSize)
                        && (timingDifference < minTimingDifference || distanceToNote <= minPositionDifference))
                    {
                        minTimingDifference = timingDifference;
                        minPositionDifference = distanceToNote;
                        applicableArcTapRequestExists = true;
                        applicableArcTapRequest = req;
                        applicableArcTapRequestIndex = i;
                    }
                }

                if (applicableArcTapRequestExists)
                {
                    var offset = currentTiming - applicableLaneRequest.AutoAtTiming;
                    if (input.Phase == TouchPhase.Moved)
                        //if (offset is >= -90 and <= 90) continue;
                        applicableArcTapRequest.Receiver.ProcessArcTapJudgement(
                            0, applicableArcTapRequest.Properties);
                    else
                        applicableArcTapRequest.Receiver.ProcessArcTapJudgement(
                            currentTiming - applicableArcTapRequest.AutoAtTiming, applicableArcTapRequest.Properties);

                    arcTapRequests.RemoveAt(applicableArcTapRequestIndex);
                }
                else if (applicableLaneRequestExists)
                {
                    var offset = currentTiming - applicableLaneRequest.AutoAtTiming;
                    // if (input.Phase == TouchPhase.Moved)
                    // {
                    //     //if (offset is >= -90 and <= 90) continue;
                    //     applicableLaneRequest.Receiver.ProcessLaneTapJudgement(
                    //         0, applicableLaneRequest.Properties);
                    // }
                    // else
                    {
                        applicableLaneRequest.Receiver.ProcessLaneTapJudgement(
                            offset, applicableLaneRequest.Properties);
                    }
                    Debug.Log(offset);
                    laneTapRequests.RemoveAt(applicableLaneRequestIndex);
                }
            }
        }

        public void HandleLaneHoldRequests(int currentTiming, UnorderedList<LaneHoldJudgementRequest> requests)
        {
            foreach (var input in CurrentInputs)
                for (var i = requests.Count - 1; i >= 0; i--)
                {
                    var req = requests[i];

                    if (currentTiming < req.StartAtTiming || req.Receiver.IsLocked) continue;

                    var judgementSize = req.Properties.CurrentJudgementSize;
                    var judgementOffset = req.Properties.CurrentJudgementOffset;
                    var worldPosition = new Vector3(ArcFormula.LaneToWorldX(req.Lane), 0, 0) + judgementOffset;
                    var screenPosition = Services.Camera.GameplayCamera.WorldToScreenPoint(worldPosition);

                    if (LaneCollide(input, screenPosition, req.Lane, judgementSize, judgementOffset == Vector3.zero))
                    {
                        req.Receiver.ProcessLaneHoldJudgement(currentTiming >= req.ExpireAtTiming, req.IsJudgement,
                            req.Properties);
                        requests.RemoveAt(i);
                    }
                }
        }

        public void HandleArcRequests(int currentTiming, UnorderedList<ArcJudgementRequest> requests)
        {
            ArcColorLogic.NewFrame(currentTiming);

            // Notify if arcs & fingers exists
            for (var c = 0; c <= ArcColorLogic.MaxColor; c++)
            {
                var color = ArcColorLogic.Get(c);

                var arcOfColorExists = false;
                for (var i = requests.Count - 1; i >= 0; i--)
                {
                    var req = requests[i];
                    if (currentTiming >= req.Arc.Timing
                        && currentTiming <= req.Arc.EndTiming
                        && req.Arc.Color == color.Color)
                    {
                        arcOfColorExists = true;
                        break;
                    }
                }

                color.ExistsArcWithinRange(arcOfColorExists);
                for (var inpIndex = 0; inpIndex < CurrentInputs.Count; inpIndex++)
                {
                    var input = CurrentInputs[inpIndex];
                    color.FingerExists(input.Id);
                }
            }

            // Process finger lifting
            for (var inpIndex = 0; inpIndex < CurrentInputs.Count; inpIndex++)
            {
                var input = CurrentInputs[inpIndex];
                if (input.Phase == TouchPhase.Ended || input.Phase == TouchPhase.Canceled)
                    for (var c = 0; c <= ArcColorLogic.MaxColor; c++)
                    {
                        var colorLogic = ArcColorLogic.Get(c);
                        var set = false;

                        for (var i = requests.Count - 1; i >= 0; i--)
                        {
                            var req = requests[i];
                            if (currentTiming >= req.StartAtTiming
                                && currentTiming <= req.Arc.EndTiming)
                            {
                                colorLogic.FingerLifted(input.Id, (float)req.Arc.TimeIncrement);
                                set = true;
                            }
                        }

                        if (!set) colorLogic.FingerLifted(input.Id, 0);
                    }
            }

            // Detect grace period
            var graceActive = false;
            for (var i = requests.Count - 1; i >= 0; i--)
            {
                var req1 = requests[i];
                if (currentTiming > req1.Arc.EndTiming || currentTiming < req1.StartAtTiming) continue;

                for (var j = i - 1; j >= 0; j--)
                {
                    var req2 = requests[j];
                    if (req2.Arc.Color == req1.Arc.Color || currentTiming > req2.Arc.EndTiming ||
                        currentTiming < req2.StartAtTiming)
                        continue;

                    var judgementSize = req2.Properties.CurrentJudgementSize;
                    var judgementOffset = req2.Properties.CurrentJudgementOffset;
                    var worldPosition1 =
                        new Vector3(req1.Arc.WorldXAt(currentTiming), req1.Arc.WorldYAt(currentTiming), 0) +
                        judgementOffset;
                    var worldPosition2 =
                        new Vector3(req2.Arc.WorldXAt(currentTiming), req2.Arc.WorldYAt(currentTiming), 0) +
                        judgementOffset;
                    var screenPosition1 = Services.Camera.GameplayCamera.WorldToScreenPoint(worldPosition1);
                    var screenPosition2 = Services.Camera.GameplayCamera.WorldToScreenPoint(worldPosition2);

                    if (ArcHitboxCollide(screenPosition1, screenPosition2, worldPosition1, worldPosition2,
                            judgementSize))
                    {
                        graceActive = true;
                        break;
                    }
                }

                if (graceActive)
                {
                    ArcColorLogic.StartGracePeriodForAllColors();
                    break;
                }
            }

            // Process finger hitting
            for (var inpIndex = 0; inpIndex < CurrentInputs.Count; inpIndex++)
            {
                var input = CurrentInputs[inpIndex];

                if (input.Phase == TouchPhase.Ended || input.Phase == TouchPhase.Canceled) continue;

                for (var i = requests.Count - 1; i >= 0; i--)
                {
                    var req = requests[i];
                    var colorLogic = ArcColorLogic.Get(req.Arc.Color);

                    if (currentTiming < req.StartAtTiming || currentTiming > req.Arc.EndTiming) continue;

                    var judgementSize = req.Properties.CurrentJudgementSize;
                    Vector2 judgementOffset = req.Properties.CurrentJudgementOffset;
                    var collide = ArcCollide(input, req.Arc, currentTiming, judgementSize, judgementOffset);
                    if (collide)
                    {
                        var worldPosition = new Vector3(req.Arc.WorldXAt(currentTiming),
                            req.Arc.WorldYAt(currentTiming), 0);
                        var screenPosition = Services.Camera.GameplayCamera.WorldToScreenPoint(worldPosition);
                        var distance = (screenPosition - input.ScreenPos).sqrMagnitude;
                        colorLogic.FingerHit(input.Id, distance, (float)req.Arc.TimeIncrement);
                    }
                    else
                    {
                        colorLogic.FingerMiss(input.Id, (float)req.Arc.TimeIncrement);
                    }
                }
            }

            // Reply to requests
            for (var inpIndex = 0; inpIndex < CurrentInputs.Count; inpIndex++)
            {
                var input = CurrentInputs[inpIndex];

                for (var i = requests.Count - 1; i >= 0; i--)
                {
                    var req = requests[i];
                    if (currentTiming < req.StartAtTiming) continue;

                    var colorLogic = ArcColorLogic.Get(req.Arc.Color);

                    var judgementSize = req.Properties.CurrentJudgementSize;
                    Vector2 judgementOffset = req.Properties.CurrentJudgementOffset;
                    var collide = ArcCollide(input, req.Arc, currentTiming, judgementSize, judgementOffset);
                    var acceptInput = colorLogic.ShouldAcceptInput(input.Id);

                    if (collide && acceptInput)
                    {
                        req.Receiver.ProcessArcJudgement(currentTiming >= req.ExpireAtTiming, req.IsJudgement,
                            req.Properties);
                        requests.RemoveAt(i);
                    }
                }
            }

            ArcColorLogic.ApplyRedValue();
        }

        public void ResetJudge()
        {
            ArcColorLogic.ResetAll();
        }

        protected Ray GetCameraRay(Vector2 screenPosition)
        {
            return Services.Camera.GameplayCamera.ScreenPointToRay(screenPosition);
        }

        private bool ArcCollide(TouchInput touch, Arc arc, int currentTiming, Vector2 judgementSize,
            Vector3 judgementOffset)
        {
            var arcWorldPosition =
                new Vector3(arc.WorldXAt(currentTiming), arc.WorldYAt(currentTiming)) + judgementOffset;
            var skyInputY = Services.Judgement.SkyInputY;
            if (arcWorldPosition.y <= skyInputY) touch.VerticalPos.y = Mathf.Min(touch.VerticalPos.y, skyInputY);

            var arcScreenPos = Services.Camera.GameplayCamera.WorldToScreenPoint(arcWorldPosition);
            var touchScreenPos = Services.Camera.GameplayCamera.WorldToScreenPoint(touch.VerticalPos);
            return ArcHitboxCollide(arcScreenPos, touchScreenPos, touch.VerticalPos, arcWorldPosition, judgementSize);
        }

        private bool ArcHitboxCollide(Vector3 screenPosition1, Vector3 screenPosition2, Vector3 worldPosition1,
            Vector3 worldPosition2, Vector2 judgementSize)
        {
            var dx = Mathf.Abs(screenPosition1.x - screenPosition2.x);
            var dy = Mathf.Abs(screenPosition1.y - screenPosition2.y);
            var screenCollide = dx <= Values.LaneScreenHitboxHorizontal * 2 * Values.ArcHitboxX / Values.LaneWidth *
                                judgementSize.x
                                && dy <= Values.LaneScreenHitboxVertical * 2 * Values.ArcHitboxY / Values.LaneWidth *
                                judgementSize.y;

            var dWx = Mathf.Abs(worldPosition1.x - worldPosition2.x);
            var dWy = Mathf.Abs(worldPosition1.y - worldPosition2.y);
            var worldCollide = dWx <= Values.ArcHitboxX * judgementSize.x &&
                               dWy <= Values.ArcHitboxY * judgementSize.y;
            return worldCollide || screenCollide;
        }

        private bool ArcTapCollide(TouchInput input, Vector3 screenPosition, Vector3 worldPosition, float width,
            Vector2 judgementSize)
        {
            var skyInputY = Services.Judgement.SkyInputY;
            if (worldPosition.y <= skyInputY) input.VerticalPos.y = Mathf.Min(input.VerticalPos.y, skyInputY);

            var hitboxX = Values.ArcTapHitboxX + Values.LaneWidth / 2 * (width - 1);
            var dSx = Mathf.Abs(input.ScreenPos.x - screenPosition.x);
            var dSy = input.ScreenPos.y - screenPosition.y;
            var screenCollide =
                dSx <= Values.LaneScreenHitboxHorizontal * 2 * hitboxX / Values.LaneWidth * judgementSize.x
                && dSy >= -Values.LaneScreenHitboxVertical * 2 * Values.ArcTapHitboxYDown / Values.LaneWidth *
                judgementSize.y
                && dSy <= Values.LaneScreenHitboxVertical * 2 * Values.ArcTapHitboxYUp / Values.LaneWidth *
                judgementSize.y;

            var dWx = Mathf.Abs(input.VerticalPos.x - worldPosition.x);
            var dWy = input.VerticalPos.y - worldPosition.y;
            var worldCollide = dWx <= hitboxX * judgementSize.x
                               && dWy >= -Values.ArcTapHitboxYDown * judgementSize.y
                               && dWy <= Values.ArcTapHitboxYUp * judgementSize.y;
            return worldCollide || screenCollide;
        }

        private bool LaneCollide(TouchInput input, Vector3 screenPosition, int lane, Vector2 judgementSize,
            bool useLane)
        {
            var worldCollide = input.Lane == lane && useLane;
            var screenCollide = Mathf.Abs(input.ScreenPos.x - screenPosition.x) <=
                                Values.LaneScreenHitboxHorizontal * judgementSize.x
                                && Mathf.Abs(input.ScreenPos.y - screenPosition.y) <=
                                Values.LaneScreenHitboxVertical * judgementSize.y;
            return worldCollide || screenCollide;
        }
    }
}