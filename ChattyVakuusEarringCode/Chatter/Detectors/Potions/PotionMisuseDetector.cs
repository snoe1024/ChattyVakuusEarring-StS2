using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Potions;

/// <summary>
/// 「無駄なポーション消費を咎める」系の共通実装。ポーションを使わなかったこと(<c>{Potion}ComplaintDetector</c>系、
/// <see cref="DetectorTrigger.TurnEnded"/>判定)とは向きが逆で、こちらは使う必要が無い場面で使ってしまった時の一言
/// (<see cref="DetectorTrigger.PotionUsed"/>判定)。
/// </summary>
/// <remarks>
/// <para>
/// ポーションの「使用者」は常に持ち主自身(<c>ChatterHub</c>が<c>Actor == Owner.Creature</c>のセッションだけに配る)だが、
/// 「効果の対象」(<see cref="PlayObserver.LastUsedPotionTarget"/>)はマルチプレイでは仲間のCreatureになりうる。
/// 具象クラスの<see cref="WasWasted"/>には両方を渡すので、対象の手札・山札・HP・ブロックといった素の状態を見る分には
/// 対象が誰でも正確だが、被弾予測(<see cref="PlayObserver.EstimateIncomingAttackDamage"/>)はローカルプレイヤー
/// (=持ち主自身)視点でしか正確に動かないため、それを使う具象クラスは対象が持ち主自身の時だけ判定すること。
/// </para>
/// </remarks>
public abstract class PotionMisuseDetector<TPotion> : PlayDetector where TPotion : PotionModel
{
    public override DetectorTrigger Triggers => DetectorTrigger.PotionUsed;

    /// <summary>この一言を提案する確率。</summary>
    protected abstract float Probability { get; }

    /// <summary>台詞のローカライズトピック。</summary>
    protected abstract string Topic { get; }

    /// <summary>このポーションの使用が(効果の対象<paramref name="target"/>にとって)無駄だったか。</summary>
    protected abstract bool WasWasted(PlayObserver observer, TPotion potion, Creature target);

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.LastUsedPotion is not TPotion potion
            || observer.LastUsedPotionTarget is not { } target
            || !WasWasted(observer, potion, target))
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
            Probability = Probability,
            Tags = new[] { UtteranceTag.PotionComplaint },
        };
    }
}
