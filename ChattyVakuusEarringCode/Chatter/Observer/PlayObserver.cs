using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Saves;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;

/// <summary>
/// プレイヤー(囁きのイヤリングの持ち主)のプレイを観察する窓口。Detectorはここを読んで条件を判定する。
/// </summary>
/// <remarks>
/// 状態を溜め込まず、なるべくゲーム本体の現在値・<c>CombatHistory</c>をその都度引く薄い読み取り専用の層にしている
/// (溜め込むと戦闘途中の生成・巻き戻しなどで簡単に食い違うため。
/// sts2_dev_knowledge/topics/model-lifecycle-and-saves.mdの「CombatHistoryを使った状態追跡パターン」参照)。
/// 判定に必要な情報が足りなくなったら、Detectorに直接ゲーム本体を触らせず、まずここにプロパティ/メソッドを足す。
/// 1つのObserverは1人の持ち主・1回の戦闘に対応し、<see cref="ChatterSession"/>が生成する。
/// </remarks>
public sealed class PlayObserver
{
    private readonly ICombatState _combatState;

    public PlayObserver(Player owner, ICombatState combatState)
    {
        Owner = owner;
        _combatState = combatState;
    }

    /// <summary>囁きのイヤリングの持ち主。</summary>
    public Player Owner { get; }

    public Creature OwnerCreature => Owner.Creature;

    /// <summary>
    /// 持ち主がこの端末のプレイヤー自身か。ダメージ予測などが「ローカルプレイヤー視点」でしか
    /// 正確に計算できないものは、これがtrueの時だけ使うこと。
    /// </summary>
    public bool OwnerIsLocalPlayer => LocalContext.IsMe(Owner);

    /// <summary>持ち主のターン数(1始まり。戦闘ごとにリセット)。</summary>
    public int TurnNumber => Owner.PlayerCombatState?.TurnNumber ?? 0;

    /// <summary>
    /// 最初のターンか。囁きのイヤリングでは、この最初のターンはヴァクーが代打ちするターンにあたり、
    /// ヴァクーの代打ち、その後のプレイヤーの操作、続く敵のターンまでを含む(次のプレイヤーのターンが始まると2になる)。
    /// 「ヴァクーが動いたターンとその余波では言わせたくない」Detectorは、これで除外する。
    /// </summary>
    public bool IsFirstTurn => TurnNumber == 1;

    /// <summary>
    /// 今が「ヴァクーのターン」(最初のターンの、手札が配られた後〜代打ちが終わるまでの<c>AutoPrePlay</c>フェーズ)か。
    /// </summary>
    public bool IsInVakuuTurn => IsFirstTurn && Owner.PlayerCombatState?.Phase == PlayerTurnPhase.AutoPrePlay;

    /// <summary>
    /// 最初のターンが、囁きのイヤリングの代打ちで消費されたターンだったか(＝ヴァクーが実際に操作した最初のターン)。
    /// 「プレイヤー自身の判断とは言えない行動があったターンでは言わせたくない」Detectorは、これで除外する
    /// (デバッグ設定でレリック無しでも発言システムを動かしている場合、最初のターンも普通にプレイヤー自身の
    /// ターンなので<see cref="IsFirstTurn"/>だけで除外すると過剰になる)。
    /// </summary>
    public bool WasVakuuTurn => IsFirstTurn && HasRelic<WhisperingEarring>();

    public int Block => OwnerCreature.Block;

    public int Hp => OwnerCreature.CurrentHp;

    public int MaxHp => OwnerCreature.MaxHp;

    /// <summary>この戦闘の「エンカウント」定義そのもの(通常戦の個々のモンスターとは別に、エリート・ボス等
    /// 複数体で構成されうる戦闘全体を指す単位)。不明ならnull。</summary>
    public EncounterModel? Encounter => _combatState.Encounter;

    /// <summary>この戦闘の種類(通常・エリート・ボス)。不明ならnull。</summary>
    public RoomType? EncounterRoomType => Encounter?.RoomType;

