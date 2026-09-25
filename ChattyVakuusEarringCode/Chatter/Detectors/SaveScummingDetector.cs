using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 戦闘中に「タイトルに戻る」でランを放棄した瞬間の一言。本家の乱数は全て決定論的な擬似乱数なので、
/// この操作はいわゆる「セーブスカム」(不利な展開になった時にタイトルへ戻り、乱数を引き直して再挑戦する行為)に
/// 使われうる。実際にそれが目的かどうかは判別できないが、ヴァクーは決めつけて煽ってよい(それが芸風なので)。
/// </summary>
public sealed class SaveScummingDetector : PlayDetector
{
    private const string Topic = "SAVE_SCUMMING";

    public override DetectorTrigger Triggers => DetectorTrigger.ReturnedToMainMenu;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer is { TurnNumber: <= 1, LastManualCardPlay: null })
        {
            return null;
        }
        
        MainFile.Logger.Info("Save Scumming Detected!", 1);
        
        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Force = true,
        };
    }
}
