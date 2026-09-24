using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

public class HighCostHandDetector : PlayDetector
{
    private const string Topic = "HIGH_COST_HAND";
    
    private const float HighCostRatio = 2.5f;

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerTurnStarted | DetectorTrigger.VakuuTurnStarted;
    
    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (!TryStartEvaluationForTurn(observer))
        {
            return null;
        }

        if (observer.Hand.Count == 0)
        {
            return null;
        }

        if (!IsMostlyHighCost(observer.Hand, observer.Owner.PlayerCombatState?.Energy ?? 3))
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Probability = 0.9f,
            Tags = new[] { UtteranceTag.HandQuality },
        };
    }

    private static bool IsMostlyHighCost(IReadOnlyList<CardModel> hand, int currentEnergy)
    {
        int sumCost = hand.Where(card => card.CanPlay())
            .Sum(card => card.Keywords.Contains(CardKeyword.Sly) ? 0 : card.EnergyCost.GetAmountToSpend() - (card.DynamicVars.TryGetValue("Energy", out var e) ? e.IntValue : 0));
        return sumCost >= currentEnergy * HighCostRatio;
    }
}