    /// <summary>
    /// この<see cref="Encounter"/>についての、モンスター図鑑と同じ勝敗記録(<c>SaveManager.Progress.EncounterStats</c>、
    /// キャラクター別の内訳付き)。本家が戦闘終了のたびに増分更新している記録をそのまま参照するだけなので、
    /// プレイ履歴を毎回全走査するような重い処理ではない。一度も記録が無ければnull。
    /// </summary>
    public EncounterStats? GetEncounterStats() =>
        Encounter is { } encounter ? SaveManager.Instance.Progress.EncounterStats.GetValueOrDefault(encounter.Id) : null;

    /// <summary>手札に、今プレイできるカード(エナジーが足りる・使用不能でない)があるか。</summary>
    public bool HasPlayableCardInHand() => Hand.Any(card => card.CanPlay());

    /// <summary>
    /// 持ち主が何も操作せずにいる秒数(カードのプレイ・ポーション使用・フェーズの切り替えで0に戻る)。
    /// <see cref="DetectorTrigger.Tick"/>の間だけ加算される。
    /// </summary>
    public double IdleSeconds { get; private set; }

    /// <summary>持ち主がターン終了を押した後か(マルチプレイで他のプレイヤーを待っている間など)。</summary>
    public bool HasEndedTurn { get; internal set; }

    /// <summary>今、持ち主が自分のターンを操作できる状態か。</summary>
    public bool IsPlayerActing =>
        Owner.PlayerCombatState?.Phase == PlayerTurnPhase.Play
        && !HasEndedTurn
        && CombatManager.Instance is { IsInProgress: true, PlayerActionsDisabled: false };

    internal void AddIdleTime(double seconds) => IdleSeconds += seconds;

    internal void NoteActivity() => IdleSeconds = 0;

    /// <summary>ターン内のフェーズが切り替わった時に、ターン単位の状態(放置時間・ターン終了済み)を戻す。</summary>
    internal void ResetTurnState()
    {
        IdleSeconds = 0;
        HasEndedTurn = false;
    }

    public int Energy => Owner.PlayerCombatState?.Energy ?? 0;

    public IReadOnlyList<CardModel> Hand => Owner.PlayerCombatState?.Hand.Cards ?? Array.Empty<CardModel>();

    /// <summary>ランのデッキ全体(戦闘中に生成されたカードは含まない)。</summary>
    public IReadOnlyList<CardModel> Deck => Owner.Deck.Cards;

    public IEnumerable<Creature> LivingEnemies => _combatState.Enemies.Where(e => e.IsAlive);

    /// <summary>
    /// <see cref="DetectorTrigger.CardPlayed"/>で呼ばれた時の、直前に持ち主が手動でプレイしたカード。
    /// それ以外のトリガーでは古い値かnullなので参照しないこと。
    /// </summary>
    public CardPlay? LastManualCardPlay { get; internal set; }

    /// <summary>
    /// <see cref="DetectorTrigger.FriendCardPlayed"/>で呼ばれた時の、直前に持ち主が手動でプレイしたカード。
    /// それ以外のトリガーでは古い値かnullなので参照しないこと。
    /// </summary>
    public CardPlay? LastFriendCardPlay { get; internal set; }

    /// <summary>
    /// <see cref="DetectorTrigger.EnemyKilled"/>で呼ばれた時の、直前に倒された敵。
    /// それ以外のトリガーでは古い値かnullなので参照しないこと。
    /// </summary>
    public Creature? LastKilledEnemy { get; internal set; }

    /// <summary>
    /// <see cref="LastKilledEnemy"/>を倒したプレイヤー。分からない場合(毒などの継続効果、敵自身によるダメージ等)はnull。
    /// 敵を倒したのがヴァクーの代打ちのカードの場合も、持ち主本人のカードなので持ち主(<see cref="Owner"/>)になる。
    /// </summary>
    public Player? LastKiller { get; internal set; }

