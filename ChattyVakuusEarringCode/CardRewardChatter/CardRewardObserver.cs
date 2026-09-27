using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// カード報酬画面・「カードを1枚選ぶ」画面(ポーション等)での状況を観察する窓口。
/// <c>Chatter.Observer.PlayObserver</c>のカード報酬画面版。
/// </summary>
/// <remarks>
/// 1回の画面表示につき1つ生成され、その場でDetectorを回して捨てる(ショップや戦闘と違い、複数の
/// タイミングにまたがって存在し続ける必要が無いため、放置時間等の状態は持たない)。
/// </remarks>
public sealed class CardRewardObserver
{
    /// <summary><see cref="RecentPastCardChoices"/>/<see cref="RecentPastChoicesOf"/>が遡る過去の報酬の回数。</summary>
    private const int RecentRewardLookback = 5;

    internal CardRewardObserver(Player owner, ICardChoiceScreenAdapter screen, IReadOnlyList<CardModel> cards, bool canSkip)
    {
        Owner = owner;
        Screen = screen;
        Cards = cards;
        CanSkip = canSkip;
    }

    /// <summary>囁きのイヤリングの持ち主(常にこの端末のローカルプレイヤー)。</summary>
    public Player Owner { get; }

    internal ICardChoiceScreenAdapter Screen { get; }

    /// <summary>選択肢として提示されているカード本体。</summary>
    public IReadOnlyList<CardModel> Cards { get; }

    /// <summary>この画面を(カードを選ばずに)スキップできるか。</summary>
    public bool CanSkip { get; }

    public bool HasRelic<T>() where T : RelicModel => Owner.GetRelic<T>() != null;

    /// <summary>
    /// 提示されている全カードがレア確定かどうか(ボス戦の報酬、またはホワイトスターレリック所持時の
    /// エリート戦での追加報酬に相当)。
    /// </summary>
    /// <remarks>
    /// 本来の判定材料である<c>CardCreationOptions.RarityOdds</c>(<c>CardRarityOddsType.BossEncounter</c>、
    /// 「レアのみ生成する」の意)は<c>CardReward</c>内部のprivateな状態で、画面表示のHarmonyフック
    /// (<c>NCardRewardSelectionScreen.ShowScreen</c>)からは見えない。一方、この抽選タイプの効果は
    /// 「提示される全カードがレア」という結果に必ず現れるため、実際に配られたカードのレアリティを
    /// 直接見ることで同じ判定ができる(2026-09、ユーザー要望への対応)。
    /// </remarks>
    public bool IsGuaranteedRareReward => Cards.Count > 0 && Cards.All(c => c.Rarity == CardRarity.Rare);

    /// <summary>
    /// このプレイヤーが過去(現在表示中の画面より前)にカード報酬等で選んだ・見送ったカードの履歴
    /// (報酬1回分の単位でグループ化、新しい順)。
    /// </summary>
    /// <remarks>
    /// 独自にトラッキング用の状態を持たず、本家が<c>RunState.MapPointHistory</c>
    /// (<c>IReadOnlyList&lt;IReadOnlyList&lt;MapPointHistoryEntry&gt;&gt;</c>、Actごと・訪れたマップポイント
    /// ごとの階層)の<c>PlayerMapPointHistoryEntry.CardChoices</c>に既に記録している履歴をそのまま読む。
    /// カード報酬・特別カード報酬・一部レリック(Lead Paperweight等)による強制スキップも含む、そのランで
    /// 実際に選択・見送りが確定した全カードが対象。<c>RunState</c>ごとランに紐づくため、セーブして
    /// 終了・再読み込みしても自然に残り、新しいランでは自動的に空になる。なお現在表示中の画面自身の選択は
    /// この画面が閉じて初めて記録されるため、ここには含まれない(自己参照にならない)。
    /// <para>
    /// <b>グループ化の単位について(2026-09)</b>: 「そのカードが出現した回で、他のどのカードもピック
    /// されなかったか」(=競合に負けたのではなく報酬全体が丸ごと見送られたか)を判定するには、
    /// どの<c>CardChoiceHistoryEntry</c>が同じ1回の報酬に属するかをグループ化する必要がある。
    /// <c>CardChoices</c>自体はフラットな1本のリストでグループの境目を持たないため、代わりに
    /// <c>MapPointHistoryEntry</c>(=1つのマップポイント、通常1回の戦闘に1回のカード報酬が対応する)を
    /// グループの単位として使う。ホワイトスターレリックのように1つのマップポイントで複数のカード報酬が
    /// 独立発生するケースでは不正確になりうるが、大多数の場合(1マップポイント=1報酬)では正確に一致する。
    /// </para>
    /// </remarks>
    private IEnumerable<IReadOnlyList<PastCardChoice>> PastRewardOccurrences =>
        Owner.RunState.MapPointHistory
            .SelectMany(mapPoints => mapPoints)
            .Reverse()
            .Select(mapPoint => mapPoint.PlayerStats.FirstOrDefault(stats => stats.PlayerId == Owner.NetId))
            .Where(stats => stats != null && stats.CardChoices.Count > 0)
            .Select(stats => (IReadOnlyList<PastCardChoice>)stats!.CardChoices
                .Select(choice => new PastCardChoice(choice.Card.Id, choice.Card.CurrentUpgradeLevel, choice.wasPicked))
                .ToList());

    /// <summary>過去の全てのカード選択履歴(グループ化を無視したフラットな一覧)。</summary>
    public IEnumerable<PastCardChoice> PastCardChoices => PastRewardOccurrences.SelectMany(occurrence => occurrence);

    /// <summary>直近<see cref="RecentRewardLookback"/>回分の報酬に絞ったカード選択履歴。</summary>
    /// <remarks>
    /// 「昔すぎることを掘り返されても覚えていない」ため、あまり過去に遡り過ぎないようにする
    /// (2026-09、ユーザー要望)。
    /// </remarks>
    public IEnumerable<PastCardChoice> RecentPastCardChoices =>
        PastRewardOccurrences.Take(RecentRewardLookback).SelectMany(occurrence => occurrence);

    /// <summary>指定したカードと同じ種類(アップグレード状態を問わずIdが一致)の過去の選択履歴。</summary>
    public IEnumerable<PastCardChoice> PastChoicesOf(CardModel card) =>
        PastCardChoices.Where(c => c.CardId == card.Id);

    /// <summary>指定したカードと同じ種類の、直近<see cref="RecentRewardLookback"/>回分に絞った過去の選択履歴。</summary>
    public IEnumerable<PastCardChoice> RecentPastChoicesOf(CardModel card) =>
        RecentPastCardChoices.Where(c => c.CardId == card.Id);

    /// <summary>
    /// 指定したカードが過去に報酬として提示されたことがあり、かつその回(1回の報酬全体)では結局
    /// どのカードもピックされなかった場合にtrue。
    /// </summary>
    /// <remarks>
    /// 「そのカード自身がピックされなかった」だけでは、単に他のもっと魅力的なカードに競り負けただけの
    /// 可能性がある(その場合にまで「取らなかったですね」と煽るのは不当、とユーザー指摘)。そこで、
    /// そのカードが含まれていた回の報酬全体を見て、その回で本当に何も選ばれなかった(競合にすら
    /// ならなかった)場合だけを対象にする。
    /// </remarks>
    public bool WasOfferedAndFullySkipped(CardModel card) =>
        PastRewardOccurrences.Any(occurrence =>
            occurrence.Any(c => c.CardId == card.Id) && occurrence.All(c => !c.WasPicked));
}
