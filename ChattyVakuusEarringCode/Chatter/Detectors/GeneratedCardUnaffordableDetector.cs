using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 生成されたカードが、コスト増加効果(レリック「とげ付きガントレット」のパワー限定コスト増加、
/// パワー「借り物の時間」のコスト増加等)さえ無ければ0コストで使えたはずなのに、実際のコストが0を超え
/// エナジー不足でプレイできない状態になっている時の一言。
/// </summary>
/// <remarks>
/// <b>2026-09、判定方法を修正</b>: 当初<c>CardModel.EnergyCost.Canonical</c>(印刷値)が0かどうかで
/// 見ていたが、これは誤り。「ポーション等の生成効果でこのターンだけ0コストにする」という効果は、印刷値
/// (<c>Canonical</c>)ではなく<c>LocalCostModifier</c>(<c>CostModifiers.Local</c>)として乗るため、
/// 本来3コストのパワーが生成時に0コスト化されたケースを<c>Canonical</c>では拾えない
/// (ユーザー指摘。`sts2_dev_knowledge`的に言えば「印刷値と実効値の違い」を見誤った例)。
/// 正しくは、<c>GetWithModifiers(CostModifiers.Local)</c>(カード固有のローカル修正だけを適用した値。
/// 生成時の0コスト化はここに乗る)が0で、かつ<c>GetResolved()</c>(=<c>GetWithModifiers(CostModifiers.All)</c>、
/// レリック・パワー等のグローバルフック込みの最終値)がそれより高くなっている、という2値の比較で判定する
/// (印刷から既に0コストのカードがグローバルフックだけで押し上げられるケースも合わせて拾える)。
/// X-cost カード(<c>CostsX</c>)は挙動が異なるため対象外。
/// 原因(どのレリック・パワーによる増加か)までは特定せず、「プレイ不可の理由がエナジー不足だけ」
/// (<c>CanPlay(out UnplayableReason, out _)</c>が<c>EnergyCostTooHigh</c>単体を返す)であることだけを見る
/// (<c>CardDrawnDetector</c>の「エナジー不足*だけ*なら」という判定と同じ考え方)。
/// </remarks>
public sealed class GeneratedCardUnaffordableDetector : PlayDetector
{
    private const string Topic = "GENERATED_CARD_UNAFFORDABLE";

    public override DetectorTrigger Triggers => DetectorTrigger.CardGenerated;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        CardModel? card = observer.LastGeneratedCard;
        if (card == null || card.EnergyCost.CostsX)
        {
            return null;
        }

        // グローバルフック(レリック・パワー等)を含めない、このカード固有の実効コスト。
        // 生成効果による「このターンだけ0コスト」もここに反映される。
        if (card.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0)
        {
            return null;
        }

        if (card.CanPlay(out var reason, out _) || (reason & UnplayableReason.EnergyCostTooHigh) == 0)
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
            Probability = 1f,
            Tags = new[] { UtteranceTag.CardDrawn },
        };
    }
}
