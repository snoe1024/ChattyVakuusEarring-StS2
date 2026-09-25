using BaseLib.Config;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Config;

/// <summary>
/// このmodの設定(BaseLibの設定画面に自動生成される)。
/// </summary>
/// <remarks>
/// <c>SimpleModConfig</c>はプロパティの型でUIを自動的に出し分ける(素の<c>bool</c>はトグル、
/// 属性なしの<c>string</c>はテキスト入力欄になる。sts2_dev_knowledge/topics/modconfig-and-localization.md参照)。
/// ラベル・ホバーテキストは`settings_ui.json`の`CHATTYVAKUUEARRING-{プロパティ名をSCREAMING_SNAKE_CASEにしたもの}.title`
/// / `.hover.desc`から取る(プレフィックスは`namespace`の最初のセグメントを大文字化したもので、
/// `CHATTY-VAKUU-EARRING`(発言のローカライズキーで使っている接頭辞)とは別物・変更不可)。
/// </remarks>
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
}