    /// <summary>直前の撃破で、敵のHPを超えて余った(無駄になった)ダメージ量。</summary>
    public int LastKillOverkillDamage { get; internal set; }

    /// <summary>
    /// <see cref="DetectorTrigger.DamageTaken"/>で呼ばれた時の、持ち主が受けたダメージの記録
    /// (与えた側は<c>Dealer</c>、ブロックされた量・HPが減った量は<c>Result</c>)。
    /// それ以外のトリガーでは古い値かnullなので参照しないこと。
    /// </summary>
    public DamageReceivedEntry? LastDamageTaken { get; internal set; }

    /// <summary>
    /// <see cref="DetectorTrigger.BlockRelicActivated"/>で呼ばれた時の、直前に発動したブロック付与レリック。
    /// それ以外のトリガーでは古い値かnullなので参照しないこと。
    /// </summary>
    public RelicModel? LastActivatedBlockRelic { get; internal set; }

    /// <summary>
    /// <see cref="DetectorTrigger.PotionUsed"/>で呼ばれた時の、直前に持ち主が使用したポーション。
    /// それ以外のトリガーでは古い値かnullなので参照しないこと。
    /// </summary>
    public PotionModel? LastUsedPotion { get; internal set; }

    /// <summary>
    /// <see cref="LastUsedPotion"/>の対象。味方・敵どちらもありうる(例: 血液ポーション系は味方、
    /// 火炎ポーションは敵)ので<see cref="Creature"/>で受ける。対象を取らないポーション(ブロックポーション等)ではnull。
    /// </summary>
    public Creature? LastUsedPotionTarget { get; internal set; }

    /// <summary>
    /// <see cref="DetectorTrigger.CardDrawn"/>で呼ばれた時の、直前にターン中に引かれたカード
    /// (最初のターン開始時の配札は含まない)。それ以外のトリガーでは古い値かnullなので参照しないこと。
    /// </summary>
    public CardModel? LastDrawnCard { get; internal set; }

    /// <summary>
    /// <see cref="DetectorTrigger.CardPlayStarted"/>で呼ばれた時の、直前にプレイが開始されたカード
    /// (自動プレイも含む)。それ以外のトリガーでは古い値かnullなので参照しないこと。
    /// </summary>
    public CardPlay? LastCardPlayStarted { get; internal set; }

    /// <summary>直前の撃破が、持ち主本人によるものか。</summary>
    public bool LastKillWasByOwner => LastKiller == Owner;

    /// <summary>直前の撃破が、持ち主以外のプレイヤー(マルチプレイの仲間)によるものか。</summary>
    public bool LastKillWasByOtherPlayer => LastKiller != null && LastKiller != Owner;

    /// <summary>この戦闘に、持ち主以外のプレイヤーがいるか(マルチプレイか)。</summary>
    public bool HasOtherPlayers => _combatState.Players.Count > 1;

    /// <summary>この戦闘における、持ち主以外のプレイヤー(マルチプレイの仲間)。1人もいなければ空。</summary>
    public IEnumerable<Player> Teammates => _combatState.Players.Where(p => p != Owner);

    public bool HasRelic<T>() where T : RelicModel => Owner.GetRelic<T>() != null;

    public T? GetRelic<T>() where T : RelicModel => Owner.GetRelic<T>();

    public bool HasPower<T>() where T : PowerModel => OwnerCreature.HasPower<T>();

    public int GetPowerAmount<T>() where T : PowerModel => OwnerCreature.GetPowerAmount<T>();

    /// <summary>持ち主が、まだ使っていない指定の型のポーションを持っているか(ポーション欄に残っている)。</summary>
    public bool HasUnusedPotion<T>() where T : PotionModel => HasUnusedPotion<T>(Owner);

