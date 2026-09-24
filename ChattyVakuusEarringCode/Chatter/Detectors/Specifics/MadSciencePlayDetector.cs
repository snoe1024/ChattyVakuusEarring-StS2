using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors.Specifics;

public class MadSciencePlayDetector : PlayDetector
{
    private const string Topic = "PLAY_MAD_SCIENCE";
    
    public override DetectorTrigger Triggers => DetectorTrigger.CardPlayed;

    public override bool ShouldActivate(PlayObserver observer) => observer.Deck.Any(card => card is MadScience);

    private bool _mentioned;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (observer.LastManualCardPlay?.Card is not MadScience || _mentioned)
        {
            return null;
        }
        
        LocString? line = SpeechTable.Pick(Topic);
        if (line == null)
        {
            return null;
        }

        _mentioned = true;
        return new Utterance
        {
            Line = line,
            Probability = 0.2f,
            Priority = 3,
        };
    }
}