namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// カード報酬画面で「特定の状況が起きたか」を見張るクラスの基底。<c>Chatter.Detectors.PlayDetector</c>の
/// カード報酬画面版。
/// </summary>
/// <remarks>
/// 増やし方は戦闘用のDetectorと同じ: このクラスを継承した<c>public sealed class</c>を(引数なしコンストラクタで)
/// <c>CardRewardChatter/Detectors/</c>配下に1つ追加すれば、<see cref="CardRewardDetectorRegistry"/>が
/// 自動で見つける。登録処理は不要。
/// </remarks>
public abstract class CardRewardDetector
{
    /// <summary>ログ用のID。既定はクラス名。</summary>
    public virtual string Id => GetType().Name;

    /// <summary>このDetectorが<see cref="Detect"/>を呼ばれたいタイミング。</summary>
    public abstract CardRewardDetectorTrigger Triggers { get; }

    /// <summary>
    /// <see cref="Triggers"/>で宣言したタイミングごとに呼ばれる。
    /// 発言したいなら<see cref="CardRewardProposal"/>を返し、何も言うことがなければnullを返す。
    /// </summary>
    public abstract CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger);
}
