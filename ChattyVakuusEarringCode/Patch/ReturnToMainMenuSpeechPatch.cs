using System;
using System.Threading.Tasks;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Patch;

/// <summary>
/// メインメニューへ戻る操作(ポーズメニューの「あきらめる」→確認ポップアップの「はい」、「保存して終了」、
/// マルチプレイクライアントの「通信切断」)が確定した瞬間を捉える。
/// </summary>
/// <remarks>
/// <para>
/// 経路ごとに呼ばれるメソッドは違うが(<c>RunManager.Abandon</c>、<c>NPauseMenu.CloseToMenu</c>、
/// <c>RunManager.LocalPlayerDisconnected</c>→<c>ReturnToMainMenuWithError</c>→
/// <c>NGame.ReturnToMainMenuAfterRun</c>等)、いずれも最終的に本家の<c>NGame.ReturnToMainMenu()</c>
/// (パラメータ無し、`async Task`)に合流する。ここ1箇所だけをパッチすれば、経路を問わず全部捕まえられる。
/// </para>
/// <para>
/// <b>ポーズメニューについて</b>: ポーズメニューは吹き出しより前面に表示され続けるため、そのままだと台詞が
/// 見切れる(実機で確認済み)。遅延を入れる場合は<c>NCapstoneContainer.Instance.Close()</c>
/// (ポーズメニュー等、開いている「キャプストーン」画面全般を閉じる本家API)も併せて呼ぶ。どうせランを終える
/// 直前なので、他のボタンが押せなくなっても問題にならない。シングルプレイでは、キャプストーン画面が開いている間
/// 戦闘が自動でポーズされる仕組みなので、閉じると戦闘がアンポーズされる副作用があるが、すぐ画面遷移するので実害は無い。
/// </para>
/// </remarks>
[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenu), new Type[] { })]
public static class ReturnToMainMenuSpeechPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NGame __instance, ref Task __result)
    {
        if (!ChatterHub.IsActive)
        {
            return true;
        }

        // ポーズメニュー等、開いているキャプストーン画面を閉じる。そのままだと吹き出しより前面に残って
        // 見切れるが、どうせランを終える(=これ以上どのボタンも意味を持たない)ので閉じてしまって問題ない。
        NCapstoneContainer.Instance?.Close();

        ChatterHub.OnReturnedToMainMenu();

        return true;
    }
}
