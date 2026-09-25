using System;
using System.Collections.Generic;
using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter;

/// <summary>
/// 「1回のショップ入室」の間だけ存在する、Observer・Speaker・Detector群の組。<c>Chatter.ChatterSession</c>のショップ版。
/// </summary>
internal sealed class ShopChatterSession
{
    private readonly List<ShopDetector> _activeDetectors = new();

    private readonly VakuuSpeaker _speaker;

    public ShopChatterSession(Player owner)
    {
        Observer = new ShopObserver(owner) { GoldAtEntry = owner.Gold };
        _speaker = new VakuuSpeaker(ShopVakuuBubble.TryShow);

        List<ShopDetector> all = ShopDetectorRegistry.CreateAll();
        foreach (ShopDetector detector in all)
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
            $"{MainFile.ModId}: shop chatter session started for player {owner.NetId}, " +
            $"{_activeDetectors.Count}/{all.Count} detector(s) active: " +
            string.Join(", ", _activeDetectors.Select(d => d.Id)));
    }

    public ShopObserver Observer { get; }

    /// <summary>
    /// 時間経過を伝える(<see cref="ShopChatterHub"/>が約1秒おきに呼ぶ)。ショップ画面が開いている間
    /// (全身像が隠れている間)は、放置時間を加算せず判定もしない。
    /// </summary>
    public void Tick(double deltaSeconds)
    {
        if (NMerchantRoom.Instance?.Inventory?.IsOpen == true)
        {
            return;
        }

        Observer.AddIdleTime(deltaSeconds);
        Dispatch(ShopDetectorTrigger.Tick);
    }

    /// <summary>
    /// <paramref name="trigger"/>のタイミングが来たことを、関心を持つDetectorに伝え、出てきた提案をSpeakerに渡す。
    /// </summary>
    public void Dispatch(ShopDetectorTrigger trigger)
    {
        if (trigger is ShopDetectorTrigger.RoomEntered or ShopDetectorTrigger.ShopClosed)
        {
            Observer.ResetIdle();
        }

        var proposals = new List<Utterance>();
        foreach (ShopDetector detector in _activeDetectors.ToList())
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
                MainFile.Logger.Error($"{MainFile.ModId}: {detector.Id}.Detect threw, deactivated for this visit: {e}");
                _activeDetectors.Remove(detector);
            }
        }

        // 強制発言 → 優先度の高い順に判断する。同順位は毎回同じDetectorが優先されないようシャッフルする。
        foreach (Utterance utterance in proposals
                     .OrderByDescending(u => u.Force)
                     .ThenByDescending(u => u.Priority)
                     .ThenBy(_ => ChatterRandom.NextDouble()))
        {
            _speaker.TrySpeak(utterance);
        }
    }
}
