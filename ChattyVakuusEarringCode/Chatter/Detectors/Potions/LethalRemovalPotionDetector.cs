using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「(単体ダメージポーション)が泣いてますよ」系の共通実装。HP+ブロック合計がポーションのダメージ以下の敵を
/// 倒しておけば、実際に受けるダメージを一定以上減らせたのに、使わずにターンが終わった時の一言。
/// </summary>
/// <remarks>
/// <para>
/// 火炎ポーション(<c>FirePotion</c>, ダメージ20)とポーションっぽい石(<c>PotionShapedRock</c>, ダメージ15)が対象。
/// ダメージ量は各ポーション自身の<c>DynamicVars.Damage</c>から読むので、この基底クラス自体はどちらの数値にも
/// 依存しない。台詞のトピックはレリック系Detectorと同じ規則(<c>{Id.Entry}_COMPLAINT</c>)で、
/// ポーションの<c>ModelId</c>から自動的に決まる(サブクラスは型引数を与えるだけで済む)。
/// </para>
/// <para>
/// 「その敵さえいなければ被弾ゼロ」という全消し条件ではなく、「倒せば実際の被弾が<see cref="MinDamageReduction"/>
/// 以上減る」かどうかで判定する(<see cref="PlayObserver.EstimateDamageReductionFromRemoving"/>、
/// 既に持ち主のブロックが十分な状況では自動的に軽減幅が小さくなる)。ある程度ブロックできているなら
/// リソースを温存して後のターンで倒す判断も十分正当なので、「軽減できるならとにかく使え」とは言わない。
/// また、既に崩御(<c>DemisePower</c>)の蓄積だけで自滅が確定している敵(<see cref="PlayObserver.IsDoomedByDemise"/>)は、
/// 既にそちらで仕留める算段がついているとみなして対象から外す。
/// </para>
/// </remarks>
public abstract class LethalRemovalPotionDetector<TPotion> : PlayDetector where TPotion : PotionModel
{
    /// <summary>これ未満しか実際の被弾を減らせないなら、わざわざ使うほどではないとみなす。</summary>
    private const int MinDamageReduction = 5;

    public override DetectorTrigger Triggers => DetectorTrigger.TurnEnded;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.WasVakuuTurn || !observer.HasUnusedPotion<TPotion>())
        {
            return null;
        }

        TPotion potion = ModelDb.Potion<TPotion>();
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
