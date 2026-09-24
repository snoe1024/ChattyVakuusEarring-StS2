using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Draws;

/// <summary>
/// ブロックカードが1枚も無い手札で、ブロック不足による大ダメージが見込まれる時にブロックカードを
/// 引いたのに、今はプレイできない(エナジー不足)時の一言。
/// </summary>
/// <remarks>
/// 被弾予測(<see cref="PlayObserver.EstimateIncomingAttackDamage"/>)を使うため、<see cref="PlayObserver.Owner"/>が
/// 常にローカルプレイヤーである(<c>ChatterHub</c>がそう保証している)前提に乗る。
/// </remarks>
public sealed class NeededBlockCardDrawnDetector : CardDrawnDetector
{
    /// <summary>これ以上受けそうならブロック不足とみなす閾値。</summary>
    private const int DamageThreshold = 10;

    protected override float Probability => 0.6f;

    protected override string Topic => "NEEDED_BLOCK_CARD_DRAWN";

    protected override bool Matches(PlayObserver observer, CardModel card)
    {
        if (!PlayObserver.CardProvidesBlock(card))
        {
            return false;
        }

        // ドロー前の手札(=今の手札から、今引いたカード自身を除いたもの)にブロックカードが無かったか。
        if (observer.Hand.Any(c => c != card && PlayObserver.CardProvidesBlock(c)))
        {
            return false;
        }

        int shortfall = observer.EstimateIncomingAttackDamage() - observer.Block;
        return shortfall >= DamageThreshold;
    }
}
