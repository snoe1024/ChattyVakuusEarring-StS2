using System.Collections.Generic;
using ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Patch;

/// <summary>
/// アタックポーション等の「カードを1枚選ぶ」画面(<c>NChooseACardSelectionScreen</c>、
/// <c>CardSelectCmd.FromChooseACardScreen</c>経由で表示される)が表示された瞬間を捉え、
/// <see cref="CardRewardChatterHub"/>に伝える。<c>CardRewardShownPatch</c>のこの画面版。
/// </summary>
/// <remarks>
/// <c>ShowScreen</c>は静的メソッドで、生成した画面インスタンスを返す(<c>TestMode</c>中はnull)。
/// 戻り値と引数の<c>cards</c>(提示されたカード一覧)・<c>canSkip</c>さえ拾えれば十分なので、
/// Postfixで両方受け取るだけで済む。
/// </remarks>
[HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen.ShowScreen))]
public static class ChooseACardShownPatch
{
    [HarmonyPostfix]
    public static void Postfix(NChooseACardSelectionScreen? __result, IReadOnlyList<CardModel> cards, bool canSkip)
    {
        if (__result != null)
        {
            CardRewardChatterHub.OnChooseACardShown(__result, cards, canSkip);
        }
    }
}
