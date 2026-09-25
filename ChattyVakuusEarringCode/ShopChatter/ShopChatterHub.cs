using System;
using System.Diagnostics;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Config;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter;

/// <summary>
/// ショップ(商人の部屋)でのゲーム本体のイベントと<see cref="ShopChatterSession"/>を結びつける入口。
/// <c>Chatter.ChatterHub</c>のショップ版だが、こちらは<c>CombatManager</c>ではなく<c>RunManager</c>の
/// 部屋の出入りイベントと、商人画面(<c>NMerchantInventory</c>)のGodotシグナルを購読する。
/// </summary>
/// <remarks>
/// 常にこの端末のローカルプレイヤーの分しか扱わない(<c>RunManager.RoomEntered</c>はパーティ全体で1回しか
/// 発火しない共有イベントなので、<c>Chatter.ChatterHub</c>のように「他人の分を弾く」フィルタは元々不要)。
/// </remarks>
internal static class ShopChatterHub
{
    private static ShopChatterSession? _session;

    /// <summary>時間経過(<see cref="ShopDetectorTrigger.Tick"/>)を伝える間隔。</summary>
    private const double TickIntervalSeconds = 1.0;

    /// <summary>1回のTickで進める時間の上限(理由は<c>Chatter.ChatterHub</c>と同じ)。</summary>
    private const double MaxTickDeltaSeconds = 2.0;

    private static bool _ticking;

    private static long _lastTickTimestamp;

    public static void Initialize()
    {
        RunManager.Instance.RoomEntered += () => Guard(OnRoomEntered);
        RunManager.Instance.RoomExited += () => Guard(OnRoomExited);
    }

    private static void OnRoomEntered()
    {
        // NMerchantRoom.Instanceは「今表示されているルームシーンが商人の部屋か」を毎回動的に見ているだけなので、
        // これがnullでない=商人の部屋に入った、という判定を兼ねられる(RunState.CurrentRoomは非公開で使えない)。
        // Inventory.Inventory.Playerは、常にこの端末のローカルプレイヤー
        // (NMerchantRoom.Inventoryは`Room.GetLocalInventory()`をラップしているため)。
        NMerchantRoom? room = NMerchantRoom.Instance;
        Player? me = room?.Inventory?.Inventory?.Player;
        if (room == null || me == null)
        {
            return;
        }

        // 設定がオンの間は、レリックを持っていなくても発言システムを有効にする(戦闘中と同じ規則)。
        if (me.GetRelic<WhisperingEarring>() == null && !ChattyVakuusEarringConfig.AllowWhisperingWithoutEarring)
        {
            return;
        }

        _session = new ShopChatterSession(me);
        _session.Dispatch(ShopDetectorTrigger.RoomEntered);

        room.Inventory.Connect(NMerchantInventory.SignalName.InventoryClosed, Callable.From(() => Guard(OnShopClosed)));

        EnsureTicking();
    }

    private static void OnShopClosed()
    {
        _session?.Dispatch(ShopDetectorTrigger.ShopClosed);
    }

    private static void OnRoomExited()
    {
        _session = null;
    }

    /// <summary>セッションがある間だけ、約1秒おきのタイマーを回す(無い間は止めておく)。</summary>
    private static void EnsureTicking()
    {
        if (_ticking || _session == null)
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

        SceneTreeTimer timer = tree.CreateTimer(TickIntervalSeconds, false);
        timer.Timeout += OnTick;
    }

    private static void OnTick()
    {
        try
        {
            double elapsed = Math.Min(Stopwatch.GetElapsedTime(_lastTickTimestamp).TotalSeconds, MaxTickDeltaSeconds);
            _lastTickTimestamp = Stopwatch.GetTimestamp();

            _session?.Tick(elapsed);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"{MainFile.ModId}: unhandled exception in shop chatter tick: {e}");
        }
        finally
        {
            if (_session != null)
            {
                ScheduleNextTick();
            }
            else
            {
                _ticking = false;
            }
        }
    }

    private static void Guard(Action handler)
    {
        try
        {
            handler();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"{MainFile.ModId}: unhandled exception in shop chatter hub: {e}");
        }
    }
}
