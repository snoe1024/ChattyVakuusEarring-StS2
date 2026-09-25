using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Config;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter;

/// <summary>
/// ゲーム本体の戦闘イベントと<see cref="ChatterSession"/>を結びつける、このmodの唯一の入口。
/// </summary>
/// <remarks>
/// <c>CombatManager</c>の公開イベントを購読するだけで、Harmonyパッチは使わない。
/// 囁きのイヤリングの持ち主がいない戦闘ではセッションが作られず、各ハンドラは何もせず即座に戻る。
/// マルチプレイでは自分自身の分しかセッションを作らない(<c>OnCombatSetUp</c>参照)ので、各クライアントの
/// 画面には、そのクライアントを操作しているプレイヤー自身のヴァクーしか喋らない。
/// ゲームの戦闘進行の途中(イベント発火元)で呼ばれるので、例外は全てここで握りつぶしてログに出す
/// (mod側のバグで戦闘そのものを止めない)。
/// </remarks>
internal static class ChatterHub
{
    private static readonly List<ChatterSession> Sessions = new();

    /// <summary>直前までに処理済みの<c>CombatHistory</c>のエントリ数(新規エントリだけを拾うため)。</summary>
    private static int _processedHistoryEntries;

    /// <summary>時間経過(<see cref="DetectorTrigger.Tick"/>)を伝える間隔。</summary>
    private const double TickIntervalSeconds = 1.0;

    /// <summary>
    /// 1回のTickで進める時間の上限。ポーズ・ロード等でタイマーが止まって戻ってきた時に、
    /// 止まっていた時間ぶんを一気に「放置」として数えないため。
    /// </summary>
    private const double MaxTickDeltaSeconds = 2.0;

    private static bool _ticking;

    private static long _lastTickTimestamp;

    public static void Initialize()
    {
        CombatManager combat = CombatManager.Instance;
        combat.CombatSetUp += state => Guard(() => OnCombatSetUp(state));
        combat.TurnStarted += state => Guard(() => OnTurnStarted(state));
        combat.PlayerEndedTurn += (player, _) => Guard(() => OnPlayerEndedTurn(player));
        combat.PlayerUnendedTurn += player => Guard(() => OnPlayerUnendedTurn(player));
        combat.AboutToSwitchToEnemyTurn += _ => Guard(OnTurnReallyEnded);
        combat.History.Changed += () => Guard(OnHistoryChanged);
        combat.CombatEnded += _ => Guard(OnCombatEnded);
    }

    /// <summary>
    /// このクリーチャーの発言を、現在いずれかのセッションが引き受けているか。
    /// バニラのイヤリング台詞の抑制パッチが、「自分たちが代わりに喋る」場合にだけ抑制するための判定。
    /// </summary>
    public static bool IsHandling(Creature? speaker)
    {
        return speaker != null && Sessions.Any(s => s.Owner.Creature == speaker);
    }

    /// <summary>
    /// 今、戦闘中でヴァクーの発言システムが動いている(=この端末のローカルプレイヤーが対象のセッションを持つ)か。
    /// どのDetectorが実際に発言するかとは無関係な、大元の有効/無効だけの判定
    /// (<see cref="Patch.ReturnToMainMenuSpeechPatch"/>が、個々のDetectorの条件に依存せず「セーブスカムの
    /// 遅延を入れるべきか」を判断するために使う)。
    /// </summary>
    public static bool IsActive => Sessions.Count > 0;

    /// <summary>
    /// バニラのイヤリングが締めの台詞を言おうとした(=ヴァクーの代打ちが終わった)ことを、
    /// 抑制パッチから伝えるための入口。
    /// </summary>
    public static void OnVakuuFinishedPlaying(Creature speaker)
    {
        Guard(() =>
        {
            foreach (ChatterSession session in Sessions.Where(s => s.Owner.Creature == speaker).ToList())
            {
                session.Dispatch(DetectorTrigger.VakuuFinishedPlaying);
            }
        });
    }

    /// <summary>
    /// 音叉・扇子・銀河の塵・波紋の鉢・マントの留め具が発動したことを、<c>BlockRelicFlashPatch</c>から伝えるための入口。
    /// これらのレリックが「発動したか」は外部から知れる公開イベント/履歴を持たないため、Harmonyで直接捕まえてここへ渡す
    /// (詳細は<c>BlockRelicFlashPatch</c>のドキュメント参照)。
    /// </summary>
    public static void OnBlockRelicActivated(RelicModel relic)
    {
        Guard(() =>
        {
            foreach (ChatterSession session in Sessions.Where(s => s.Owner == relic.Owner).ToList())
            {
                session.Observer.LastActivatedBlockRelic = relic;
                session.Dispatch(DetectorTrigger.BlockRelicActivated);
            }
        });
    }

