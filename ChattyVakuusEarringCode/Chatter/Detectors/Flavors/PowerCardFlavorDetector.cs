using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Flavors;

/// <inheritdoc cref="CardPlayFlavorDetector"/>
public sealed class UncommonPowerCardFlavorDetector : CardPlayFlavorDetector
{
    // パワーはアタック・スキルに比べて1戦闘中にプレイされる回数が少ないので、見劣りしないよう確率を上げてある。
    protected override float Probability => 0.06f;

    protected override string Topic => "UNCOMMON_POWER_CARD_FLAVOR";

    protected override bool Matches(CardModel model) => model.Type is CardType.Power && model.Rarity is CardRarity.Uncommon;
}

/// <inheritdoc cref="CardPlayFlavorDetector"/>
public sealed class RarePowerCardFlavorDetector : CardPlayFlavorDetector
{
    // パワーはアタック・スキルに比べて1戦闘中にプレイされる回数が少ないので、見劣りしないよう確率を上げてある。
    protected override float Probability => 0.08f;

    protected override string Topic => "RARE_POWER_CARD_FLAVOR";

    protected override bool Matches(CardModel model) => model.Type is CardType.Power && model.Rarity is CardRarity.Rare;
}
