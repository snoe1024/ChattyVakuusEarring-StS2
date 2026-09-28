using System;
using BaseLib.Config;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Config;

/// <summary>
/// このmodの設定(BaseLibの設定画面に自動生成される)。
/// </summary>
[ConfigHoverTipsByDefault]
public sealed class ChattyVakuusEarringConfig : SimpleModConfig
{
    /// <summary>
    /// 囁きのイヤリングを持っていなくても、ヴァクーの発言システム(ChatterSession)を有効にする。
    /// バニラの囁きのイヤリング自身の代打ち処理はそのままなので、これをオンにしても代打ちは起きない
    /// (代打ちに関わる発言 = FirstTurnReviewDetector/VakuuKillDetectorは発動しない)。それ以外の
    /// Detector(手札評価・ダメージ/ブロック関連の小言、戦闘開始時の一言等)を、レリックを引く前から
    /// 試したいプレイヤー向けの設定。
    /// </summary>
    public static bool AllowWhisperingWithoutEarring { get; set; }

    private const float DefaultSpeechDurationMultiplier = 1.0f;

    /// <summary>
    /// 吹き出しの表示時間の倍率(<see cref="Chatter.Speaker.SpeechDuration"/>が参照)。1.0が標準、
    /// 大きくするほど長く表示される。言語圏による自動倍率とは別軸で、ユーザーが手動でさらに調整するための値。
    /// </summary>
    /// <remarks>
    /// 当初はこの値自体を言語圏に応じて初期化しようとしたが、それには<c>[ModInitializer]</c>実行時点で
    /// <c>LocManager.Instance</c>を読む必要があり、この時点ではまだ利用不可でクラッシュする
    /// (sts2_dev_knowledge/topics/modconfig-and-localization.md)。言語圏による自動倍率は
    /// <see cref="Chatter.Speaker.SpeechDuration"/>側で、ラン開始まで遅延した別の仕組みとして独立に持つ。
    /// </remarks>
    [ConfigSlider(0.5, 3.0, 0.1, Format = "×{0:0.0}")]
    public static float SpeechDurationMultiplier { get; set; } = DefaultSpeechDurationMultiplier;
}