    /// <summary>
    /// メインメニューへ戻る操作が確定したことを、<c>ReturnToMainMenuSpeechPatch</c>から伝えるための入口。
    /// 戦闘が無い時は<see cref="Sessions"/>が空なので、何も起きない。
    /// </summary>
    public static void OnReturnedToMainMenu()
    {
        Guard(() =>
        {
            foreach (ChatterSession session in Sessions.ToList())
            {
                session.Dispatch(DetectorTrigger.ReturnedToMainMenu);
            }
        });
    }

    private static void OnCombatSetUp(CombatState state)
    {
        // 前の戦闘が(ラン中断などで)CombatEndedを経ずに終わった場合の取りこぼし対策。
        EndSessions();

        foreach (Player player in state.Players)
        {
            // マルチプレイでは、自分以外のプレイヤーの分は作らない。他人の囁きのイヤリングについて
            // 自分の画面でヴァクーを喋らせても、こちらの操作でもこちらの端末視点の判定でもないので
            // ただうるさいだけ(かつ、ダメージ予測系のAPIはローカルプレイヤー視点でしか正確に動かないため、
            // 他人のセッションを作ると不正確な判定が紛れ込む余地も生む)。「仲間が倒した」等、他人の行動への
            // 反応は自分自身のセッション(FriendKillDetector等)側で拾うので、これで機能が失われることはない。
            if (!LocalContext.IsMe(player))
            {
                continue;
            }

            // 設定がオンの間は、レリックを持っていなくても発言システムを有効にする。ただしバニラの
            // 囁きのイヤリング自身の代打ち処理には一切手を入れていないので、代打ちに関わる発言
            // (FirstTurnReviewDetector/VakuuKillDetector、VanillaEarringSpeechPatch経由のもの)は
            // レリックを実際に持っていない限り発火しない。
            if (player.GetRelic<WhisperingEarring>() != null || ChattyVakuusEarringConfig.AllowWhisperingWithoutEarring)
            {
                Sessions.Add(new ChatterSession(player, state));
            }
        }

        _processedHistoryEntries = CombatManager.Instance.History.Entries.Count();

        EnsureTicking();
    }

    private static void OnTurnStarted(CombatState state)
    {
        // 敵のターン開始でも発火するので、プレイヤーのターンだけを拾う。
        // プレイヤーのターン開始は全員の準備(最初のターンなら代打ちの自動プレイも)が終わった後に1回だけ発火する。
        if (state.CurrentSide != CombatSide.Player)
        {
            return;
        }

        foreach (ChatterSession session in Sessions.ToList())
        {
            session.Dispatch(DetectorTrigger.PlayerTurnStarted);
        }
    }

    private static void OnPlayerEndedTurn(Player player)
    {
        // プレイヤーが自分でターン終了を押した場合のみ拾う。ターン開始時の自動プレイ中にターンが終わるカード
        // (ヴァクーの代打ち等)や、行動不能の持ち主が自動で「終了扱い」になる場合は、Phaseがプレイ中ではない。
        if (player.PlayerCombatState?.Phase != PlayerTurnPhase.Play)
        {
            return;
        }

        foreach (ChatterSession session in Sessions.Where(s => s.Owner == player).ToList())
        {
            session.Observer.HasEndedTurn = true;
            session.Dispatch(DetectorTrigger.PlayerEndedTurn);
        }
    }

    private static void OnPlayerUnendedTurn(Player player)
    {
        foreach (ChatterSession session in Sessions.Where(s => s.Owner == player).ToList())
        {
            session.Observer.HasEndedTurn = false;
            session.Observer.NoteActivity();
        }
    }

    /// <summary>
    /// プレイヤーのターンが本当に終わった時(ターン終了時効果が全て済み、これから敵のターンに切り替わる直前)。
    /// ターン終了ボタンを押した瞬間(<see cref="OnPlayerEndedTurn"/>)より後なので、ターン終了時効果による
    /// 撃破・ブロック増加等の結果を踏まえて判定できる。
    /// </summary>
    private static void OnTurnReallyEnded()
    {
        foreach (ChatterSession session in Sessions.ToList())
        {
            session.Dispatch(DetectorTrigger.TurnEnded);
        }
    }

