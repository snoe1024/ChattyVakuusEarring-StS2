using System.Collections.Generic;
using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 直近に手動でプレイしたカードのレア度が、コモン・ベーシックばかりに偏っている時の一言。
/// </summary>
/// <remarks>
/// 直近<see cref="WindowSize"/>枚(手動プレイのみ、オート・友軍プレイは含まない)の窓を保持し、
/// 全てコモン・ベーシックだった時に発火する。一度言ったら、窓の中身が崩れる(コモン・ベーシック以外を
/// プレイする)までは再度言わない(<see cref="_armed"/>)。窓・武装状態はDetectorインスタンス自身が
/// 持つ(戦闘ごとに新規作成されるので、戦闘をまたいで持ち越されることはない)。
/// </remarks>
public sealed class CommonCardStreakDetector : PlayDetector
{
    private const string Topic = "COMMON_CARD_STREAK";

    private const int WindowSize = 8;

    private readonly Queue<CardRarity> _recentRarities = new();

    private bool _armed = true;

    public override bool ShouldActivate(PlayObserver observer) => observer.ActIndex >= 2;

    public override DetectorTrigger Triggers => DetectorTrigger.CardPlayed;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        CardRarity? rarity = observer.LastManualCardPlay?.Card.Rarity;
        if (rarity == null)
        {
            return null;
        }

        _recentRarities.Enqueue(rarity.Value);
        if (_recentRarities.Count > WindowSize)
        {
            _recentRarities.Dequeue();
        }

        bool allCommonOrBasic = _recentRarities.Count == WindowSize
            && _recentRarities.All(r => r is CardRarity.Basic or CardRarity.Common);

        if (!allCommonOrBasic)
        {
            _armed = true;
            return null;
        }

        if (!_armed)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        _armed = false;
        return new Utterance
        {
            Line = line,
            Probability = 0.3f,
            Tags = new[] { UtteranceTag.HandQuality },
        };
    }
}
