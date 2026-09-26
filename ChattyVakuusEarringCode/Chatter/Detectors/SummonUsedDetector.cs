using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Characters;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「召喚」(ネクロバインダーのキーワード、<c>OstyCmd.Summon</c>が成立した瞬間)についての一言。
/// ネクロバインダー自身が使った時と、それ以外のキャラクターが使った時(フレンドカード獲得イベント等)とで
/// 話題を分ける。
/// </summary>
public sealed class SummonUsedDetector : PlayDetector
{
    private const string Topic = "SUMMON_USED";

    public override DetectorTrigger Triggers => DetectorTrigger.Summoned;

    private bool _fired;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.IsFirstTurn)
        {
            return null;
        }

        // ネクロバインダー以外は最初の一回しか言わない
        bool isNative = observer.Owner.Character is Necrobinder;
        if (!isNative && _fired)
        {
            return null;
        }
        
        LocString? line = SpeechTable.Pick(Topic, isNative ? "native" : "other");
        if (line == null)
        {
            return null;
        }
        
        _fired = true;

        return new Utterance
        {
            Line = line,
            Probability = isNative ? 0.15f : 0.5f,
            Tags = new[] { UtteranceTag.CharacterMechanic },
        };
    }
}