    private static void OnHistoryChanged()
    {
        if (Sessions.Count == 0)
        {
            return;
        }

        IEnumerable<CombatHistoryEntry> entries = CombatManager.Instance.History.Entries;
        int count = entries.Count();
        if (count < _processedHistoryEntries)
        {
            // Clear()された。
            _processedHistoryEntries = 0;
        }

        List<CombatHistoryEntry> freshEntries = entries.Skip(_processedHistoryEntries).ToList();
        _processedHistoryEntries = count;

        foreach (CombatHistoryEntry entry in freshEntries)
        {
            switch (entry)
            {
                // 早指し判定等、ボタンを押した瞬間の間隔を見たいDetector向け(自動プレイも含めて渡す。
                // 除外は購読側でLastCardPlayStarted.IsAutoPlayを見て行う)。自分以外(マルチプレイの仲間)の分は
                // 渡さない(プレイのペースは各プレイヤー自身の話なので)。
                case CardPlayStartedEntry started:
                    foreach (ChatterSession session in Sessions.Where(s => s.Owner == started.CardPlay.Card.Owner).ToList())
                    {
                        session.Observer.LastCardPlayStarted = started.CardPlay;
                        session.Dispatch(DetectorTrigger.CardPlayStarted);
                    }

                    break;

                // 自動プレイ(ヴァクーの代打ち等)は「プレイヤーのプレイスタイル」ではないので対象外。
                case CardPlayFinishedEntry { CardPlay.IsAutoPlay: false } played:
                    foreach (ChatterSession session in Sessions)
                    {
                        if (session.Owner == played.CardPlay.Card.Owner)
                        {
                            session.Observer.NoteActivity();
                            session.Observer.LastManualCardPlay = played.CardPlay;
                            session.Dispatch(DetectorTrigger.CardPlayed);
                        }
                        else
                        {
                            session.Observer.NoteActivity();
                            session.Observer.LastFriendCardPlay = played.CardPlay;
                            session.Dispatch(DetectorTrigger.FriendCardPlayed);
                        }
                    }

                    break;

                // ポーションの使用も「操作した」ことになる(放置ではない)。
                case PotionUsedEntry potionUsed:
                    foreach (ChatterSession session in Sessions.Where(s => s.Owner.Creature == potionUsed.Actor).ToList())
                    {
                        session.Observer.NoteActivity();
                        session.Observer.LastUsedPotion = potionUsed.Potion;
                        session.Observer.LastUsedPotionTarget = potionUsed.Target;
                        session.Dispatch(DetectorTrigger.PotionUsed);
                    }

                    break;

                // 最初のターン開始時の配札(FromHandDraw)は「ターン中に引いた」ことにはならないので対象外。
                case CardDrawnEntry { FromHandDraw: false } drawn:
                    foreach (ChatterSession session in Sessions.Where(s => s.Owner.Creature == drawn.Actor).ToList())
                    {
                        session.Observer.LastDrawnCard = drawn.Card;
                        session.Dispatch(DetectorTrigger.CardDrawn);
                    }

                    break;

                // 敵の撃破は戦闘全体の出来事なので、全セッション(=全てのイヤリングの持ち主)に伝える。
                case DamageReceivedEntry { Result.WasTargetKilled: true, Receiver.Side: CombatSide.Enemy } killed:
                    foreach (ChatterSession session in Sessions.ToList())
                    {
                        session.Observer.LastKilledEnemy = killed.Receiver;
                        session.Observer.LastKiller = KillAttribution.FindKiller(killed);
                        session.Observer.LastKillOverkillDamage = killed.Result.OverkillDamage;
                        session.Dispatch(DetectorTrigger.EnemyKilled);
                    }

                    break;

                // 持ち主がダメージを受けた。
                case DamageReceivedEntry { Receiver.Player: not null } taken:
                    foreach (ChatterSession session in Sessions.Where(s => s.Owner.Creature == taken.Receiver).ToList())
                    {
                        session.Observer.LastDamageTaken = taken;
                        session.Dispatch(DetectorTrigger.DamageTaken);
                    }

                    break;
            }
        }
    }

    private static void OnCombatEnded()
    {
        EndSessions();
    }

    /// <summary>セッションがある間だけ、約1秒おきのタイマーを回す(無い間は止めておく)。</summary>
    private static void EnsureTicking()
    {
        if (_ticking || Sessions.Count == 0)
        {
            return;
        }

        _ticking = true;
        _lastTickTimestamp = Stopwatch.GetTimestamp();
        ScheduleNextTick();
    }

    private static void ScheduleNextTick()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            _ticking = false;
            return;
        }

        // 本家のCmd.Waitと同じ、シーンツリーのタイマー。ポーズ中はツリーごと止まる(processAlways: false)。
        SceneTreeTimer timer = tree.CreateTimer(TickIntervalSeconds, false);
        timer.Timeout += OnTick;
    }

    private static void OnTick()
    {
        try
        {
            double elapsed = Math.Min(Stopwatch.GetElapsedTime(_lastTickTimestamp).TotalSeconds, MaxTickDeltaSeconds);
            _lastTickTimestamp = Stopwatch.GetTimestamp();

            foreach (ChatterSession session in Sessions.ToList())
            {
                session.Tick(elapsed);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"{MainFile.ModId}: unhandled exception in chatter tick: {e}");
        }
        finally
        {
            if (Sessions.Count > 0)
            {
                ScheduleNextTick();
            }
            else
            {
                _ticking = false;
            }
        }
    }

    private static void EndSessions()
    {
        foreach (ChatterSession session in Sessions)
        {
            session.Dispose();
        }

        Sessions.Clear();
    }

    private static void Guard(Action handler)
    {
        try
        {
            handler();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"{MainFile.ModId}: unhandled exception in chatter hub: {e}");
        }
    }
}
