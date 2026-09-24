using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「特定のパターンのプレイが起きたか」を見張り、起きたらヴァクーの台詞(<see cref="Utterance"/>)を提案するクラスの基底。
/// </summary>
/// <remarks>
/// <para>
/// 新しい台詞を増やす手順: このクラスを継承した<c>public sealed class</c>を(引数なしコンストラクタで)
/// <c>Chatter/Detectors/</c>に1つ追加し、<c>ChattyVakuusEarring/localization/*/relics.json</c>に台詞を書く。
/// アセンブリ内の継承クラスは<see cref="DetectorRegistry"/>が自動で見つけるので、登録処理は不要。
/// </para>
/// <para>
/// インスタンスは戦闘ごとに(持ち主ごとに)新規作成される。「この戦闘で既に指摘した」等の状態はフィールドに持ってよい。
/// Detectorは純粋な判定役で、ゲーム状態を変更したり自分で吹き出しを出したりしてはいけない
/// (発言するかどうかの最終判断は<see cref="VakuuSpeaker"/>が行う)。
/// </para>
/// <para>
/// 例外を投げた場合は<c>ChatterSession</c>が握りつぶしてログに出し、そのDetectorをこの戦闘の間だけ無効化する。
/// </para>
/// </remarks>
public abstract class PlayDetector
{
    /// <summary>ログ用のID。既定はクラス名。</summary>
    public virtual string Id => GetType().Name;

    /// <summary>このDetectorが<see cref="Detect"/>を呼ばれたいタイミング。</summary>
    public abstract DetectorTrigger Triggers { get; }

    /// <summary>
    /// 戦闘開始時に1度だけ呼ばれ、このDetectorが今回の戦闘で発火しうるかを判断する。
    /// falseを返すと、次の戦闘まで<see cref="Detect"/>は一切呼ばれない。
    /// 所持レリック・キャラクター・カードプール(デッキ)を見て、明らかに発火しえない場合にfalseにする。
    /// </summary>
    public virtual bool ShouldActivate(PlayObserver observer) => true;

    /// <summary>
    /// <see cref="Triggers"/>で宣言したタイミングごとに呼ばれる。
    /// 発言したいならUtteranceを返し、何も言うことがなければnullを返す。
    /// </summary>
    public abstract Utterance? Detect(PlayObserver observer, DetectorTrigger trigger);

    private int _lastEvaluatedTurn;

    /// <summary>
    /// 「1ターンに1回だけ評価したい」Detector向けの補助。最初のターンに<c>VakuuTurnStarted</c>と
    /// <c>PlayerTurnStarted</c>の両方を購読している時のように、同じターンに複数のトリガーが来ても、
    /// 最初の1回だけtrueを返す(2回目以降はfalse)。
    /// </summary>
    protected bool TryStartEvaluationForTurn(PlayObserver observer)
    {
        if (observer.TurnNumber == _lastEvaluatedTurn)
        {
            return false;
        }

        _lastEvaluatedTurn = observer.TurnNumber;
        return true;
    }
}
