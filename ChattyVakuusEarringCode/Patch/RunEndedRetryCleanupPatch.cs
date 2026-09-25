using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Patch;

/// <summary>
/// ランが完了した(勝利・敗北いずれか)瞬間に、<see cref="RetryCountDetector"/>が記録したそのランの
/// セーブスカム回数を破棄する。
/// </summary>
/// <remarks>
/// <c>RunManager.OnEnded(bool isVictory)</c>は、経路によらず本家が「ランが本当に終わった」時に呼ぶ
/// 唯一の入口(勝利は<c>RunManager</c>内、敗北は<c>CreatureCmd</c>から呼ばれる)。「あきらめる」操作は
/// この入口を通らないが、セーブファイル自体が消えるので、そちらの掃除は<see cref="RetryCountDetector"/>の
/// 起動時スイープに任せる。戻り値の<c>SerializableRun</c>からこのランのシードを取り、
/// <see cref="RetryCountDetector.ForgetRun"/>でそのシードに紐づく記録だけ削除する。
/// </remarks>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
public static class RunEndedRetryCleanupPatch
{
    [HarmonyPostfix]
    public static void Postfix(SerializableRun __result)
    {
        RetryCountDetector.ForgetRun(__result.SerializableRng.Seed);
    }
}
