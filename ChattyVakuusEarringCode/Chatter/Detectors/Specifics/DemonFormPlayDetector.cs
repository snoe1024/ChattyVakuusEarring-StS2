using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Specifics;

public class DemonFormPlayDetector : PlayDetector
{
    private const string Topic = "PLAY_DEMON_FORM";
    private const string TopicNotIronclad = "PLAY_DEMON_FORM-NOT_IRONCLAD";
    
    public override DetectorTrigger Triggers => DetectorTrigger.CardPlayed;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.LastManualCardPlay?.Card is not DemonForm)
        {
            return null;
        }
        
        LocString? line = SpeechTable.Pick(observer.Owner.Character is Ironclad ? Topic : TopicNotIronclad);
        if (line == null)
        {
            return null;
        }

        return new Utterance
        {
            Line = line,
            Priority = 8,
            Force = true,
        };
    }
}