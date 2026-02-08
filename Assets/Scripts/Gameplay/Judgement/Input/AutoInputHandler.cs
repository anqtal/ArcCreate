using ArcCreate.Utility;

namespace ArcCreate.Gameplay.Judgement.Input
{
    public class AutoInputHandler : IInputHandler
    {
        public void PollInput()
        {
        }

        public void HandleTapRequests(
            int currentTiming,
            UnorderedList<LaneTapJudgementRequest> laneTapRequests,
            UnorderedList<ArcTapJudgementRequest> arcTapRequests)
        {
            for (var i = laneTapRequests.Count - 1; i >= 0; i--)
            {
                var req = laneTapRequests[i];
                if (currentTiming >= req.AutoAtTiming)
                {
                    req.Receiver.ProcessLaneTapJudgement(0, req.Properties);
                    laneTapRequests.RemoveAt(i);
                }
            }

            for (var i = arcTapRequests.Count - 1; i >= 0; i--)
            {
                var req = arcTapRequests[i];
                if (currentTiming >= req.AutoAtTiming)
                {
                    req.Receiver.ProcessArcTapJudgement(0, req.Properties);
                    arcTapRequests.RemoveAt(i);
                }
            }
        }

        public void HandleLaneHoldRequests(int currentTiming, UnorderedList<LaneHoldJudgementRequest> requests)
        {
            for (var i = requests.Count - 1; i >= 0; i--)
            {
                var req = requests[i];
                if (currentTiming >= req.AutoAtTiming)
                {
                    req.Receiver.ProcessLaneHoldJudgement(false, req.IsJudgement, req.Properties);
                    requests.RemoveAt(i);
                }
            }
        }

        public void HandleArcRequests(int currentTiming, UnorderedList<ArcJudgementRequest> requests)
        {
            for (var i = requests.Count - 1; i >= 0; i--)
            {
                var req = requests[i];
                if (currentTiming >= req.AutoAtTiming)
                {
                    req.Receiver.ProcessArcJudgement(false, req.IsJudgement, req.Properties);
                    requests.RemoveAt(i);
                }
            }
        }

        public void ResetJudge()
        {
        }
    }
}