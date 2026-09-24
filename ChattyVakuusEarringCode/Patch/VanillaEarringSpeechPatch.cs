using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Patch;

/// <summary>
/// バニラの囁きのイヤリングが代打ちの締めに言う台詞(approval / warning)を、このmodの発言システムへ引き取る。
/// </summary>
/// <remarks>
/// <c>WhisperingEarring</c>本体には手を入れず、その台詞を出す<c>TalkCmd.Play</c>だけを横取りする。
/// 引き取った時点は「ヴァクーの代打ちが終わった」タイミングそのものなので、それを
/// <see cref="Chatter.Observer.DetectorTrigger.VakuuFinishedPlaying"/>としてDetectorに伝え、
/// 実際に何を喋るか(元の台詞に限らない)はDetector側が決める。
/// 自分たちがセッションを持っていない持ち主(=このmodの処理対象外)の台詞は、素通しにしてバニラ通りに喋らせる。
/// </remarks>
[HarmonyPatch(typeof(TalkCmd), nameof(TalkCmd.Play))]
public static class VanillaEarringSpeechPatch
{
    [HarmonyPrefix]
    public static bool Prefix(LocString line, Creature speaker)
    {
        // 自分たち(VakuuSpeaker)の発言は素通し。
        if (VakuuSpeaker.IsEmitting)
        {
            return true;
        }

        if (!VanillaEarringLines.IsVanillaClosingLine(line) || !ChatterHub.IsHandling(speaker))
        {
            return true;
        }

        ChatterHub.OnVakuuFinishedPlaying(speaker);
        return false; // バニラの台詞は出さない(代わりにDetector経由で喋る)
    }
}