    /// <summary>
    /// 任意のプレイヤーについて、まだ使っていない指定の型のポーションを持っているか。ポーションの対象
    /// (<see cref="LastUsedPotionTarget"/>)はマルチプレイの仲間になりうるので、持ち主固定の
    /// <see cref="HasUnusedPotion{T}()"/>とは別に、対象を指定できる版を用意している。
    /// </summary>
    public static bool HasUnusedPotion<T>(Player player) where T : PotionModel => player.Potions.Any(p => p is T);

    /// <summary>
    /// 指定したクリーチャーの手札。プレイヤーでない(敵の)クリーチャーや、何らかの理由で戦闘状態が
    /// 取れない場合は空。ポーションの対象(<see cref="LastUsedPotionTarget"/>)の手札を調べる判定で使う
    /// (持ち主自身の手札は<see cref="Hand"/>を使えばよい)。
    /// </summary>
    public static IReadOnlyList<CardModel> HandOf(Creature creature) =>
        creature.Player?.PlayerCombatState?.Hand.Cards ?? Array.Empty<CardModel>();

    /// <summary>指定したクリーチャーの山札(ドローパイル)の残り枚数。</summary>
    public static int DrawPileCountOf(Creature creature) =>
        creature.Player?.PlayerCombatState?.DrawPile.Cards.Count ?? 0;

    /// <summary>
    /// カードの攻撃ダメージを表す<c>DynamicVar</c>を1つ選んで返す。優先順位: <c>Damage</c> → <c>CalculatedDamage</c> →
    /// <c>OstyDamage</c>(カードは通常これらのうちどれか1つだけを持つ設計なので、実質「どれが定義されているか」を
    /// 吸収するだけ)。どれも持たなければnull(このカードは直接ダメージを与えない)。
    /// </summary>
    /// <remarks>
    /// 呼び出し側は、返ってきた<c>DynamicVar</c>の実際の型に応じて値の取り出し方を選ぶこと
    /// (現在のフック込みの見込み値なら<c>PreviewValue</c>、対象ごとの計算式が要るなら
    /// <c>CalculatedDamageVar.Calculate(target)</c>等)。<see cref="AttackDamageBenefitsFromStrength"/>も参照
    /// (オスティ由来のダメージは持ち主自身の筋力が乗らない)。
    /// </remarks>
    public static DynamicVar? GetAttackDamageVar(CardModel card)
    {
        if (card.DynamicVars.TryGetValue("Damage", out DynamicVar? damage))
        {
            return damage;
        }

        if (card.DynamicVars.TryGetValue("CalculatedDamage", out DynamicVar? calculated))
        {
            return calculated;
        }

        return card.DynamicVars.TryGetValue("OstyDamage", out DynamicVar? osty) ? osty : null;
    }

    /// <summary>
    /// <see cref="GetAttackDamageVar"/>が返すダメージ源が、持ち主自身の筋力の恩恵を受けるか。
    /// オスティ(Osty、プレイヤーとは別のクリーチャーである相棒)由来のダメージ(<c>OstyDamageVar</c>、または
    /// <c>IsFromOsty</c>が立った<c>CalculatedDamageVar</c>。例: <c>Unleash</c>・<c>Protector</c>)は、
    /// オスティ自身の筋力は乗るが、持ち主(プレイヤー)の筋力(フレックスポーション等で得るもの)は乗らない。
    /// ダメージを与えないカード(<see cref="GetAttackDamageVar"/>がnull)はfalse。
    /// </summary>
    public static bool AttackDamageBenefitsFromStrength(CardModel card) => GetAttackDamageVar(card) switch
    {
        OstyDamageVar => false,
        CalculatedDamageVar { IsFromOsty: true } => false,
        { } => true,
        null => false,
    };

    /// <summary>
    /// カードのブロック量を表す<c>DynamicVar</c>を1つ選んで返す。優先順位: <c>Block</c> → <c>CalculatedBlock</c>
    /// (カードは通常どちらか1つだけを持つ設計)。どちらも持たなければnull(このカードは直接ブロックを与えない)。
    /// </summary>
    public static DynamicVar? GetBlockVar(CardModel card)
    {
        if (card.DynamicVars.TryGetValue("Block", out DynamicVar? block))
        {
            return block;
        }

        return card.DynamicVars.TryGetValue("CalculatedBlock", out DynamicVar? calculated) ? calculated : null;
    }

