using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「爆発の小瓶が泣いてますよ」。敵が複数いて、そのうち1体はHP+ブロック合計が10以下で、
/// それを倒しておけば実際の被弾を一定以上減らせたのに、使わずにターンが終わった時の一言。
/// </summary>
/// <remarks>
/// <see cref="LethalRemovalPotionDetector{TPotion}"/>とほぼ同じ判定(倒せば実際の被弾がどれだけ減るかで見る。
/// 既に崩御で自滅が確定している敵は除外する)だが、爆発の小瓶は全体攻撃で単体ダメージポーションより
/// 弱い(10ダメージ)ため、敵が複数いる場面に限定する(敵が1体だけなら火炎ポーション等の単体ダメージ
/// ポーションの方が適切な選択なので対象外)。
/// </remarks>
public sealed class ExplosiveAmpouleComplaintDetector : PlayDetector
{
    /// <summary>この人数未満(=1体だけ)の時は対象外。</summary>
    private const int MinEnemyCount = 2;

    /// <summary>これ未満しか実際の被弾を減らせないなら、わざわざ使うほどではないとみなす。</summary>
    private const int MinDamageReduction = 5;

    public override DetectorTrigger Triggers => DetectorTrigger.TurnEnded;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.WasVakuuTurn || !observer.HasUnusedPotion<ExplosiveAmpoule>())
        {
            return null;
        }

        if (observer.LivingEnemies.Count() < MinEnemyCount)
        {
            return null;
        }

        ExplosiveAmpoule potion = ModelDb.Potion<ExplosiveAmpoule>();
        int damageThreshold = potion.DynamicVars.Damage.IntValue;

        bool worthUsing = observer.LivingEnemies.Any(enemy =>
            enemy.CurrentHp + enemy.Block <= damageThreshold
            && !observer.IsDoomedByDemise(enemy)
            && observer.EstimateDamageReductionFromRemoving(enemy) >= MinDamageReduction);
        if (!worthUsing)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick($"{potion.Id.Entry}_COMPLAINT");
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Probability = 0.6f,
            Tags = new[] { UtteranceTag.PotionComplaint, UtteranceTag.Attacking },
        };
    }
}
