using System.Collections.Generic;
using ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Patch;

/// <summary>
/// カード報酬画面(<c>NCardRewardSelectionScreen</c>、戦闘勝利後にカードを選ぶ画面)が表示された瞬間を捉え、
/// <see cref="CardRewardChatterHub"/>に伝える。
/// </summary>
/// <remarks>
/// <c>ShowScreen</c>は静的メソッドで、生成した画面インスタンスを返す(<c>TestMode</c>中はnull)。
/// 戻り値と引数の<c>options</c>(提示されたカード一覧)さえ拾えれば十分なので、Postfixで両方受け取るだけで済む。
/// </remarks>
[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.ShowScreen))]
public static class CardRewardShownPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCardRewardSelectionScreen? __result, IReadOnlyList<CardCreationResult> options)
    {
        if (__result != null)
        {
            CardRewardChatterHub.OnCardRewardShown(__result, options);
        }
    }
}
