using System;
using System.Collections.Generic;
using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter;

/// <summary>
/// 「1回の戦闘 × 囁きのイヤリングの持ち主1人」の間だけ存在する、Observer・Speaker・Detector群の組。
/// </summary>
/// <remarks>
/// 戦闘開始時にDetectorを作って発火しうるものだけに絞り(<see cref="PlayDetector.ShouldActivate"/>)、
/// その後は<see cref="Dispatch"/>で「今このタイミングが来た」と伝えるたびに、該当Detectorの提案を集めてSpeakerに渡す。
/// </remarks>
internal sealed class ChatterSession : IDisposable
{
    private readonly List<PlayDetector> _activeDetectors = new();

    private readonly VakuuSpeaker _speaker;

    private readonly PlayerCombatState? _playerCombatState;

    public ChatterSession(Player owner, ICombatState combatState)
    {
        Observer = new PlayObserver(owner, combatState);
        _speaker = new VakuuSpeaker(owner.Creature);

        _playerCombatState = owner.PlayerCombatState;
        if (_playerCombatState != null)
        {
            _playerCombatState.PlayerTurnPhaseChanged += OnPlayerTurnPhaseChanged;
        }

        List<PlayDetector> all = DetectorRegistry.CreateAll();
        foreach (PlayDetector detector in all)
        {
            try
            {
                if (detector.ShouldActivate(Observer))
                {
                    _activeDetectors.Add(detector);
                }
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"{MainFile.ModId}: {detector.Id}.ShouldActivate threw, deactivated: {e}");
            }
        }

        MainFile.Logger.Info(
            $"{MainFile.ModId}: chatter session started for player {owner.NetId}, " +
            $"{_activeDetectors.Count}/{all.Count} detector(s) active: " +
            string.Join(", ", _activeDetectors.Select(d => d.Id)));
    }

    public PlayObserver Observer { get; }

    public Player Owner => Observer.Owner;

    public void Dispose()
    {
        if (_playerCombatState != null)
        {
            _playerCombatState.PlayerTurnPhaseChanged -= OnPlayerTurnPhaseChanged;
        }
    }

    /// <summary>
    /// 最初のターンで<c>AutoPrePlay</c>フェーズに入った(=手札が配られ終わり、ヴァクーの代打ちが始まる)時に、
    /// <see cref="DetectorTrigger.VakuuTurnStarted"/>を出す。このイベントは<c>CombatManager</c>には無く、
    /// <c>PlayerCombatState</c>自身が持っているので、セッションが直接購読する。
    /// </summary>
    private void OnPlayerTurnPhaseChanged()
    {
        try
        {
            // フェーズが変わるたびに、前のフェーズ(前のターン)の放置時間・ターン終了済みの状態を持ち越さないよう戻す。
            Observer.ResetTurnState();

            if (Observer.IsInVakuuTurn)
            {
                Dispatch(DetectorTrigger.VakuuTurnStarted);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"{MainFile.ModId}: unhandled exception in phase handler: {e}");
        }
    }

    /// <summary>
    /// 時間経過を伝える(<see cref="ChatterHub"/>が約1秒おきに呼ぶ)。持ち主が操作できる間だけ、
    /// 放置時間を加算して<see cref="DetectorTrigger.Tick"/>を出す。
    /// </summary>
    public void Tick(double deltaSeconds)
    {
        if (!Observer.IsPlayerActing)
        {
            return;
        }

        Observer.AddIdleTime(deltaSeconds);
        Dispatch(DetectorTrigger.Tick);
    }

    /// <summary>
    /// <paramref name="trigger"/>のタイミングが来たことを、関心を持つDetectorに伝え、出てきた提案をSpeakerに渡す。
    /// </summary>
    public void Dispatch(DetectorTrigger trigger)
    {
        if (Owner.Creature.IsDead)
        {
            return;
        }

        var proposals = new List<Utterance>();
        foreach (PlayDetector detector in _activeDetectors.ToList())
        {
            if ((detector.Triggers & trigger) == 0)
            {
                continue;
            }

            try
            {
                Utterance? utterance = detector.Detect(Observer, trigger);
                if (utterance != null)
                {
                    utterance.DetectorId = detector.Id;
                    proposals.Add(utterance);
                }
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"{MainFile.ModId}: {detector.Id}.Detect threw, deactivated for this combat: {e}");
                _activeDetectors.Remove(detector);
            }
        }

        // 強制発言 → 優先度の高い順に判断する。同順位は毎回同じDetectorが優先されないようシャッフルする。
        // 先に喋った側の間隔ルールが後続を自然に黙らせ、先の側が確率判定などで黙れば後続に順番が回る。
        foreach (Utterance utterance in proposals
                     .OrderByDescending(u => u.Force)
                     .ThenByDescending(u => u.Priority)
                     .ThenBy(_ => ChatterRandom.NextDouble()))
        {
            _speaker.TrySpeak(utterance);
        }
    }
}
