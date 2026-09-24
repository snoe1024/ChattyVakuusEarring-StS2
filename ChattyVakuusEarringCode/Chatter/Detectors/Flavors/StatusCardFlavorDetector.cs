using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Flavors;

/// <summary>
/// 状態異常(<see cref="CardType.Status"/>)・呪い(<see cref="CardType.Curse"/>)カードのプレイに対する相槌。
/// </summary>
/// <remarks><inheritdoc cref="CardPlayFlavorDetector"/></remarks>
public sealed class StatusCardFlavorDetector : CardPlayFlavorDetector
{
    // 状態異常・呪いはそもそもプレイされる機会自体が稀なので、来た時はやや高めの確率で拾う。
    protected override float Probability => 0.12f;

    protected override string Topic => "STATUS_CARD_FLAVOR";

    protected override bool Matches(CardModel model) => model.Type is CardType.Status or CardType.Curse;
}
