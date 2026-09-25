using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter;

/// <summary>
/// ショップ(商人の部屋)で「特定の状況が起きたか」を見張るクラスの基底。<c>Chatter.Detectors.PlayDetector</c>のショップ版。
/// </summary>
/// <remarks>
/// 増やし方は戦闘用のDetectorと同じ: このクラスを継承した<c>public sealed class</c>を(引数なしコンストラクタで)
/// <c>ShopChatter/</c>配下に1つ追加すれば、<see cref="ShopDetectorRegistry"/>が自動で見つける。登録処理は不要。
/// </remarks>
public abstract class ShopDetector
{
    /// <summary>ログ用のID。既定はクラス名。</summary>
    public virtual string Id => GetType().Name;

    /// <summary>このDetectorが<see cref="Detect"/>を呼ばれたいタイミング。</summary>
    public abstract ShopDetectorTrigger Triggers { get; }

    /// <summary>
    /// 入室時に1度だけ呼ばれ、このDetectorが今回の入室で発火しうるかを判断する。
    /// falseを返すと、次の入室まで<see cref="Detect"/>は一切呼ばれない。
    /// </summary>
    public virtual bool ShouldActivate(ShopObserver observer) => true;

    /// <summary>
    /// <see cref="Triggers"/>で宣言したタイミングごとに呼ばれる。
    /// 発言したいならUtteranceを返し、何も言うことがなければnullを返す。
    /// </summary>
    public abstract Utterance? Detect(ShopObserver observer, ShopDetectorTrigger trigger);
}
