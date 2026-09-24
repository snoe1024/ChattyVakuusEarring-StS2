using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「ひとつまみの狂気が泣いてますよ」。手札に0コストを超えるカード(コストを無料化する意味がある対象)が
/// 1枚も無く、選びようが無かった状況での使用への一言。
/// </summary>
/// <remarks>
/// 対象になりうるカードの条件は、本家の<c>TouchOfInsanity.OnUse</c>の選択フィルタと同じ
/// (<c>CostsEnergyOrStars</c>の通常時・グローバル修正込みの両方をorで見る)。
/// </remarks>
public sealed class TouchOfInsanityMisuseDetector : PotionMisuseDetector<TouchOfInsanity>
{
    protected override float Probability => 0.6f;

    protected override string Topic => "TOUCH_OF_INSANITY_MISUSE";

    protected override bool WasWasted(PlayObserver observer, TouchOfInsanity potion, Creature target) =>
        !PlayObserver.HandOf(target).Any(c =>
            c.CostsEnergyOrStars(includeGlobalModifiers: false) || c.CostsEnergyOrStars(includeGlobalModifiers: true));
}
