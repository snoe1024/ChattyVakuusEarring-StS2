using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 「随分と長引いていますねぇ。」。戦闘が長引いた時の一言。
/// </summary>
public sealed class LongFightDetector : PlayDetector
{
    private const string Topic = "LONG_FIGHT";

    /// <summary>最初に言うターン数。</summary>
    private const int FirstTurn = 8;

    /// <summary>言った後、まだ戦闘が続いている場合に次を言うまでのターン数。</summary>
    private const int RepeatEveryTurns = 6;

    private int _nextTurn = FirstTurn;

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerTurnStarted;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.TurnNumber < _nextTurn)
        {
            return null;
        }

        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        _nextTurn = observer.TurnNumber + RepeatEveryTurns;
        return new Utterance
        {
            Line = line,
            Probability = 0.7f,
            Tags = new[] { UtteranceTag.Encounter },
        };
    }
}
