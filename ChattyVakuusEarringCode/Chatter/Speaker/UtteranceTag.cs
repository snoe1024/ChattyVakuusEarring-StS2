namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;

/// <summary>
/// 発言の「話題の種類」。<see cref="VakuuSpeaker"/>が「同じ話題を直前に喋ったばかりか」を判定するために使う。
/// 粒度は粗くてよい(細かく分けるほど「連続で同じ話題」を弾けなくなる)。
/// 新しいDetectorで既存のどれにも当てはまらない話題が出てきたら、ここに値を足す。
/// </summary>
public enum UtteranceTag
{
    /// <summary>最初のターンにヴァクーが代打ちした結果への講評(バニラの「なかなか上手いものでしょう？」系)。</summary>
    FirstTurnReview,

    /// <summary>「{レリック}が泣いてますよ」系。所持レリックの効果を活かせていないことへの小言。</summary>
    RelicComplaint,

    /// <summary>ブロックまわりの指摘。</summary>
    Blocking,

    /// <summary>攻撃まわりの指摘。</summary>
    Attacking,

    /// <summary>エナジー配分まわりの指摘。</summary>
    Energy,

    /// <summary>手札の質についての指摘。</summary>
    HandQuality,

    /// <summary>敵を倒したことについての一言。</summary>
    Kill,

    /// <summary>プレイヤーが何もしないまま時間が経ったことについての一言(放置)。</summary>
    Idle,

    /// <summary>プレイヤーの危機(大ダメージ・瀕死・致死級の攻撃)についての一言。</summary>
    Danger,

    /// <summary>戦闘そのもの(相手・長さ)についての一言。</summary>
    Encounter,

    /// <summary>ポーションを使わなかった(使いどころを逃した)ことについての指摘。</summary>
    PotionComplaint,

    /// <summary>ミスでも何でもない、ただカードをプレイしたことへの相槌・独り言(低確率の雑談)。</summary>
    CardFlavor,

    /// <summary>ターン中に引いた、重要だが今は手札に置いておくしかないカードへの一言。</summary>
    CardDrawn,
    
    /// <summary>フレンドとの協力行為に関する指摘</summary>
    Cooperation,

    /// <summary>ショップ(商人の部屋)での出来事についての一言。</summary>
    Shop,

    /// <summary>プレイのペース(早指し等)についての指摘。</summary>
    Pace,
}
