using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Flavors;

/// <summary>
/// プレイミスでも確率の偏りでもない、ただカードをプレイしただけの時にたまに挟む相槌・独り言の共通実装。
/// </summary>
/// <remarks>
/// <para>
/// これまでのDetectorは全て「何かがおかしい/偏っている」時にしか喋らなかったため、何事もなく手堅く
/// プレイしているだけの時にヴァクーが終始無言になりがちだった。カードタイプ(アタック/スキル/パワー/
/// 状態異常)ごとに、それらしい相槌を低確率で挟むためのもの。判断ロジックを持たず、「このカードタイプが
/// 来たら、低確率で台詞を1つ提案する」だけなので、具象クラスは<see cref="Topic"/>と<see cref="Matches"/>を
/// 実装するだけで済む。
/// </para>
/// <para>
/// カード1種類ごとの個別ネタ(<c>DemonFormPlayDetector</c>等)とは役割が違う。あちらは特定のカードだけの
/// 専用の一言、こちらは対象のカードタイプなら何でも良い汎用の相槌。両方に該当する場合は、個別ネタの方に
/// 高い<see cref="Utterance.Priority"/>を設定しておけば、そちらが先に判断される。
/// </para>
/// </remarks>
public abstract class CardPlayFlavorDetector : PlayDetector
{
    public override DetectorTrigger Triggers => DetectorTrigger.CardPlayed;

    /// <summary>この相槌を提案する確率。カードタイプの登場頻度に応じて具象クラスごとに調整する。</summary>
    protected abstract float Probability { get; }

    /// <summary>台詞のローカライズトピック(<c>{話題}.{0,1,2...}</c>から1つ選ばれる)。</summary>
    protected abstract string Topic { get; }

    /// <summary>このカードタイプが対象かどうか。</summary>
    protected abstract bool Matches(CardModel model);

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        CardModel? card = observer.LastManualCardPlay?.Card;
        if (card == null || !Matches(card))
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
            // 「何かがおかしい」系の指摘と同じ場面で出た場合は、そちらを優先させたいので既定より低めにする。
            Priority = -5,
            Tags = new[] { UtteranceTag.CardFlavor },
        };
    }
}
