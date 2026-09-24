using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Flavors;

/// <inheritdoc cref="CardPlayFlavorDetector"/>
public sealed class CommonSkillCardFlavorDetector : CardPlayFlavorDetector
{
    protected override float Probability => 0.04f;

    protected override string Topic => "COMMON_SKILL_CARD_FLAVOR";

    protected override bool Matches(CardModel model) =>
        model.Type is CardType.Skill && model.Rarity is CardRarity.Common or CardRarity.Basic && model.Enchantment is null;
}

/// <inheritdoc cref="CardPlayFlavorDetector"/>
public sealed class UncommonSkillCardFlavorDetector : CardPlayFlavorDetector
{
    protected override float Probability => 0.06f;

    protected override string Topic => "UNCOMMON_SKILL_CARD_FLAVOR";

    protected override bool Matches(CardModel model) => model.Type is CardType.Skill && model.Rarity is CardRarity.Uncommon;
}

/// <inheritdoc cref="CardPlayFlavorDetector"/>
public sealed class RareSkillCardFlavorDetector : CardPlayFlavorDetector
{
    protected override float Probability => 0.08f;

    protected override string Topic => "RARE_SKILL_CARD_FLAVOR";

    protected override bool Matches(CardModel model) => model.Type is CardType.Skill && model.Rarity is CardRarity.Rare;
}
