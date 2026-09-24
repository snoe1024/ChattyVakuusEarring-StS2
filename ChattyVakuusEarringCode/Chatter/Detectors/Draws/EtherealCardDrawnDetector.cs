using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Draws;

/// <summary>
/// 「非スターターのエセリアルカードを引いたのに今はプレイできない」への一言。
/// エセリアルは未使用のままターンが終わると廃棄されるので、パワーよりも一段と痛い機会損失になる。
/// </summary>
/// <remarks>
/// <para>
/// 状態異常・呪いのエセリアルカード(積極的に引いて早めに廃棄で消しておきたいもの)は対象外。
/// あちらは「使いたいのに使えない」ではなく「むしろ使わず廃棄されるのが望ましい」カードなので、
/// このDetectorの「機会損失」という前提と噛み合わない。
/// </para>
/// <para>
/// <c>Keywords</c>(<c>KeywordSources.All</c>)ではなく<see cref="CardModel.CanonicalKeywords"/>で見ている。
/// エセリアルの状態異常/呪いを後から増やすレリック・パワーが存在し、それらは(a)戦闘中に個別のカードへ
/// 直接<c>AddKeyword</c>する(<c>KeywordSources.Local</c>)か、(b)そのモデルが存在する間だけ全カードに
/// その場で付与する(<c>KeywordSources.Global</c>、カードには一切保存されない)かのいずれかで実現される。
/// <c>CanonicalKeywords</c>はカードクラス自体が生まれつき持つ固定の keyword セット(インスタンスの状態や
/// 戦闘中の一時効果に左右されない)なので、どちらの「後付け」も自然に除外できる。
/// 加えて、今の戦闘中に生成されたカード(<see cref="PlayObserver.WasCardGeneratedThisCombat"/>)も対象外にする
/// (デッキに元から入っていたカードではなく、機会損失を語るには前提が合わないため)。
/// </para>
/// </remarks>
public sealed class EtherealCardDrawnDetector : CardDrawnDetector
{
    protected override float Probability => 0.5f;

    protected override string Topic => "ETHEREAL_CARD_DRAWN";

    protected override bool Matches(PlayObserver observer, CardModel card) =>
        card.Rarity != CardRarity.Basic
        && card.Type is not (CardType.Status or CardType.Curse)
        && card.CanonicalKeywords.Contains(CardKeyword.Ethereal)
        && !PlayObserver.WasCardGeneratedThisCombat(card);
}
