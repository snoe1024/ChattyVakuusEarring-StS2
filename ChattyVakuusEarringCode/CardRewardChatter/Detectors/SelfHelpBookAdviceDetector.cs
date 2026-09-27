using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter.Detectors;

/// <summary>
/// イベント「Self Help Book」でデッキの1枚に付与される専用エンチャント(鋭さ→アタック限定/身軽→スキル限定/
/// 迅速→パワー限定、いずれも+2)を手がかりに、そのイベントで選んだ内容(どのページを読んだか)を踏まえた
/// 助言を、経験後最初のカード報酬で一度だけ行う。
/// </summary>
/// <remarks>
/// このイベントの選択肢自体(裏表紙だけ読む/一節読む/一冊読破/読まない)は、選んだ内容を汎用の
/// イベント履歴に直接残さないため、後から「どれを選んだか」を判別する手段が無い。一方、選択肢ごとに
/// 異なる専用のエンチャントをデッキの1枚に付与する副作用があるので、イベント自体を直接参照せず、
/// その結果(付与されたエンチャントの種類)だけから選んだ内容を逆算する(2026-09、ユーザー提案)。
/// これらのエンチャントは他のレリック(WingCharm等)や別のイベントからも付与されうるため厳密な確定ではないが、
/// 実用上十分な近似として扱う。
/// </remarks>
public sealed class SelfHelpBookAdviceDetector : CardRewardDetector
{
    private const string Topic = "CARD_REWARD_SELF_HELP_BOOK";

    private const int Priority = 10;

    /// <summary>
    /// 一度助言したら、そのラン中は二度と言わない(何度もカード報酬を開くたびに同じ蒸し返しをしないため)。
    /// Detectorのインスタンス自体は画面表示のたびに作り直される(<c>CardRewardDetectorRegistry</c>)ので、
    /// フラグはstaticに持ち、<c>RunManager.RunStarted</c>(新しいランを開始・再開するたびに発火)で
    /// リセットする。
    /// </summary>
    private static bool _adviceGiven;

    static SelfHelpBookAdviceDetector()
    {
        RunManager.Instance.RunStarted += _ => _adviceGiven = false;
    }

    public override CardRewardDetectorTrigger Triggers => CardRewardDetectorTrigger.Shown;

    public override CardRewardProposal? Detect(CardRewardObserver observer, CardRewardDetectorTrigger trigger)
    {
        if (_adviceGiven)
        {
            return null;
        }

        string? kind = PileType.Deck.GetPile(observer.Owner).Cards
            .Select(c => EnchantmentKind(c.Enchantment))
            .FirstOrDefault(k => k != null);
        if (kind == null)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic, kind);
        if (line == null)
        {
            return null;
        }

        _adviceGiven = true;

        var utterance = new Utterance
        {
            Line = line,
            Priority = Priority,
            Tags = new[] { UtteranceTag.CardReward },
        };

        return new CardRewardProposal(utterance, null);
    }

    private static string? EnchantmentKind(EnchantmentModel? enchantment) => enchantment switch
    {
        Sharp => "sharp",
        Nimble => "nimble",
        Swift => "swift",
        _ => null,
    };
}
