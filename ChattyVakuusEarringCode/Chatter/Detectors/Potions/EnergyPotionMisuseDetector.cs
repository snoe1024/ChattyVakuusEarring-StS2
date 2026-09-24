using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「エナジーポーションが泣いてますよ」。手札が0枚の相手に使った上に、カードを引く手段(ポーション)も
/// 残っていない、エナジーを得ても何も出来ない状況での使用への一言。
/// </summary>
public sealed class EnergyPotionMisuseDetector : PotionMisuseDetector<EnergyPotion>
{
    protected override float Probability => 0.6f;

    protected override string Topic => "ENERGY_POTION_MISUSE";

    protected override bool WasWasted(PlayObserver observer, EnergyPotion potion, Creature target)
    {
        if (target.Player is not { } player || PlayObserver.HandOf(target).Any(card => card.CanPlay()))
        {
            return false;
        }

        return !PlayObserver.HasUnusedPotion<SwiftPotion>(player)
            && !PlayObserver.HasUnusedPotion<Clarity>(player)
            && !PlayObserver.HasUnusedPotion<BottledPotential>(player)
            && !PlayObserver.HasUnusedPotion<GlowwaterPotion>(player);
    }
}
