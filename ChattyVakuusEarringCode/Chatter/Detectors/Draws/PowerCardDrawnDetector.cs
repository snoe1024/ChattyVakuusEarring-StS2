using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Draws;

/// <summary>
/// 「せっかくパワーを引いたのに今は撃てない」への一言。
/// </summary>
public sealed class PowerCardDrawnDetector : CardDrawnDetector
{
    protected override float Probability => 0.35f;

    protected override string Topic => "POWER_CARD_DRAWN";

    protected override bool Matches(PlayObserver observer, CardModel card) => card.Type == CardType.Power;
}