    /// <summary>
    /// このカードがブロックを与えるか(<see cref="GetBlockVar"/>が非null)。
    /// スピードポーション(敏捷付与)がブロックの足しになる手札を持っているかの判定などで使う。
    /// </summary>
    public static bool CardProvidesBlock(CardModel card) => GetBlockVar(card) != null;

    /// <summary>
    /// このカードがダメージを与えるか(<see cref="GetAttackDamageVar"/>が非null)。
    /// 「ダメージを与えるかどうか」だけを見たい場合に使う。筋力(フレックスポーション等)の恩恵を受けるかまで
    /// 見たい場合は<see cref="AttackDamageBenefitsFromStrength"/>を使うこと。
    /// </summary>
    public static bool CardProvidesDamage(CardModel card) => GetAttackDamageVar(card) != null;

    /// <summary>
    /// このカードがエナジーを与えるか(<c>EnergyVar</c>を持つか)。「エナジー不足で今はプレイできないカードでも、
    /// 先にこのカードをプレイすれば実質プレイできる」かどうかの判定(<c>CardDrawnDetector</c>)で使う。
    /// </summary>
    public static int EnergyCardProvide(CardModel card) => card.DynamicVars.TryGetValue("Energy", out var e)
        ? e.IntValue - (card.Keywords.Contains(CardKeyword.Sly) ? 0 : card.EnergyCost.GetResolved()) : 0;

    /// <summary>
    /// このカードが、今の戦闘中に(コピー・生成効果などで)新しく生成されたものか。デッキに元からあった
    /// カードではないので、「エセリアルなのに引けて嬉しい/使えず勿体ない」といった、デッキ構築を前提にした
    /// 判定からは除外したい時に使う(<c>CombatHistory</c>の<c>CardGeneratedEntry</c>から判定)。
    /// </summary>
    public static bool WasCardGeneratedThisCombat(CardModel card) =>
        CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Any(e => e.Card == card);

    /// <summary>
    /// この戦闘が、ダブルボス(Ascension「DoubleBoss」)の1戦目に該当するか。1戦目は、直後にもう一度
    /// ボス戦が控えているので、ボス用に温存する判断が正当になりうる場面。2戦目やダブルボスでない
    /// 通常のボス戦は対象外(false)。
    /// </summary>
    public bool IsFirstOfDoubleBossEncounter
    {
        get
        {
            ActModel act = Owner.RunState.Act;
            return act.SecondBossEncounter != null && _combatState.Encounter == act.BossEncounter;
        }
    }

    /// <summary>ランで現在いる層(Act)のインデックス(0始まり)。<see cref="ActModel.Index"/>参照。</summary>
    public int ActIndex => Owner.RunState.Act.Index;

    /// <summary>今ターン、持ち主が終えたカードプレイ(手動・自動の両方)。</summary>
    public IEnumerable<CardPlayFinishedEntry> CardPlaysThisTurn()
    {
        return CombatManager.Instance.History.CardPlaysFinished
            .Where(e => e.CardPlay.Card.Owner == Owner && e.HappenedThisTurn(_combatState));
    }

    /// <summary>
    /// 今ターン、自動プレイされたカードの枚数。最初のターンでは主にヴァクーの代打ち分に当たる
    /// (他の自動プレイ効果があればそれも含まれる)。
    /// </summary>
    public int AutoPlayedCardCountThisTurn => CardPlaysThisTurn().Count(e => e.CardPlay.IsAutoPlay);

