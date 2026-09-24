using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;

/// <summary>
/// DetectorがSpeakerへ「これを喋りたい」と伝えるためのメッセージ。
/// </summary>
/// <remarks>
/// 台詞のバリエーションからどれを選ぶかの抽選は、Detector側(<see cref="SpeechTable.Pick"/>)で済ませてから
/// このクラスを作る。Speakerは「喋るか/喋らないか」だけを決め、台詞そのものには関与しない。
/// </remarks>
public sealed class Utterance
{
    /// <summary>
    /// 選択済みの台詞(ローカライゼーションキーが1つに確定した<see cref="LocString"/>)。
    /// 文字列ではなく<see cref="LocString"/>なのは、実際のテキスト化(SmartFormat展開)を
    /// 吹き出しを出す<c>TalkCmd.Play</c>に任せるため。[red]や[shake]等の装飾はローカライゼーション側の仕事で、
    /// コード側では一切付け足さない。
    /// </summary>
    /// <remarks>
    /// 台詞に状況依存の語(倒せたはずの敵の名前など)を入れたい時は、Detectorが<see cref="SpeechTable.Pick"/>で
    /// 得た<see cref="LocString"/>に、<see cref="Utterance"/>を作る前に<c>line.Add("Enemy", monster.Title)</c>のように
    /// 変数を積み、ローカライズ側に<c>{Enemy}</c>と書く(本家も<c>PowerModel</c>等で同じ方法を使っている)。
    /// 変数の中身は<see cref="LocString"/>が持ち運ぶので、Speakerは台詞の中身を一切知らなくてよい。
    /// </remarks>
    public required LocString Line { get; init; }

    /// <summary>
    /// trueの時、間隔・タグ・確率などSpeakerの判断を全て無視して必ず喋る。
    /// (バニラ由来の「最初のターン終了時の一言」のように、必ず喋る前提のものにだけ使う)
    /// </summary>
    public bool Force { get; init; }

    /// <summary>発言確率(0〜1)。<see cref="Force"/>がtrueの時は無視される。</summary>
    public float Probability { get; init; } = 1f;

    /// <summary>
    /// この発言の話題。直前の発言と同じタグを持つ発言は、時間が経っていない限りSpeakerが弾く。
    /// </summary>
    public IReadOnlyList<UtteranceTag> Tags { get; init; } = Array.Empty<UtteranceTag>();

    /// <summary>
    /// 同じ場面で複数のDetectorが提案した時に、Speakerが判断する順番(既定0、大きいほど先)。
    /// 先に喋ったら、後続は<c>VakuuSpeaker</c>の間隔ルールで自然に黙る。先の提案が確率判定などで黙った場合は、
    /// 後続にそのまま順番が回る(=「こちらが喋るなら、あちらは言わせたくない」を、別のDetectorを書き換えずに宣言できる)。
    /// 同じ優先度同士の順番は毎回ランダム。<see cref="Force"/>の発言は優先度に関係なく常に先に判断される。
    /// </summary>
    public int Priority { get; init; }

    /// <summary>発言元のDetector ID(ログ用)。<c>ChatterSession</c>が埋めるのでDetector側では触らない。</summary>
    public string DetectorId { get; internal set; } = "";
}