    /// <summary>
    /// 今ターン(プレイヤーのターン中)に倒された敵。誰が倒したかは問わない。
    /// 最初のターンの代打ちが終わった時点なら、ヴァクーの代打ち中に倒された敵にあたる。
    /// </summary>
    /// <remarks>
    /// <c>CombatHistory</c>のダメージ記録から拾うので、ダメージ以外で倒れた敵(HP減少効果など)は含まれない。
    /// </remarks>
    public IEnumerable<Creature> EnemiesKilledThisTurn()
    {
        return CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(e => e.Receiver.Side == CombatSide.Enemy && e.Result.WasTargetKilled && e.HappenedThisTurn(_combatState))
            .Select(e => e.Receiver)
            .Distinct();
    }

    /// <summary>今ターン、持ち主が敵から受けた(ブロックで防げなかった)ダメージの合計。</summary>
    public int HpLostThisTurn()
    {
        return CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(e => e.Receiver == OwnerCreature && e.HappenedThisTurn(_combatState))
            .Sum(e => e.Result.UnblockedDamage);
    }

    /// <summary>今ターン、持ち主がダメージを与えた(攻撃した)敵。生死・現在のHP/ブロックは呼び出し時点のもの。</summary>
    public IEnumerable<Creature> EnemiesAttackedThisTurn()
    {
        return CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(e => e.Dealer == OwnerCreature && e.Receiver.Side == CombatSide.Enemy && e.HappenedThisTurn(_combatState))
            .Select(e => e.Receiver)
            .Distinct();
    }

    /// <summary>
    /// 今ターン、持ち主がカードで直接ダメージを与えた敵(<c>CardSource</c>があるダメージのみ。
    /// 棘等の反射ダメージや継続効果は含まない)。
    /// </summary>
    public IEnumerable<Creature> EnemiesDamagedByCardsThisTurn()
    {
        return CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(e => e.CardSource != null && e.Dealer == OwnerCreature && e.Receiver.Side == CombatSide.Enemy
                        && e.HappenedThisTurn(_combatState))
            .Select(e => e.Receiver)
            .Distinct();
    }

    /// <summary>
    /// 今ターン、持ち主が手動でプレイしたカードによるブロック獲得の中に、<paramref name="surplus"/>
    /// (現時点の総ブロックのうち、想定被弾量を上回っている分)以下の量だったものが1つでもあったか。
    /// そのカードが無くても<paramref name="surplus"/>分の余裕は既にあったはずなので、そのカードの獲得分は
    /// 丸ごと不要だった、とみなせる。
    /// </summary>
    /// <remarks>
    /// 0コストのカード・自動プレイによる獲得は対象外(<c>CardPlay.Resources.EnergySpent == 0</c>という1条件で
    /// 両方まとめて除外できる。本家の<c>ResourceInfo.EnergySpent</c>のドキュメントコメントより、
    /// 自動プレイされたカードは名目コストに関わらず<c>EnergySpent</c>が常に0になる)。
    /// コストを払っていない獲得は、プレイヤー自身の配分ミスとは言いにくいため。
    /// </remarks>
    public bool WasBlockCardWastedThisTurn(int surplus)
    {
        if (surplus <= 0)
        {
            return false;
        }

        return CombatManager.Instance.History.Entries
            .OfType<BlockGainedEntry>()
            .Where(e => e.Receiver == OwnerCreature && e.HappenedThisTurn(_combatState))
            .Any(e => e.CardPlay is { Resources.EnergySpent: > 0 } && e.Amount <= surplus);
    }

    /// <summary>
    /// 指定したカードプレイが、その対象に実際に与えた(ブロック後・オーバーキル込みでHPが実際に減った)
    /// ダメージの実測値。<c>DynamicVars</c>からの予測計算(Weak/Vulnerable補正・<c>CalculatedDamageVar</c>等の
    /// カード固有ロジックを自前で再現する必要があり、カードによっては再現しきれない)ではなく、
    /// <c>CombatHistory</c>に実際に記録された値をそのまま拾う。
    /// </summary>
    /// <remarks>
    /// 対応する<see cref="CardPlayStartedEntry"/>(このプレイと同一の<c>CardPlay</c>インスタンス)まで履歴を
    /// 遡り、その区間内で<c>CardSource</c>が同じ<c>CardModel</c>インスタンスかつ対象が一致する
    /// <see cref="DamageReceivedEntry"/>だけを合算する。この区間縛りのおかげで、同じカードインスタンスが
    /// 同ターンに複数回プレイされていても(リプレイ等)このプレイの分だけを正しく拾え、複数回ヒットする攻撃
    /// (Repeat等)も区間内の全エントリが自然に合算されるので、ヒット回数を別途考慮する必要はない。
    /// </remarks>
    /// <returns>対象が無い、または対応する履歴が見つからない場合は0。</returns>
    public int GetRealizedDamage(CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return 0;
        }

        int total = 0;
        foreach (CombatHistoryEntry entry in CombatManager.Instance.History.Entries.Reverse())
        {
            if (entry is CardPlayStartedEntry started && started.CardPlay == cardPlay)
            {
                break;
            }

            if (entry is DamageReceivedEntry { CardSource: { } source } damage
                && source == cardPlay.Card && damage.Receiver == cardPlay.Target)
            {
                total += damage.Result.UnblockedDamage + damage.Result.OverkillDamage;
            }
        }

        return total;
    }

    /// <summary>
    /// 敵(既定では生きている敵全員)が次の行動で持ち主に与える攻撃ダメージの合計の予測(ブロック考慮前)。
    /// </summary>
    /// <param name="enemies">
    /// 集計対象の敵を絞りたい時に指定する(例: 「特定の1体を除いたら」を見たい場合は
    /// <c>LivingEnemies.Where(e =&gt; e != target)</c>を渡す)。省略時は<see cref="LivingEnemies"/>全員。
    /// </param>
    /// <remarks>
    /// ダメージの補正計算(脆弱など)は本家の<c>AttackIntent</c>がローカルプレイヤー視点で行うため、
    /// <see cref="OwnerIsLocalPlayer"/>がfalseの時は正確でない。
    /// </remarks>
    public int EstimateIncomingAttackDamage(IEnumerable<Creature>? enemies = null)
    {
        Creature[] targets = { OwnerCreature };
        int total = 0;
        foreach (Creature enemy in enemies ?? LivingEnemies)
        {
            if (enemy.Monster == null)
            {
                continue;
            }

            // MultiAttackIntent inherits AttackIntent.
            foreach (AttackIntent attack in enemy.Monster.NextMove.Intents.OfType<AttackIntent>())
            {
                total += attack.GetTotalDamage(targets, enemy);
            }
        }

        return total;
    }

    /// <summary>
    /// 生きている敵から<paramref name="enemy"/>を除いた場合に、実際に受けるダメージ
    /// (想定被弾量からブロックを引いた分。0未満にはならない)がどれだけ減るか。
    /// </summary>
    /// <remarks>
    /// 「攻撃さえ来なくなるか(=0になるか)」ではなく「どれだけ軽くなるか」を見る。既にブロックが
    /// 被弾の大半をカバーしている場合は、この敵を除いても実質的な軽減はほぼ無い(0に近い値になる)ので、
    /// 呼び出し側は返り値に閾値を設けて「そこそこ効果がある時だけ」に絞るとよい。
    /// </remarks>
    public int EstimateDamageReductionFromRemoving(Creature enemy)
    {
        int block = Block;
        int withEnemy = Math.Max(0, EstimateIncomingAttackDamage() - block);
        int withoutEnemy = Math.Max(0, EstimateIncomingAttackDamage(LivingEnemies.Where(e => e != enemy)) - block);
        return withEnemy - withoutEnemy;
    }

    /// <summary>
    /// 指定した敵が、既に崩御(<c>DemisePower</c>、スタックが貯まるとそのクリーチャー自身のターン終了時に
    /// HPを失う持続ダメージ)のスタックだけで自滅することが確定しているか(スタック量が現在HP以上)。
    /// </summary>
    /// <remarks>
    /// 崩御は対象自身のターン終了時に発動するため、今まさに来ようとしている攻撃(このターン中に来る分)を
    /// 止めてはくれない。それでも、既にこれだけ積んであるなら「この敵は崩御で仕留める計画で、意図的に
    /// ポーションを温存している」可能性が高いとみなし、単体ダメージポーション系の「倒せば軽減できる」判定から
    /// この敵を除外する(でないと、既に必殺の算段がついている敵にまでポーションを使えとうるさく言ってしまう)。
    /// </remarks>
    public bool IsDoomedByDemise(Creature enemy) => enemy.GetPowerAmount<DemisePower>() >= enemy.CurrentHp;

    /// <summary>
    /// 生きている敵全員の筋力が<paramref name="strengthReduction"/>下がっていたら、実際に受けるダメージ
    /// (想定被弾量からブロックを引いた分。0未満にはならない)がどれだけ減るかを見積もる。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 「1ヒットにつき筋力の減少分だけダメージが下がる」という単純な引き算では、脆弱(<c>VulnerablePower</c>)・
    /// 弱体(<c>WeakPower</c>)・巨人(<c>ColossusPower</c>)等の乗算補正を無視してしまい、過大評価になりうる
    /// (実例: 筋力ぶんそのまま引いた場合、脆弱の敵に対する巨人の50%軽減が二重に効かない)。
    /// </para>
    /// <para>
    /// そこで、各攻撃について「今まさに<c>AttackIntent.GetSingleDamage</c>(<c>Hook.ModifyDamage</c>経由で、
    /// 筋力・脆弱・弱体・巨人・その他登録されている全ての<c>ModifyDamageMultiplicative</c>/
    /// <c>ModifyDamageAdditive</c>フックを適用済み)が返す実際の値」と、「素のダメージ(<c>DamageCalc()</c>、
    /// 筋力を含むフック適用前の値)」を比較して、この攻撃に効いている倍率を逆算する
    /// (<c>倍率 = 実際の値 ÷ (素のダメージ + 現在の筋力)</c>)。この倍率を、筋力を減らした後の加算後の値
    /// (<c>素のダメージ + 現在の筋力 - 減少分</c>、下限0)に掛け直すことで、その場に実際に効いている
    /// 乗算補正を保ったまま「筋力だけ下がったら」を近似する。
    /// </para>
    /// <para>
    /// 前提: 攻撃1回ぶんの加算補正が実質「素のダメージ+筋力」だけである(=筋力以外の加算フックが乗っていない)
    /// ケースがほとんどであることに依存した近似。稀に他の加算効果が乗っているとズレるが、乗算補正を
    /// 完全に無視するより大幅に正確になる。
    /// </para>
    /// </remarks>
    public int EstimateDamageReductionFromLoweringStrength(int strengthReduction)
    {
        if (strengthReduction <= 0)
        {
            return 0;
        }

        Creature[] targets = { OwnerCreature };
        int reducedTotal = 0;
        foreach (Creature enemy in LivingEnemies)
        {
            if (enemy.Monster == null)
            {
                continue;
            }

            int currentStrength = enemy.GetPowerAmount<StrengthPower>();
            foreach (AttackIntent attack in enemy.Monster.NextMove.Intents.OfType<AttackIntent>())
            {
                int modifiedPerHit = attack.GetSingleDamage(targets, enemy);
                decimal additiveBase = (attack.DamageCalc?.Invoke() ?? modifiedPerHit) + currentStrength;
                decimal multiplier = additiveBase > 0 ? modifiedPerHit / additiveBase : 1m;
                decimal reducedBase = Math.Max(0, additiveBase - strengthReduction);
                reducedTotal += Math.Max(0, (int)(reducedBase * multiplier)) * attack.Repeats;
            }
        }

        int block = Block;
        int unblockedCurrent = Math.Max(0, EstimateIncomingAttackDamage() - block);
        int unblockedReduced = Math.Max(0, reducedTotal - block);
        return unblockedCurrent - unblockedReduced;
    }
}
